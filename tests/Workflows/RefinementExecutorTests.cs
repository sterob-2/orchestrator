using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class RefinementExecutorTests
{
    [Fact]
    public async Task HandleAsync_StoresRefinementResult()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[],\"complexitySignals\":[\"risk\"],\"complexitySummary\":\"low\"}");
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new RefinementExecutor(workContext, config.Workflow);
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

        string? stored = null;
        workflowContext.Setup(c => c.QueueStateUpdateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, value, _) => stored = value)
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task HandleAsync_DoRGateFailsWhenRefinementMissing()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(2, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new DorExecutor(workContext, config.Workflow);
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
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Contains("DoR gate failed", output.Notes, StringComparison.OrdinalIgnoreCase);
    }
}
