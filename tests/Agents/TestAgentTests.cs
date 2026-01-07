using System.IO;
using Moq;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Agents;

public class TestAgentTests
{
    [Fact]
    public async Task RunAsync_UpdatesSpecAcceptanceCriteria()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateFile("specs/issue-1.md", "- [ ] add tests\n");

        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Test");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var branch = WorkItemBranch.BuildBranchName(workItem);

        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(branch, config.Workflow.DefaultBaseBranch));
        repo.Setup(r => r.CommitAndPush(branch, It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(true);

        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);

        var agent = new TestAgent();

        var result = await agent.RunAsync(ctx);

        var specPath = Path.Combine(workspace.WorkspacePath, "specs", "issue-1.md");
        var content = File.ReadAllText(specPath);

        Assert.True(result.Success);
        Assert.Contains("updated acceptance criteria", result.Notes);
        Assert.Contains("- [x] add tests", content);
    }

    [Fact]
    public async Task RunAsync_ReturnsNoteWhenSpecMissing()
    {
        using var workspace = new TempWorkspace();

        var workItem = MockWorkContext.CreateWorkItem(number: 2, title: "Test");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);

        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);

        var agent = new TestAgent();

        var result = await agent.RunAsync(ctx);

        Assert.True(result.Success);
        Assert.Contains("could not find the spec file", result.Notes);
    }
}
