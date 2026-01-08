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
        var runner = new WorkflowRunner(labelSync, humanInLoop);

        var output = await runner.RunAsync(context, WorkflowStage.Dev, CancellationToken.None);

        Assert.NotNull(output);
    }

    [Fact]
    public async Task RunAsync_WhenIterationLimitReached_ReturnsBlockedOutput()
    {
        var config = MockWorkContext.CreateConfig();
        config = config with
        {
            Workflow = config.Workflow with { MaxDevIterations = 0 }
        };
        var workItem = new WorkItem(2, "Title", "Body", "url", new List<string>());
        var github = new Mock<IGitHubClient>();
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);
        var workflow = WorkflowFactory.Build(WorkflowStage.Dev, context);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", "/tmp", "/tmp", "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

        Assert.NotNull(output);
        Assert.False(output!.Success);
        Assert.Contains("Iteration limit reached", output.Notes);
    }
}
