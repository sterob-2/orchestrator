using Orchestrator.App.Infrastructure;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Infrastructure;

public class ServiceFactoryTests
{
    [Fact]
    public void Create_ReturnsInfrastructureImplementations()
    {
        var config = MockWorkContext.CreateConfig();

        var services = ServiceFactory.Create(config);

        Assert.IsType<Orchestrator.App.Infrastructure.GitHub.OctokitGitHubClient>(services.GitHub);
        Assert.IsType<Orchestrator.App.Infrastructure.Filesystem.RepoWorkspace>(services.Workspace);
        Assert.IsType<Orchestrator.App.Infrastructure.Git.RepoGit>(services.RepoGit);
        Assert.IsType<Orchestrator.App.Infrastructure.Llm.LlmClient>(services.Llm);
        Assert.IsType<Orchestrator.App.Infrastructure.Mcp.McpClientManager>(services.McpManager);
    }
}
