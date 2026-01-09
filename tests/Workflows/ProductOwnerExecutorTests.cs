using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class ProductOwnerExecutorTests
{
    [Fact]
    public async Task HandleAsync_AnswersProductQuestion()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // Setup LLM to return product answer
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"Question\":\"What happens when user clicks submit?\",\"Answer\":\"The form is validated and submitted to the backend\",\"Reasoning\":\"Based on clarified story\"}");

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        // Setup classification result
        var classificationResult = "{\"Classification\":{\"Question\":\"What happens when user clicks submit?\",\"Type\":\"Product\",\"Reasoning\":\"User workflow question\"}}";
        workContext.State[WorkflowStateKeys.QuestionClassificationResult] = classificationResult;

        // Setup refinement result
        var refinementResult = "{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[\"What happens when user clicks submit?\"],\"complexitySignals\":[],\"complexitySummary\":\"low\"}";
        workContext.State[WorkflowStateKeys.RefinementResult] = refinementResult;

        var executor = new ProductOwnerExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.QuestionClassificationResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(classificationResult);
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refinementResult);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        string? storedAnswer = null;
        workflowContext.Setup(c => c.QueueStateUpdateAsync(WorkflowStateKeys.CurrentQuestionAnswer, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, value, _) => storedAnswer = value)
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.NotNull(storedAnswer);
        Assert.Contains("validated", storedAnswer);
        Assert.Equal(WorkflowStage.Refinement, output.NextStage);
    }

    [Fact]
    public async Task HandleAsync_FailsWhenClassificationMissing()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new ProductOwnerExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync<string>(
            WorkflowStateKeys.QuestionClassificationResult,
            It.IsAny<Func<string>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Contains("missing classification", output.Notes, StringComparison.OrdinalIgnoreCase);
    }
}
