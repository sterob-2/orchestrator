using System.IO;
using Moq;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Agents;

public class TechLeadReviewAgentTests
{
    [Fact]
    public async Task RunAsync_ReturnsFailureWhenNoPr()
    {
        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Review");
        var config = MockWorkContext.CreateConfig();

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetPullRequestNumberAsync(It.IsAny<string>()))
            .ReturnsAsync((int?)null);

        var workspace = new Mock<IRepoWorkspace>(MockBehavior.Strict);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);

        var ctx = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);
        var agent = new TechLeadReviewAgent();

        var result = await agent.RunAsync(ctx);

        Assert.False(result.Success);
        Assert.Contains("No open PR found", result.Notes);
    }

    [Fact]
    public async Task RunAsync_WritesReviewAndAddsApprovalLabel()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateFile("Assets/Docs/architecture.md", "Architecture");
        workspace.CreateFile("specs/issue-1.md", "Spec content");

        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Review");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var branch = WorkItemBranch.BuildBranchName(workItem);

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetPullRequestNumberAsync(branch))
            .ReturnsAsync(5);
        github.Setup(g => g.GetPullRequestDiffAsync(5))
            .ReturnsAsync("diff");

        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(branch, config.Workflow.DefaultBaseBranch));
        repo.Setup(r => r.CommitAndPush(branch, It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(true);

        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("APPROVED\n- Looks good.");

        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);
        var agent = new TechLeadReviewAgent();

        var result = await agent.RunAsync(ctx);

        var reviewPath = Path.Combine(workspace.WorkspacePath, "reviews", "issue-1.md");
        var content = File.ReadAllText(reviewPath);

        Assert.True(result.Success);
        Assert.Contains(config.Labels.CodeReviewApprovedLabel, result.AddLabels ?? Array.Empty<string>());
        Assert.Contains("APPROVED", content);
    }
}
