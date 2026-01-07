using System.IO;
using Moq;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Agents;

public class ReleaseAgentTests
{
    [Fact]
    public async Task RunAsync_WritesReleaseNotes()
    {
        using var workspace = new TempWorkspace();
        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Release");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var branch = WorkItemBranch.BuildBranchName(workItem);

        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(branch, config.Workflow.DefaultBaseBranch));
        repo.Setup(r => r.CommitAndPush(branch, It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(true);

        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);

        var agent = new ReleaseAgent();

        var result = await agent.RunAsync(ctx);

        var releasePath = Path.Combine(workspace.WorkspacePath, "orchestrator", "release", "issue-1.md");
        var content = File.ReadAllText(releasePath);

        Assert.True(result.Success);
        Assert.Contains("Release notes written", result.Notes);
        Assert.Contains("Release Notes: Issue 1 - Release", content);
    }
}
