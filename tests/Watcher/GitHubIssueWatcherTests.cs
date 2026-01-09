using Moq;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Watcher;
using Orchestrator.App.Workflows;
using Orchestrator.App.Workflows.Executors;

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

        var exception = Record.Exception(() => watcher.RequestScan());
        Assert.Null(exception);
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

    [Fact]
    public async Task RunAsync_PollsWhenNoWebhookTrigger()
    {
        var config = MockWorkContext.CreateConfig() with
        {
            Workflow = MockWorkContext.CreateConfig().Workflow with
            {
                PollIntervalSeconds = 1,
                FastPollIntervalSeconds = 1
            }
        };
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
            checkpoints,
            delay: (_, ct) => Task.CompletedTask); // Instant "delay" for testing

        await watcher.RunAsync(cts.Token);

        Assert.True(runner.Called);
    }

    [Fact]
    public async Task RunAsync_UsesSlowPollingWhenNoWorkItem()
    {
        var config = MockWorkContext.CreateConfig() with
        {
            Workflow = MockWorkContext.CreateConfig().Workflow with
            {
                PollIntervalSeconds = 60,
                FastPollIntervalSeconds = 10
            }
        };

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem>()); // No work items

        using var cts = new CancellationTokenSource();
        var runner = new CancelingRunner(cts);
        var checkpoints = new InMemoryWorkflowCheckpointStore();

        var delayDuration = TimeSpan.Zero;
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
            checkpoints,
            delay: (duration, ct) =>
            {
                delayDuration = duration;
                cts.Cancel(); // Cancel after first delay
                return Task.CompletedTask;
            });

        await watcher.RunAsync(cts.Token);

        Assert.Equal(TimeSpan.FromSeconds(60), delayDuration); // Should use slow interval
    }

    [Fact]
    public async Task RunAsync_UsesFastPollingWhenActiveWorkItem()
    {
        var config = MockWorkContext.CreateConfig() with
        {
            Workflow = MockWorkContext.CreateConfig().Workflow with
            {
                PollIntervalSeconds = 60,
                FastPollIntervalSeconds = 10
            }
        };

        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.DevLabel });
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        using var cts = new CancellationTokenSource();
        var delayDurations = new List<TimeSpan>();
        var runner = new Mock<IWorkflowRunner>();
        runner.Setup(r => r.RunAsync(It.IsAny<WorkContext>(), It.IsAny<WorkflowStage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowOutput?)null)
            .Callback(() =>
            {
                // Cancel after second delay is computed (after first scan completes)
                if (delayDurations.Count >= 2) cts.Cancel();
            });

        var checkpoints = new InMemoryWorkflowCheckpointStore();
        var watcher = new GitHubIssueWatcher(
            config,
            github.Object,
            runner.Object,
            item => new WorkContext(
                item,
                github.Object,
                config,
                new Mock<IRepoWorkspace>().Object,
                new Mock<IRepoGit>().Object,
                new Mock<ILlmClient>().Object),
            checkpoints,
            delay: (duration, ct) =>
            {
                delayDurations.Add(duration);
                return Task.CompletedTask;
            });

        await watcher.RunAsync(cts.Token);

        // First delay is 60s (no previous work), second delay is 10s (after finding work)
        Assert.Equal(2, delayDurations.Count);
        Assert.Equal(TimeSpan.FromSeconds(60), delayDurations[0]); // Slow before first scan
        Assert.Equal(TimeSpan.FromSeconds(10), delayDurations[1]); // Fast after finding active work
    }

    [Fact]
    public async Task RunAsync_UsesFastPollingForCodeReviewLabel()
    {
        var config = MockWorkContext.CreateConfig() with
        {
            Workflow = MockWorkContext.CreateConfig().Workflow with
            {
                PollIntervalSeconds = 60,
                FastPollIntervalSeconds = 10
            }
        };

        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.CodeReviewNeededLabel });
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        using var cts = new CancellationTokenSource();
        var delayDurations = new List<TimeSpan>();
        var runner = new Mock<IWorkflowRunner>();
        runner.Setup(r => r.RunAsync(It.IsAny<WorkContext>(), It.IsAny<WorkflowStage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowOutput?)null)
            .Callback(() =>
            {
                // Cancel after second delay is computed (after first scan completes)
                if (delayDurations.Count >= 2) cts.Cancel();
            });

        var checkpoints = new InMemoryWorkflowCheckpointStore();
        var watcher = new GitHubIssueWatcher(
            config,
            github.Object,
            runner.Object,
            item => new WorkContext(
                item,
                github.Object,
                config,
                new Mock<IRepoWorkspace>().Object,
                new Mock<IRepoGit>().Object,
                new Mock<ILlmClient>().Object),
            checkpoints,
            delay: (duration, ct) =>
            {
                delayDurations.Add(duration);
                return Task.CompletedTask;
            });

        await watcher.RunAsync(cts.Token);

        // First delay is 60s (no previous work), second delay is 10s (after finding code review work)
        Assert.Equal(2, delayDurations.Count);
        Assert.Equal(TimeSpan.FromSeconds(60), delayDurations[0]);
        Assert.Equal(TimeSpan.FromSeconds(10), delayDurations[1]);
    }

    [Fact]
    public async Task RunAsync_DisablesPollingWhenIntervalZero()
    {
        var config = MockWorkContext.CreateConfig() with
        {
            Workflow = MockWorkContext.CreateConfig().Workflow with
            {
                PollIntervalSeconds = 0, // Polling disabled
                FastPollIntervalSeconds = 0
            }
        };

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem>()); // No work items

        using var cts = new CancellationTokenSource();
        var checkpoints = new InMemoryWorkflowCheckpointStore();
        var runner = new Mock<IWorkflowRunner>();

        var delayCalledWithInfinite = false;
        var tcs = new TaskCompletionSource<bool>();

        var watcher = new GitHubIssueWatcher(
            config,
            github.Object,
            runner.Object,
            item => new WorkContext(
                item,
                github.Object,
                config,
                new Mock<IRepoWorkspace>().Object,
                new Mock<IRepoGit>().Object,
                new Mock<ILlmClient>().Object),
            checkpoints,
            delay: async (duration, ct) =>
            {
                if (duration == Timeout.InfiniteTimeSpan && !delayCalledWithInfinite)
                {
                    delayCalledWithInfinite = true;
                    cts.Cancel(); // Cancel to exit
                }
                await tcs.Task; // Wait until we're done testing
            });

        var runTask = watcher.RunAsync(cts.Token);

        // Wait a bit for the delay to be called
        await Task.Delay(100);
        tcs.SetResult(true); // Allow delay to complete

        await runTask;

        // When polling is disabled, delay should be called with Timeout.Infinite
        Assert.True(delayCalledWithInfinite);
    }

    [Fact]
    public async Task RunAsync_HybridMode_RespondsToWebhookDuringPolling()
    {
        var config = MockWorkContext.CreateConfig() with
        {
            Workflow = MockWorkContext.CreateConfig().Workflow with
            {
                PollIntervalSeconds = 300, // Long delay
                FastPollIntervalSeconds = 300
            }
        };

        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.WorkItemLabel });
        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        using var cts = new CancellationTokenSource();
        var runner = new CancelingRunner(cts);
        var checkpoints = new InMemoryWorkflowCheckpointStore();

        GitHubIssueWatcher? watcher = null;
        watcher = new GitHubIssueWatcher(
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
            checkpoints,
            delay: async (duration, ct) =>
            {
                // Simulate webhook trigger during polling delay
                await Task.Delay(10, ct);
                watcher!.RequestScan();
                await Task.Delay(Timeout.Infinite, ct); // Would wait forever without webhook
            });

        await watcher.RunAsync(cts.Token);

        Assert.True(runner.Called); // Should process webhook trigger, not wait for polling
    }

    [Fact]
    public async Task RunAsync_CoalescesMultipleWebhookTriggers()
    {
        var config = MockWorkContext.CreateConfig();
        var workItem = MockWorkContext.CreateWorkItem(labels: new List<string> { config.Labels.WorkItemLabel });

        var github = new Mock<IGitHubClient>();
        github.Setup(g => g.GetOpenWorkItemsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<WorkItem> { workItem });

        using var cts = new CancellationTokenSource();
        var runCount = 0;
        var runner = new Mock<IWorkflowRunner>();
        runner.Setup(r => r.RunAsync(It.IsAny<WorkContext>(), It.IsAny<WorkflowStage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowOutput?)null)
            .Callback(() =>
            {
                runCount++;
                if (runCount >= 1) cts.Cancel();
            });

        var checkpoints = new InMemoryWorkflowCheckpointStore();
        var watcher = new GitHubIssueWatcher(
            config,
            github.Object,
            runner.Object,
            item => new WorkContext(
                item,
                github.Object,
                config,
                new Mock<IRepoWorkspace>().Object,
                new Mock<IRepoGit>().Object,
                new Mock<ILlmClient>().Object),
            checkpoints);

        // Queue multiple webhook triggers
        watcher.RequestScan();
        watcher.RequestScan();
        watcher.RequestScan();

        await watcher.RunAsync(cts.Token);

        // Should only run once despite multiple triggers
        Assert.Equal(1, runCount);
    }
}
