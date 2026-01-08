using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Watcher;

public class GitHubWebhookListenerTests
{
    [Fact]
    public void NormalizePath_ReturnsDefaultWhenMissing()
    {
        var normalized = GitHubWebhookListener.NormalizePath(null);

        Assert.Equal("/webhook", normalized);
    }

    [Theory]
    [InlineData("webhook", "/webhook")]
    [InlineData("/webhook", "/webhook")]
    [InlineData("/webhook/", "/webhook")]
    [InlineData("hooks/incoming/", "/hooks/incoming")]
    public void NormalizePath_NormalizesLeadingAndTrailingSlashes(string input, string expected)
    {
        var normalized = GitHubWebhookListener.NormalizePath(input);

        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void IsSignatureValid_AllowsMissingSecret()
    {
        var payload = "payload"u8.ToArray();

        var valid = GitHubWebhookListener.IsSignatureValid(null, payload, null);

        Assert.True(valid);
    }

    [Fact]
    public void IsSignatureValid_RejectsMissingSignatureHeader()
    {
        var payload = "payload"u8.ToArray();

        var valid = GitHubWebhookListener.IsSignatureValid("secret", payload, null);

        Assert.False(valid);
    }

    [Fact]
    public void IsSignatureValid_AcceptsValidSignature()
    {
        var payload = "payload"u8.ToArray();
        var signature = GitHubWebhookListener.ComputeSignature("secret", payload);

        var valid = GitHubWebhookListener.IsSignatureValid("secret", payload, signature);

        Assert.True(valid);
    }

    [Fact]
    public void IsSignatureValid_RejectsInvalidSignature()
    {
        var payload = "payload"u8.ToArray();

        var valid = GitHubWebhookListener.IsSignatureValid("secret", payload, "sha256=deadbeef");

        Assert.False(valid);
    }

    [Theory]
    [InlineData("issues", "opened", true)]
    [InlineData("issues", "labeled", true)]
    [InlineData("issues", "unlabeled", true)]
    [InlineData("issues", "edited", true)]
    [InlineData("issues", "reopened", true)]
    [InlineData("issues", "closed", false)]
    [InlineData("pull_request", "opened", false)]
    [InlineData(null, "opened", false)]
    public void IsRelevantEvent_FiltersEvents(string? eventName, string? action, bool expected)
    {
        var result = GitHubWebhookListener.IsRelevantEvent(eventName, action);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryGetAction_ReturnsActionFromPayload()
    {
        var payload = "{\"action\":\"opened\"}"u8.ToArray();

        var action = GitHubWebhookListener.TryGetAction(payload);

        Assert.Equal("opened", action);
    }

    [Fact]
    public void TryGetAction_ReturnsNullOnInvalidJson()
    {
        var payload = "{not-json"u8.ToArray();

        var action = GitHubWebhookListener.TryGetAction(payload);

        Assert.Null(action);
    }

    [Fact]
    public void EvaluateRequest_RejectsNonPostMethod()
    {
        var payload = "{}"u8.ToArray();

        var decision = GitHubWebhookListener.EvaluateRequest(
            httpMethod: "GET",
            path: "/webhook",
            expectedPath: "/webhook",
            secret: null,
            signatureHeader: null,
            payload: payload,
            eventName: "issues");

        Assert.Equal((int)System.Net.HttpStatusCode.MethodNotAllowed, decision.StatusCode);
        Assert.False(decision.ShouldTrigger);
    }

    [Fact]
    public void EvaluateRequest_RejectsWrongPath()
    {
        var payload = "{}"u8.ToArray();

        var decision = GitHubWebhookListener.EvaluateRequest(
            httpMethod: "POST",
            path: "/wrong",
            expectedPath: "/webhook",
            secret: null,
            signatureHeader: null,
            payload: payload,
            eventName: "issues");

        Assert.Equal((int)System.Net.HttpStatusCode.NotFound, decision.StatusCode);
        Assert.False(decision.ShouldTrigger);
    }

    [Fact]
    public void EvaluateRequest_RejectsInvalidSignature()
    {
        var payload = "{}"u8.ToArray();

        var decision = GitHubWebhookListener.EvaluateRequest(
            httpMethod: "POST",
            path: "/webhook",
            expectedPath: "/webhook",
            secret: "secret",
            signatureHeader: "sha256=bad",
            payload: payload,
            eventName: "issues");

        Assert.Equal((int)System.Net.HttpStatusCode.Unauthorized, decision.StatusCode);
        Assert.False(decision.ShouldTrigger);
    }

    [Fact]
    public void EvaluateRequest_HandlesPing()
    {
        var payload = "{}"u8.ToArray();

        var decision = GitHubWebhookListener.EvaluateRequest(
            httpMethod: "POST",
            path: "/webhook",
            expectedPath: "/webhook",
            secret: null,
            signatureHeader: null,
            payload: payload,
            eventName: "ping");

        Assert.Equal((int)System.Net.HttpStatusCode.OK, decision.StatusCode);
        Assert.False(decision.ShouldTrigger);
    }

    [Fact]
    public void EvaluateRequest_RejectsMissingEvent()
    {
        var payload = "{}"u8.ToArray();

        var decision = GitHubWebhookListener.EvaluateRequest(
            httpMethod: "POST",
            path: "/webhook",
            expectedPath: "/webhook",
            secret: null,
            signatureHeader: null,
            payload: payload,
            eventName: null);

        Assert.Equal((int)System.Net.HttpStatusCode.BadRequest, decision.StatusCode);
        Assert.False(decision.ShouldTrigger);
    }

    [Fact]
    public void EvaluateRequest_RejectsIssuesWithoutAction()
    {
        var payload = "{}"u8.ToArray();

        var decision = GitHubWebhookListener.EvaluateRequest(
            httpMethod: "POST",
            path: "/webhook",
            expectedPath: "/webhook",
            secret: null,
            signatureHeader: null,
            payload: payload,
            eventName: "issues");

        Assert.Equal((int)System.Net.HttpStatusCode.BadRequest, decision.StatusCode);
        Assert.False(decision.ShouldTrigger);
    }

    [Fact]
    public void EvaluateRequest_AcceptsRelevantIssueEvent()
    {
        var payload = "{\"action\":\"opened\"}"u8.ToArray();

        var decision = GitHubWebhookListener.EvaluateRequest(
            httpMethod: "POST",
            path: "/webhook",
            expectedPath: "/webhook",
            secret: null,
            signatureHeader: null,
            payload: payload,
            eventName: "issues");

        Assert.Equal((int)System.Net.HttpStatusCode.Accepted, decision.StatusCode);
        Assert.True(decision.ShouldTrigger);
    }

    [Fact]
    public async Task StartAsync_TriggersOnRelevantIssueEvent()
    {
        var port = GetFreePort();
        var config = MockWorkContext.CreateConfig() with
        {
            WebhookListenHost = "localhost",
            WebhookPort = port,
            WebhookPath = "/webhook",
            WebhookSecret = ""
        };

        var triggered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var listener = new GitHubWebhookListener(config, () => triggered.TrySetResult(), preferHttps: false);

        var runTask = listener.StartAsync(cts.Token);
        var response = await PostWebhookAsync(listener, "/webhook", "{\"action\":\"opened\"}", "issues");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await triggered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        cts.Cancel();
        await runTask;
    }

    [Fact]
    public async Task StartAsync_IgnoresPingEvent()
    {
        var port = GetFreePort();
        var config = MockWorkContext.CreateConfig() with
        {
            WebhookListenHost = "localhost",
            WebhookPort = port,
            WebhookPath = "/webhook",
            WebhookSecret = ""
        };

        var triggered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var listener = new GitHubWebhookListener(config, () => triggered.TrySetResult(), preferHttps: false);

        var runTask = listener.StartAsync(cts.Token);
        var response = await PostWebhookAsync(listener, "/webhook", "{}", "ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(triggered.Task.IsCompleted);

        cts.Cancel();
        await runTask;
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<HttpResponseMessage> PostWebhookAsync(
        GitHubWebhookListener listener,
        string path,
        string body,
        string eventName)
    {
        var prefix = await WaitForActivePrefixAsync(listener);
        if (prefix is null)
        {
            throw new InvalidOperationException("Listener did not start.");
        }

        var uri = new Uri(prefix);
        var scheme = uri.Scheme;
        var host = uri.Host;
        var port = uri.Port;
        var url = $"{scheme}://{host}:{port}{path}";
        using var client = CreateHttpClient(scheme);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                content.Headers.Add("X-GitHub-Event", eventName);
                return await client.PostAsync(url, content);
            }
            catch (HttpRequestException) when (attempt < 4)
            {
                await Task.Delay(50);
            }
        }

        using var finalContent = new StringContent(body, Encoding.UTF8, "application/json");
        finalContent.Headers.Add("X-GitHub-Event", eventName);
        return await client.PostAsync(url, finalContent);
    }

    private static HttpClient CreateHttpClient(string scheme)
    {
        if (!string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpClient();
        }

        return new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
    }

    private static async Task<string?> WaitForActivePrefixAsync(GitHubWebhookListener listener)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (!string.IsNullOrWhiteSpace(listener.ActivePrefix))
            {
                return listener.ActivePrefix;
            }

            await Task.Delay(25);
        }

        return listener.ActivePrefix;
    }
}
