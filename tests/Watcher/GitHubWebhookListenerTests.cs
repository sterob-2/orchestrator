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
}
