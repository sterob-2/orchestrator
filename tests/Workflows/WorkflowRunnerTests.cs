using Moq;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class WorkflowRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsExpectedOutput()
    {
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var config = OrchestratorConfig.FromEnvironment();
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        github.Setup(g => g.RemoveLabelsAsync(It.IsAny<int>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        github.Setup(g => g.AddLabelsAsync(It.IsAny<int>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        var workspace = new Mock<IRepoWorkspace>(MockBehavior.Strict);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var labelSync = new LabelSyncHandler(github.Object, config.Labels);
        var humanInLoop = new HumanInLoopHandler(github.Object, config.Labels);
        var checkpoints = new InMemoryWorkflowCheckpointStore();
        var runner = new WorkflowRunner(labelSync, humanInLoop, checkpoints);

        var output = await runner.RunAsync(context, WorkflowStage.Dev, CancellationToken.None);

        Assert.NotNull(output);
        Assert.Equal(WorkflowStage.CodeReview, output!.NextStage);
    }

    [Fact]
    public async Task RunAsync_WhenIterationLimitReached_ReturnsBlockedOutput()
    {
        var workItem = new WorkItem(2, "Title", "Body", "url", new List<string>());
        var config = MockWorkContext.CreateConfig();
        config = config with
        {
            Workflow = config.Workflow with { MaxDevIterations = 1 }
        };
        var github = new Mock<IGitHubClient>(MockBehavior.Strict);
        github.Setup(g => g.RemoveLabelsAsync(It.IsAny<int>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        github.Setup(g => g.AddLabelsAsync(It.IsAny<int>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        var workspace = new Mock<IRepoWorkspace>(MockBehavior.Strict);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var labelSync = new LabelSyncHandler(github.Object, config.Labels);
        var humanInLoop = new HumanInLoopHandler(github.Object, config.Labels);
        var checkpoints = new InMemoryWorkflowCheckpointStore();
        var runner = new WorkflowRunner(labelSync, humanInLoop, checkpoints);

        var first = await runner.RunAsync(context, WorkflowStage.Dev, CancellationToken.None);
        var second = await runner.RunAsync(context, WorkflowStage.Dev, CancellationToken.None);

        Assert.NotNull(first);
        Assert.True(first!.Success);
        Assert.NotNull(second);
        Assert.False(second!.Success);
        Assert.Contains("Iteration limit reached", second.Notes);
        github.Verify(
            g => g.AddLabelsAsync(workItem.Number, config.Labels.BlockedLabel, config.Labels.UserReviewRequiredLabel),
            Times.Once);
    }
}
