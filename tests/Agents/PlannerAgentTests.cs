using Moq;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Agents;

public class PlannerAgentTests
{
    [Fact]
    public async Task RunAsync_ReturnsSkipNotes_WhenPlanComplete()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateFile("plans/issue-1.md", "STATUS: COMPLETE\n");

        var workItem = MockWorkContext.CreateWorkItem(number: 1, body: "Acceptance criteria\n- add tests");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);

        var agent = new PlannerAgent();

        var result = await agent.RunAsync(ctx);

        Assert.True(result.Success);
        Assert.Contains("Plan already complete", result.Notes);
    }
}
