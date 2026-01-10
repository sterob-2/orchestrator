using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows.Executors;

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

    [Fact]
    public async Task HandleAsync_ContinuesWhenGitCommitFailsWithLibGit2Exception()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Throws(new LibGit2Sharp.LibGit2SharpException("Git error"));
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

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success); // Should continue despite git error
    }

    [Fact]
    public async Task HandleAsync_ContinuesWhenGitCommitFailsWithInvalidOperationException()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        repo.Setup(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Throws(new InvalidOperationException("Git operation error"));
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

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success); // Should continue despite git error
    }

    [Fact]
    public async Task HandleAsync_UsesFallbackWhenLlmResponseInvalid()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body with acceptance criteria:\n- Criterion 1\n- Criterion 2", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("invalid json response");
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

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success); // Should use fallback and succeed
    }

    [Fact]
    public async Task HandleAsync_RoutesToQuestionClassifierWhenQuestionsExist()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // Return refinement with open questions
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[\"Which framework?\"],\"complexitySignals\":[],\"complexitySummary\":\"low\"}");

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

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Equal(WorkflowStage.QuestionClassifier, output.NextStage);
    }

    [Fact]
    public async Task HandleAsync_RoutesToDorWhenNoQuestions()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // Return refinement with NO open questions
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[],\"complexitySignals\":[],\"complexitySummary\":\"low\",\"answeredQuestions\":[]}");

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

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Equal(WorkflowStage.DoR, output.NextStage);
    }

    [Fact]
    public async Task HandleAsync_BlocksAfterTwoFailedAttempts()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // Return refinement with same question (indicating failed answer attempt)
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[\"Which framework?\"],\"complexitySignals\":[],\"complexitySummary\":\"low\"}");

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        // Simulate second attempt (same question persists)
        workContext.State[WorkflowStateKeys.LastProcessedQuestion] = "Which framework?";
        workContext.State[WorkflowStateKeys.QuestionAttemptCount] = "1";

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

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.Null(output.NextStage); // Should block after 2 attempts
    }

    [Fact]
    public async Task HandleAsync_IncorporatesAnswerFromPreviousStage()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // LLM should receive the answer as a synthetic comment
        string? capturedUser = null;
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((model, system, user) => capturedUser = user)
            .ReturnsAsync("{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[],\"complexitySignals\":[],\"complexitySummary\":\"low\",\"answeredQuestions\":[]}");

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var executor = new RefinementExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        // Simulate answer from TechnicalAdvisor with the question context
        var workflowContext = new Mock<IWorkflowContext>();

        // Setup answer data to be read from IWorkflowContext
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.CurrentQuestionAnswer, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Use React framework");
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.LastProcessedQuestion, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Which framework should we use?");
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.LastProcessedQuestionNumber, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1");
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.TechnicalAdvisorResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.ProductOwnerResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("");

        // Other string state reads return empty
        workflowContext.Setup(c => c.ReadOrInitStateAsync(
            It.Is<string>(key => key != WorkflowStateKeys.CurrentQuestionAnswer &&
                                key != WorkflowStateKeys.LastProcessedQuestion &&
                                key != WorkflowStateKeys.LastProcessedQuestionNumber &&
                                key != WorkflowStateKeys.TechnicalAdvisorResult &&
                                key != WorkflowStateKeys.ProductOwnerResult),
            It.IsAny<Func<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("");

        workflowContext.Setup(c => c.ReadOrInitStateAsync(It.IsAny<string>(), It.IsAny<Func<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.True(output.Success);
        Assert.NotNull(capturedUser);

        // The answer should have been incorporated in markdown checkbox format
        var hasAnswer = capturedUser.Contains("Use React framework");
        var hasCheckbox = capturedUser.Contains("- [x]");
        var hasAnswerSource = capturedUser.Contains("(TechnicalAdvisor)");

        Assert.True(hasAnswer, $"Expected 'Use React framework' in prompt. Got: {capturedUser.Substring(0, Math.Min(1000, capturedUser.Length))}");
        Assert.True(hasCheckbox, "Expected checkbox format '- [x]' in prompt");
        Assert.True(hasAnswerSource, "Expected '(TechnicalAdvisor)' indicating answer source in prompt");

        // Verify answer was cleared from IWorkflowContext
        workflowContext.Verify(c => c.QueueStateUpdateAsync(
            WorkflowStateKeys.CurrentQuestionAnswer,
            "",
            It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_BlocksAndPostsCommentWhenAllQuestionsAmbiguous()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();

        // Setup LLM to return refinement with NO open questions (they've all been processed)
        var llmRefinementJson = "{\"clarifiedStory\":\"Story\",\"acceptanceCriteria\":[\"Given X\"],\"openQuestions\":[],\"complexitySignals\":[],\"complexitySummary\":\"low\"}";
        llm.Setup(l => l.GetUpdatedFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(llmRefinementJson);

        var workContext = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        // Setup previous refinement result with an ambiguous question (from a previous workflow run)
        var previousRefinementJson = WorkflowJson.Serialize(new RefinementResult(
            "Previous Story",
            new List<string> { "AC1" },
            new List<OpenQuestion>(),
            new ComplexityIndicators(new List<string>(), "low"),
            AnsweredQuestions: null,
            AmbiguousQuestions: new List<OpenQuestion> { new(1, "Ambiguous question?") }));

        var workflowContext = new Mock<IWorkflowContext>();

        // Return the previous refinement when reading RefinementResult
        workflowContext.Setup(c => c.ReadOrInitStateAsync(WorkflowStateKeys.RefinementResult, It.IsAny<Func<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousRefinementJson);

        // Other state reads return empty
        workflowContext.Setup(c => c.ReadOrInitStateAsync(
            It.Is<string>(key => key != WorkflowStateKeys.RefinementResult),
            It.IsAny<Func<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("");

        workflowContext.Setup(c => c.ReadOrInitStateAsync(It.IsAny<string>(), It.IsAny<Func<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.QueueStateUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        workflowContext.Setup(c => c.SendMessageAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var executor = new RefinementExecutor(workContext, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var output = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        Assert.False(output.Success);
        Assert.Null(output.NextStage);
        Assert.Contains("ambiguous", output.Notes, StringComparison.OrdinalIgnoreCase);

        // Verify GitHub comment was posted
        github.Verify(g => g.CommentOnWorkItemAsync(
            1,
            It.Is<string>(s => s.Contains("Ambiguous Questions"))),
            Times.Once);
    }
}
