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
}
