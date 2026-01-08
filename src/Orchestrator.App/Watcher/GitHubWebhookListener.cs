using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Orchestrator.App.Watcher;

internal sealed class GitHubWebhookListener : IAsyncDisposable
{
    private readonly OrchestratorConfig _config;
    private readonly Action _onWebhook;
    private readonly HttpListener _listener;
    private readonly string _path;

    public GitHubWebhookListener(OrchestratorConfig config, Action onWebhook)
    {
        _config = config;
        _onWebhook = onWebhook;
        _listener = new HttpListener();
        _path = NormalizePath(config.WebhookPath);

        var prefix = $"https://{_config.WebhookListenHost}:{_config.WebhookPort}{_path}/";
        _listener.Prefixes.Add(prefix);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _listener.Start();
        }
        catch (HttpListenerException ex)
        {
            Logger.WriteLine($"[Webhook] Failed to start listener: {ex.Message}");
            return;
        }

        Logger.WriteLine($"[Webhook] Listening on {_listener.Prefixes.First()}");

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

        if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            response.Close();
            return;
        }

        if (!string.Equals(request.Url?.AbsolutePath, _path, StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.Close();
            return;
        }

        var payload = await ReadBodyAsync(request, cancellationToken);
        if (!IsSignatureValid(_config.WebhookSecret, payload, request.Headers["X-Hub-Signature-256"]))
        {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.Close();
            return;
        }

        _onWebhook();
        response.StatusCode = (int)HttpStatusCode.Accepted;
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
}
