using Moq;

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
        var humanInLoop = new HumanInLoopHandler();
        var runner = new WorkflowRunner(labelSync, humanInLoop);

        var output = await runner.RunAsync(context, WorkflowStage.Dev, CancellationToken.None);

        Assert.NotNull(output);
        Assert.Equal(WorkflowStage.CodeReview, output!.NextStage);
    }
}
