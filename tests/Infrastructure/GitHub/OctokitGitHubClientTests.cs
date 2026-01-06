using Orchestrator.App.Infrastructure.GitHub;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Infrastructure.GitHub;

public class OctokitGitHubClientTests
{
    [Fact]
    public void Constructor_WithValidConfig_CreatesInstance()
    {
        var config = MockWorkContext.CreateConfig();

        var client = new OctokitGitHubClient(config);

        Assert.NotNull(client);
    }
}
