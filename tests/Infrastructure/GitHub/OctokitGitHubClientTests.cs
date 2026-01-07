using System.Linq;
using System.Net.Http;
using Octokit;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Infrastructure.GitHub;

public class OctokitGitHubClientTests
{
    [Fact]
    public void Constructor_ConfiguresHttpClientHeaders()
    {
        var config = MockWorkContext.CreateConfig() with { GitHubToken = "token-123" };
        using var http = new HttpClient();
        var octokit = new GitHubClient(new ProductHeaderValue("test", "1.0"));

        _ = new OctokitGitHubClient(config, octokit, http);

        var userAgent = string.Join(" ", http.DefaultRequestHeaders.UserAgent.Select(u => u.ToString()));
        Assert.Contains("conjunction-orchestrator", userAgent);
        Assert.Contains("0.3", userAgent);
        Assert.Contains(
            "application/vnd.github+json",
            http.DefaultRequestHeaders.Accept.Select(a => a.MediaType));
        Assert.NotNull(http.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", http.DefaultRequestHeaders.Authorization!.Scheme);
        Assert.Equal("token-123", http.DefaultRequestHeaders.Authorization!.Parameter);
    }
}
