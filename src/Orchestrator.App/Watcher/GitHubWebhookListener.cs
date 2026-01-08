using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchestrator.App.Watcher;

internal sealed class GitHubWebhookListener : IAsyncDisposable
{
    private readonly OrchestratorConfig _config;
    private readonly Action _onWebhook;
    private readonly HttpListener _listener;
    private readonly string _path;
    private string? _activePrefix;
    private readonly bool _preferHttps;
    internal string? ActivePrefix => _activePrefix;

    public GitHubWebhookListener(OrchestratorConfig config, Action onWebhook, bool preferHttps = true)
    {
        _config = config;
        _onWebhook = onWebhook;
        _listener = new HttpListener();
        _path = NormalizePath(config.WebhookPath);
        _preferHttps = preferHttps;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_config.WebhookSecret))
        {
            Logger.WriteLine("[Webhook] Warning: WEBHOOK_SECRET is not set; signature validation is disabled.");
        }

        var started = _preferHttps
            ? TryStartListener(useHttps: true) || TryStartListener(useHttps: false)
            : TryStartListener(useHttps: false);

        if (!started)
        {
            Logger.WriteLine("[Webhook] Failed to start listener on HTTPS or HTTP.");
            return;
        }

        if (_activePrefix is not null)
        {
            Logger.WriteLine($"[Webhook] Listening on {_activePrefix}");
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var contextTask = _listener.GetContextAsync();
                var completed = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, cancellationToken));
                if (completed != contextTask)
                {
                    break;
                }

                _ = Task.Run(() => HandleAsync(contextTask.Result, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested during shutdown.
        }
        catch (ObjectDisposedException)
        {
            // Expected if the listener is disposed during shutdown.
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;
        response.ContentType = "text/plain";

        var payload = await ReadBodyAsync(request, cancellationToken);
        var eventName = request.Headers["X-GitHub-Event"];
        var decision = EvaluateRequest(
            request.HttpMethod,
            request.Url?.AbsolutePath,
            _path,
            _config.WebhookSecret,
            request.Headers["X-Hub-Signature-256"],
            payload,
            eventName);

        if (decision.ShouldTrigger)
        {
            _onWebhook();
        }

        response.StatusCode = decision.StatusCode;
        response.Close();
    }

    private async Task<byte[]> ReadBodyAsync(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await request.InputStream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    internal static bool IsSignatureValid(string? secret, byte[] payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var expected = ComputeSignature(secret, payload);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader));
    }

    internal static string ComputeSignature(string secret, byte[] payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payload);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static WebhookDecision EvaluateRequest(
        string? httpMethod,
        string? path,
        string expectedPath,
        string? secret,
        string? signatureHeader,
        byte[] payload,
        string? eventName)
    {
        if (!string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return new WebhookDecision((int)HttpStatusCode.MethodNotAllowed, false);
        }

        if (!string.Equals(path, expectedPath, StringComparison.OrdinalIgnoreCase))
        {
            return new WebhookDecision((int)HttpStatusCode.NotFound, false);
        }

        if (!IsSignatureValid(secret, payload, signatureHeader))
        {
            return new WebhookDecision((int)HttpStatusCode.Unauthorized, false);
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            return new WebhookDecision((int)HttpStatusCode.BadRequest, false);
        }

        if (string.Equals(eventName, "ping", StringComparison.OrdinalIgnoreCase))
        {
            return new WebhookDecision((int)HttpStatusCode.OK, false);
        }

        var action = TryGetAction(payload);
        if (string.Equals(eventName, "issues", StringComparison.OrdinalIgnoreCase) && action is null)
        {
            return new WebhookDecision((int)HttpStatusCode.BadRequest, false);
        }

        if (!IsRelevantEvent(eventName, action))
        {
            return new WebhookDecision((int)HttpStatusCode.Accepted, false);
        }

        return new WebhookDecision((int)HttpStatusCode.Accepted, true);
    }

    internal static bool IsRelevantEvent(string? eventName, string? action)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return false;
        }

        if (!string.Equals(eventName, "issues", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return action is not null && (
            action.Equals("opened", StringComparison.OrdinalIgnoreCase)
            || action.Equals("edited", StringComparison.OrdinalIgnoreCase)
            || action.Equals("labeled", StringComparison.OrdinalIgnoreCase)
            || action.Equals("unlabeled", StringComparison.OrdinalIgnoreCase)
            || action.Equals("reopened", StringComparison.OrdinalIgnoreCase));
    }

    internal static string? TryGetAction(byte[] payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("action", out var actionElement) &&
                actionElement.ValueKind == JsonValueKind.String)
            {
                return actionElement.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    internal static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/webhook";
        }

        return path.StartsWith("/", StringComparison.OrdinalIgnoreCase)
            ? path.TrimEnd('/')
            : "/" + path.TrimEnd('/');
    }

    public ValueTask DisposeAsync()
    {
        _listener.Close();
        return ValueTask.CompletedTask;
    }

    internal sealed record WebhookDecision(int StatusCode, bool ShouldTrigger);

    private bool TryStartListener(bool useHttps)
    {
        var scheme = useHttps ? "https" : "http";
        var prefix = $"{scheme}://{_config.WebhookListenHost}:{_config.WebhookPort}{_path}/";
        _listener.Prefixes.Clear();
        _listener.Prefixes.Add(prefix);

        try
        {
            _listener.Start();
            _activePrefix = prefix;
            if (!useHttps)
            {
                Logger.WriteLine("[Webhook] Warning: HTTP listener enabled; use HTTPS in production.");
            }

            return true;
        }
        catch (HttpListenerException ex)
        {
            Logger.WriteLine($"[Webhook] Failed to start {scheme.ToUpperInvariant()} listener: {ex.Message}");
            return false;
        }
    }
}
