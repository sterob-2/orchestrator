using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class PlannerExecutorTests
{
    [Fact]
    public async Task HandleAsync_UsesInputWorkItemForPlanLookup()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateFile("plans/issue-1.md", "STATUS: COMPLETE\n");

        var contextItem = MockWorkContext.CreateWorkItem(number: 99, body: "Acceptance criteria\n- add tests");
        var inputItem = MockWorkContext.CreateWorkItem(number: 1, body: "Acceptance criteria\n- add tests");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(contextItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);

        var projectContext = new ProjectContext(
            config.RepoOwner,
            config.RepoName,
            config.Workflow.DefaultBaseBranch,
            config.WorkspacePath,
            config.WorkspaceHostPath,
            config.ProjectOwner,
            config.ProjectOwnerType,
            config.ProjectNumber);
        var input = new WorkflowInput(inputItem, projectContext, Mode: null, Attempt: 0);

        var executor = new PlannerExecutor(ctx);

        var output = await executor.HandleAsync(input, new Mock<IWorkflowContext>().Object);

        Assert.True(output.Success);
        Assert.Contains("Plan already complete", output.Notes);
        Assert.Equal(WorkflowStage.TechLead, output.NextStage);
    }
}
