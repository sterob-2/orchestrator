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
        var checkpoints = new InMemoryWorkflowCheckpointStore();
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
            checkpoints);

        await watcher.RunOnceAsync(CancellationToken.None);

        Assert.True(runner.Called);
        Assert.Equal(workItem, runner.WorkItem);
        Assert.Equal(WorkflowStage.ContextBuilder, runner.Stage);
    }

    [Fact]
    public async Task RunOnceAsync_TriggersRunnerForDorLabel()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.DorLabel });

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        var runner = new TestRunner();
        var checkpoints = new InMemoryWorkflowCheckpointStore();
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
            checkpoints);

        await watcher.RunOnceAsync(CancellationToken.None);

        Assert.True(runner.Called);
        Assert.Equal(WorkflowStage.DoR, runner.Stage);
    }

    [Fact]
    public async Task RunOnceAsync_TriggersRunnerForSpecGateLabel()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.SpecGateLabel });

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        var runner = new TestRunner();
        var checkpoints = new InMemoryWorkflowCheckpointStore();
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
            checkpoints);

        await watcher.RunOnceAsync(CancellationToken.None);

        Assert.True(runner.Called);
        Assert.Equal(WorkflowStage.SpecGate, runner.Stage);
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
        var checkpoints = new Mock<IWorkflowCheckpointStore>();
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
            checkpoints.Object);

        await watcher.RunOnceAsync(CancellationToken.None);

        Assert.False(runner.Called);
        checkpoints.Verify(store => store.Reset(workItem.Number), Times.Once);
        github.Verify(g => g.RemoveLabelsAsync(workItem.Number, It.IsAny<string[]>()), Times.Once);
        github.Verify(g => g.AddLabelsAsync(workItem.Number, config.Labels.WorkItemLabel), Times.Once);
    }

    [Fact]
    public async Task RunAsync_StopsImmediatelyWhenCancelled()
    {
        var config = MockWorkContext.CreateConfig();
        var github = new Mock<IGitHubClient>();
        var runner = new TestRunner();
        var checkpoints = new InMemoryWorkflowCheckpointStore();
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
            checkpoints);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await watcher.RunAsync(cts.Token);

        Assert.False(runner.Called);
    }

    [Fact]
    public async Task RunAsync_ProcessesRequestedScan()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.WorkItemLabel });

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        using var cts = new CancellationTokenSource();
        var runner = new CancelingRunner(cts);
        var checkpoints = new InMemoryWorkflowCheckpointStore();
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
            checkpoints);

        watcher.RequestScan();
        await watcher.RunAsync(cts.Token);

        Assert.True(runner.Called);
    }

    [Fact]
    public async Task RunAsync_SeedsInitialScan()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.WorkItemLabel });

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        using var cts = new CancellationTokenSource();
        var runner = new CancelingRunner(cts);
        var checkpoints = new InMemoryWorkflowCheckpointStore();
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
            checkpoints);

        await watcher.RunAsync(cts.Token);

        Assert.True(runner.Called);
    }

    [Fact]
    public void TryRequestScan_ReturnsFalseAfterChannelCompleted()
    {
        var config = MockWorkContext.CreateConfig();
        var github = new Mock<IGitHubClient>();
        var runner = new TestRunner();
        var checkpoints = new InMemoryWorkflowCheckpointStore();
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
            checkpoints);

        watcher.CompleteScanChannel();

        var accepted = watcher.TryRequestScan();

        Assert.False(accepted);
    }

    [Fact]
    public void RequestScan_LogsWhenChannelClosed()
    {
        var config = MockWorkContext.CreateConfig();
        var github = new Mock<IGitHubClient>();
        var runner = new TestRunner();
        var checkpoints = new InMemoryWorkflowCheckpointStore();
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
            checkpoints);

        watcher.CompleteScanChannel();

        watcher.RequestScan();
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

    private sealed class CancelingRunner : IWorkflowRunner
    {
        private readonly CancellationTokenSource _cts;
        public bool Called { get; private set; }

        public CancelingRunner(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        public Task<WorkflowOutput?> RunAsync(WorkContext context, WorkflowStage stage, CancellationToken cancellationToken)
        {
            Called = true;
            _cts.Cancel();
            return Task.FromResult<WorkflowOutput?>(new WorkflowOutput(true, "ok", WorkflowStageGraph.NextStageFor(stage)));
        }
    }
}
