using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class DorExecutorTests
{
    [Fact]
    public async Task HandleAsync_PassesWhenRefinementHasNoOpenQuestions()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body with at least 50 characters to pass DoR gate validation rules", "url", new List<string> { "estimate:3" });
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns(false);
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new DorExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var refinement = new RefinementResult(
            "Clarified story with sufficient detail for DoR gate",
            new List<string>
            {
                "Given a user is logged in, when they submit a form, then it should be validated.",
                "Given valid input, when processing, then it should succeed.",
                "Given invalid input, when processing, then an error should be returned."
            },
            new List<string>(), // No open questions
            new ComplexityIndicators(new List<string> { "signal" }, null));

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowJson.Serialize(refinement));
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Contains("DoR gate passed", output.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_FailsWhenRefinementHasOpenQuestions()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(2, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.CommentOnWorkItemAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns(false);
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new DorExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var refinement = new RefinementResult(
            "Clarified story",
            new List<string> { "Criterion 1" },
            new List<string> { "Question 1?", "Question 2?" },
            new ComplexityIndicators(new List<string>(), null));

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowJson.Serialize(refinement));
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Contains("DoR gate failed", output.Notes, StringComparison.OrdinalIgnoreCase);
        github.Verify(g => g.CommentOnWorkItemAsync(2, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_FailsWhenRefinementMissing()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(3, "Title", "Body", "url", new List<string>());
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
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Contains("missing refinement", output.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_WritesFileWhenGateFails()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(4, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.CommentOnWorkItemAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var workspace = new Mock<IRepoWorkspace>();
        string? writtenContent = null;
        workspace.Setup(w => w.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => writtenContent = content);
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns(false);
        var llm = new Mock<ILlmClient>();
        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new DorExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var refinement = new RefinementResult(
            "Story",
            new List<string>(),
            new List<string> { "Open question?" },
            new ComplexityIndicators(new List<string>(), null));

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowJson.Serialize(refinement));
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.NotNull(writtenContent);
        Assert.Contains("DoR Result: Issue #4", writtenContent);
        Assert.Contains("FAILED", writtenContent);
        Assert.Contains("Open question?", writtenContent);
        workspace.Verify(w => w.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
