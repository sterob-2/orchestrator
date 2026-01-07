using Moq;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Watcher;

public class GitHubIssueWatcherTests
{
    [Fact]
    public async Task RunOnceAsync_TriggersRunnerForWorkItem()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.WorkItemLabel });

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        var runner = new TestRunner();
        var watcher = new GitHubIssueWatcher(
            config,
            github.Object,
            runner,
            item => new WorkContext(
                item,
                github.Object,
                config,
                new Mock<IRepoWorkspace>().Object,
                new Mock<IRepoGit>().Object,
                new Mock<ILlmClient>().Object),
            (_, _) => Task.CompletedTask);

        await watcher.RunOnceAsync(CancellationToken.None);

        Assert.True(runner.Called);
        Assert.Equal(workItem, runner.WorkItem);
        Assert.Equal(WorkflowStage.Refinement, runner.Stage);
    }

    [Fact]
    public async Task RunOnceAsync_ResetsWorkItemWhenResetLabelPresent()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string>
        {
            config.Labels.ResetLabel,
            config.Labels.DevLabel
        });

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });
        github.Setup(g => g.RemoveLabelsAsync(It.IsAny<int>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        github.Setup(g => g.AddLabelsAsync(It.IsAny<int>(), It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);

        var runner = new TestRunner();
        var watcher = new GitHubIssueWatcher(
            config,
            github.Object,
            runner,
            item => new WorkContext(
                item,
                github.Object,
                config,
                new Mock<IRepoWorkspace>().Object,
                new Mock<IRepoGit>().Object,
                new Mock<ILlmClient>().Object),
            (_, _) => Task.CompletedTask);

        await watcher.RunOnceAsync(CancellationToken.None);

        Assert.False(runner.Called);
        github.Verify(g => g.RemoveLabelsAsync(workItem.Number, It.IsAny<string[]>()), Times.Once);
        github.Verify(g => g.AddLabelsAsync(workItem.Number, config.Labels.WorkItemLabel), Times.Once);
    }

    private sealed class TestRunner : IWorkflowRunner
    {
        public bool Called { get; private set; }
        public WorkItem? WorkItem { get; private set; }
        public WorkflowStage Stage { get; private set; }

        public Task<WorkflowOutput?> RunAsync(WorkContext context, WorkflowStage stage, CancellationToken cancellationToken)
        {
            Called = true;
            WorkItem = context.WorkItem;
            Stage = stage;
            return Task.FromResult<WorkflowOutput?>(new WorkflowOutput(true, "ok", WorkflowStageGraph.NextStageFor(stage)));
        }
    }
}
