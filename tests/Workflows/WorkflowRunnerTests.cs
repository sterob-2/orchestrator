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
        github.Setup(g => g.GetIssueCommentsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<IReadOnlyList<IssueComment>>(Array.Empty<IssueComment>()));
        var workspace = new Mock<IRepoWorkspace>(MockBehavior.Strict);
        workspace.Setup(w => w.Exists(It.IsAny<string>())).Returns(false);
        workspace.Setup(w => w.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        workspace.Setup(w => w.ReadOrTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(string.Empty);
        var repo = new Mock<IRepoGit>(MockBehavior.Strict);
        repo.Setup(r => r.EnsureBranch(It.IsAny<string>(), It.IsAny<string>()));
        repo.Setup(r => r.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(false);
        var llm = new Mock<ILlmClient>(MockBehavior.Strict);
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);

        var labelSync = new LabelSyncHandler(github.Object, config.Labels);
        var humanInLoop = new HumanInLoopHandler(github.Object, config.Labels);
        var metricsStore = new InMemoryWorkflowMetricsStore();
        var checkpoints = new InMemoryWorkflowCheckpointStore();
        var runner = new WorkflowRunner(labelSync, humanInLoop, metricsStore, checkpoints);

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
        github.Setup(g => g.RemoveLabelsAsync(It.IsAny<int>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        github.Setup(g => g.AddLabelsAsync(It.IsAny<int>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        var workspace = new Mock<IRepoWorkspace>();
        var repo = new Mock<IRepoGit>();
        var llm = new Mock<ILlmClient>();
        var context = new WorkContext(workItem, github.Object, config, workspace.Object, repo.Object, llm.Object);
        var labelSync = new LabelSyncHandler(github.Object, config.Labels);
        var humanInLoop = new HumanInLoopHandler(github.Object, config.Labels);
        var metricsStore = new InMemoryWorkflowMetricsStore();
        var checkpoints = new InMemoryWorkflowCheckpointStore();
        var runner = new WorkflowRunner(labelSync, humanInLoop, metricsStore, checkpoints);

        var output = await runner.RunAsync(context, WorkflowStage.Dev, CancellationToken.None);

        Assert.NotNull(output);
        Assert.False(output!.Success);
        Assert.Contains("Iteration limit reached", output.Notes);
    }
}
