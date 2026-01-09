using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Workflows;

public class QuestionClassifierExecutorTests
{
    [Fact]
    public async Task HandleAsync_ClassifiesTechnicalQuestion()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // Setup LLM to return technical classification
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"Question\":\"Which framework should we use?\",\"Type\":\"Technical\",\"Reasoning\":\"Question about technology choice\"}");

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        // Setup refinement result with open question
        var refinementResult = "{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[\"Which framework should we use?\"],\"complexitySignals\":[],\"complexitySummary\":\"low\"}";
        workContext.State[WorkflowStateKeys.RefinementResult] = refinementResult;

        var executor = new QuestionClassifierExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refinementResult);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        string? storedClassification = null;
        workflowContext.Setup(c => c.QueueStateUpdateAsync(WorkflowStateKeys.QuestionClassificationResult, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, value, _) => storedClassification = value)
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.NotNull(storedClassification);
        Assert.Contains("Technical", storedClassification);
        Assert.Equal(WorkflowStage.TechnicalAdvisor, output.NextStage);
    }

    [Fact]
    public async Task HandleAsync_ClassifiesProductQuestion()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // Setup LLM to return product classification
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"Question\":\"What happens when user clicks submit?\",\"Type\":\"Product\",\"Reasoning\":\"Question about user workflow\"}");

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        // Setup refinement result with open question
        var refinementResult = "{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[\"What happens when user clicks submit?\"],\"complexitySignals\":[],\"complexitySummary\":\"low\"}";
        workContext.State[WorkflowStateKeys.RefinementResult] = refinementResult;

        var executor = new QuestionClassifierExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refinementResult);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Equal(WorkflowStage.ProductOwner, output.NextStage);
    }

    [Fact]
    public async Task HandleAsync_BlocksOnAmbiguousQuestion()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // Setup LLM to return ambiguous classification
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"Question\":\"Should we do this?\",\"Type\":\"Ambiguous\",\"Reasoning\":\"Question is unclear\"}");

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        // Setup refinement result with open question
        var refinementResult = "{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[\"Should we do this?\"],\"complexitySignals\":[],\"complexitySummary\":\"low\"}";
        workContext.State[WorkflowStateKeys.RefinementResult] = refinementResult;

        var executor = new QuestionClassifierExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refinementResult);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Null(output.NextStage); // Should block - no next stage
    }
}
