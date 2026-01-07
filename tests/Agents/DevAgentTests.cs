using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Orchestrator.App;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Agents;

public class DevAgentTests
{
    [Fact]
    public async Task RunAsync_SpecFileMissing_ReturnsSpecQuestionResult()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Test Issue");
        var ctx = MockWorkContext.Create(workspace: workspace, workItem: workItem);
        var agent = new DevAgent();

        var result = await agent.RunAsync(ctx);

        Assert.True(result.Success);
        Assert.NotNull(result.Notes);
        Assert.Contains(ctx.Config.Labels.SpecQuestionsLabel, result.AddLabels ?? Array.Empty<string>());

        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task RunAsync_WithMcp_SpecFileMissing_ReturnsSpecQuestionResult()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var mockMcp = new Mock<McpClientManager>();
        mockMcp.Setup(m => m.CallToolAsync("get_file_info", It.IsAny<IDictionary<string, object?>>()))
            .ThrowsAsync(new InvalidOperationException("File not found"));

        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Test Issue");
        var ctx = MockWorkContext.Create(workspace: workspace, workItem: workItem);
        var ctxWithMcp = ctx with { Mcp = mockMcp.Object };
        var agent = new DevAgent();

        var result = await agent.RunAsync(ctxWithMcp);

        Assert.True(result.Success);
        Assert.NotNull(result.Notes);

        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task RunAsync_ProjectSummaryRequest_PostsSummary()
    {
        var workItem = MockWorkContext.CreateWorkItem(
            number: 7,
            title: "Summarize project",
            body: "Project: https://github.com/users/test-owner/projects/2\nIssue #42");
        var config = MockWorkContext.CreateConfig();

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetProjectSnapshotAsync("test-owner", 2, ProjectOwnerType.User))
            .ReturnsAsync(new ProjectSnapshot(
                "test-owner",
                2,
                ProjectOwnerType.User,
                "Test Project",
                new List<ProjectItem> { new("Item", 42, "https://github.com/test/repo/issues/42", "In Progress") }));
        github.Setup(g => g.CommentOnWorkItemAsync(42, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var workspace = new Mock<IRepoWorkspace>(MockBehavior.Strict);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var ctx = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var agent = new DevAgent();

        var result = await agent.RunAsync(ctx);

        Assert.True(result.Success);
        Assert.Contains("Posted project summary", result.Notes);
    }

    [Fact]
    public async Task RunAsync_UpdatesFilesAndAddsReviewLabel()
    {
        using var workspace = new TempWorkspace();
        var config = MockWorkContext.CreateConfig(workspacePath: workspace.WorkspacePath);
        var labels = new List<string> { config.Labels.WorkItemLabel, config.Labels.SpecClarifiedLabel };
        var workItem = MockWorkContext.CreateWorkItem(
            number: 3,
            title: "Implement feature",
            body: "Implement feature",
            labels: labels);
        workspace.CreateFile(
            "specs/issue-3.md",
            "## Files\n- orchestrator/src/Orchestrator.App/Example.cs\n- orchestrator/tests/ExampleTests.cs\n");

        var branch = WorkItemBranch.BuildBranchName(workItem);
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.EnsureBranch(branch, config.Workflow.DefaultBaseBranch));
        repo.Setup(r => r.CommitAndPush(branch, It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(true);

        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>();
        llm.SetupSequence(l => l.GetUpdatedFileAsync(config.DevModel, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("class Example {}")
            .ReturnsAsync("class ExampleTests {}")
            .ReturnsAsync("{\"ok\": true, \"missing\": []}");

        var ctx = new WorkContext(workItem, github.Object, config, workspace.Workspace, repo.Object, llm.Object);
        var agent = new DevAgent();

        var result = await agent.RunAsync(ctx);

        Assert.True(result.Success);
        Assert.Contains(config.Labels.CodeReviewNeededLabel, result.AddLabels ?? Array.Empty<string>());
    }
}
