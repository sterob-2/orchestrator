using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class CodeReviewExecutorTests
{
    [Fact]
    public async Task HandleAsync_ApprovesWhenFindingsBelowThreshold()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(6, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetPullRequestNumberAsync(It.IsAny<string>())).ReturnsAsync(1);
        github.Setup(g => g.GetPullRequestDiffAsync(It.IsAny<int>())).ReturnsAsync("diff");
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(true);
        workspace.Setup(w => w.ReadAllText(It.IsAny<string>())).Returns("content");
        workspace.Setup(w => w.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"approved\":true,\"summary\":\"ok\",\"findings\":[]}");
        
        // Mock CommentOnWorkItemAsync
        github.Setup(g => g.CommentOnWorkItemAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var devResult = new DevResult(true, workItem.Number, new List<string> { "src/App.cs" }, "");
        var executor = new CodeReviewExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(It.IsAny<string>(), It.IsAny<Func<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.DevResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowJson.Serialize(devResult));

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
    }

    [Fact]
    public async Task HandleAsync_RequiresHumanReviewAfterMaxAttempts()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(7, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetPullRequestNumberAsync(It.IsAny<string>())).ReturnsAsync(1);
        github.Setup(g => g.GetPullRequestDiffAsync(It.IsAny<int>())).ReturnsAsync("diff");
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(true);
        workspace.Setup(w => w.ReadAllText(It.IsAny<string>())).Returns("content");
        workspace.Setup(w => w.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"approved\":false,\"summary\":\"issues\",\"findings\":[{\"severity\":\"BLOCKER\",\"category\":\"Security\",\"message\":\"Issue\"}]}");
        
        github.Setup(g => g.CommentOnWorkItemAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var devResult = new DevResult(true, workItem.Number, new List<string> { "src/App.cs" }, "");
        var executor = new CodeReviewExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(It.IsAny<string>(), It.IsAny<Func<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.DevResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowJson.Serialize(devResult));

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Null(output.NextStage);
    }
}
