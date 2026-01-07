using System.IO;
using Moq;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Agents;

public class PlannerPlanServiceTests
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

        var notes = await PlannerPlanService.RunAsync(ctx);

        Assert.Contains("Plan already complete", notes);
    }

    [Fact]
    public async Task RunAsync_CreatesPlanAndOpensPr_WhenCommitSucceeds()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateFile("docs/templates/plan.md", "STATUS: PENDING\n");

        var workItem = MockWorkContext.CreateWorkItem(number: 2, body: "Acceptance criteria\n- add tests");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var branch = WorkItemBranch.BuildBranchName(workItem);

        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(branch, config.Workflow.DefaultBaseBranch));
        repo.Setup(r => r.CommitAndPush(branch, It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(true);

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.OpenPullRequestAsync(branch, config.Workflow.DefaultBaseBranch, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://github.com/test/repo/pull/1");

        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);

        var notes = await PlannerPlanService.RunAsync(ctx);

        var planPath = Path.Combine(workspace.WorkspacePath, "plans", "issue-2.md");
        var saved = File.ReadAllText(planPath);

        Assert.Contains("opened a draft PR", notes);
        Assert.Contains("- [ ] add tests", saved);
    }

    [Fact]
    public async Task RunAsync_ReturnsSkipNotes_WhenNoCommitAndNoPr()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateFile("docs/templates/plan.md", "STATUS: PENDING\n");

        var workItem = MockWorkContext.CreateWorkItem(number: 3, body: "Acceptance criteria\n- add tests");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var branch = WorkItemBranch.BuildBranchName(workItem);

        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(branch, config.Workflow.DefaultBaseBranch));
        repo.Setup(r => r.CommitAndPush(branch, It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(false);

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetPullRequestNumberAsync(branch))
            .ReturnsAsync((int?)null);

        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);

        var notes = await PlannerPlanService.RunAsync(ctx);

        Assert.Contains("No new commits; skipping PR creation.", notes);
    }
}
