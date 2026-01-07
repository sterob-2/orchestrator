using System.IO;
using Moq;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Agents;

public class TechLeadAgentTests
{
    [Fact]
    public async Task RunAsync_SkipsWhenSpecIsComplete()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateFile("specs/issue-1.md", "STATUS: COMPLETE\n");

        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Spec", labels: new List<string> { "ready-for-agents" });
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);

        var agent = new TechLeadAgent();

        var result = await agent.RunAsync(ctx);

        Assert.True(result.Success);
        Assert.Contains("Spec already complete", result.Notes);
    }

    [Fact]
    public async Task RunAsync_WritesSpecAndSetsNextStage()
    {
        using var workspace = new TempWorkspace();
        workspace.CreateFile("docs/templates/spec.md", "# Spec: Issue {{ISSUE_NUMBER}} - {{ISSUE_TITLE}}\nSTATUS: PENDING\nUPDATED: {{UPDATED_AT_UTC}}\n");

        var workItem = MockWorkContext.CreateWorkItem(number: 2, title: "Orchestrator", body: "Update orchestrator spec.");
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var branch = WorkItemBranch.BuildBranchName(workItem);

        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(branch, config.Workflow.DefaultBaseBranch));
        repo.Setup(r => r.CommitAndPush(branch, It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(true);

        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("## Scope\n- test\n\n## Files\n- src/Orchestrator.App/Program.cs\n- tests/Issue2Tests.cs\n\n## Risks\n- none\n\n## Implementation Plan\n- step\n\n## Acceptance Criteria\n- [ ] done\n");

        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);
        var agent = new TechLeadAgent();

        var result = await agent.RunAsync(ctx);

        var specPath = Path.Combine(workspace.WorkspacePath, "specs", "issue-2.md");
        var content = File.ReadAllText(specPath);

        Assert.True(result.Success);
        Assert.Equal(config.Labels.DevLabel, result.NextStageLabel);
        Assert.Contains(config.Labels.SpecClarifiedLabel, result.AddLabels ?? Array.Empty<string>());
        Assert.Contains("STATUS: COMPLETE", content);
        Assert.Contains("## Files", content);
    }
}
