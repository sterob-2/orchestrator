using System.Threading.Channels;

namespace Orchestrator.App.Watcher;

internal sealed class GitHubIssueWatcher
{
    private readonly OrchestratorConfig _config;
    private readonly IGitHubClient _github;
    private readonly IWorkflowRunner _runner;
    private readonly Func<WorkItem, WorkContext> _contextFactory;
    private readonly IWorkflowCheckpointStore _checkpointStore;
    private readonly Channel<bool> _scanSignals;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public GitHubIssueWatcher(
        OrchestratorConfig config,
        IGitHubClient github,
        IWorkflowRunner runner,
        Func<WorkItem, WorkContext> contextFactory,
        IWorkflowCheckpointStore checkpointStore,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _config = config;
        _github = github;
        _runner = runner;
        _contextFactory = contextFactory;
        _checkpointStore = checkpointStore;
        _delay = delay ?? Task.Delay;
        _scanSignals = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Logger.WriteLine("Orchestrator stopped");
            return;
        }

        var pollingEnabled = _config.Workflow.PollIntervalSeconds > 0;
        Logger.WriteLine(pollingEnabled
            ? $"Watcher started. Polling enabled (idle: {_config.Workflow.PollIntervalSeconds}s, fast: {_config.Workflow.FastPollIntervalSeconds}s), webhook triggers supported."
            : "Watcher started. Polling disabled, waiting for webhook triggers only.");

        Logger.WriteLine($"[Watcher] Watching for label: '{_config.Labels.WorkItemLabel}' (and workflow stage labels)");
        Logger.WriteLine($"[Watcher] Repository: {_config.RepoOwner}/{_config.RepoName}");


        RequestScan(); // Initial scan
        WorkItem? lastWorkItem = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Compute next poll interval based on last work item
                var pollInterval = ComputePollIntervalSeconds(_config, lastWorkItem);

                // Wait for either webhook signal OR polling timer
                Task signalTask = _scanSignals.Reader.ReadAsync(cancellationToken).AsTask();
                Task delayTask = pollingEnabled
                    ? _delay(TimeSpan.FromSeconds(pollInterval), cancellationToken)
                    : _delay(Timeout.InfiniteTimeSpan, cancellationToken); // Never complete if polling disabled

                var completedTask = await Task.WhenAny(signalTask, delayTask);

                // Drain any additional pending webhook signals (coalesce)
                if (completedTask == signalTask)
                {
                    while (_scanSignals.Reader.TryRead(out _))
                    {
                        // Coalesce multiple webhook triggers
                    }
                }

                // Run the scan
                lastWorkItem = await RunOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                Logger.WriteLine(ex.ToString());
            }
            catch (TimeoutException ex)
            {
                Logger.WriteLine(ex.ToString());
            }
        }

        Logger.WriteLine("Orchestrator stopped");
    }

    public void RequestScan()
    {
        if (!TryRequestScan())
        {
            Logger.WriteLine("Watcher trigger dropped.");
        }
    }

    internal bool TryRequestScan()
    {
        return _scanSignals.Writer.TryWrite(true);
    }

    internal void CompleteScanChannel()
    {
        _scanSignals.Writer.TryComplete();
    }

    internal async Task<WorkItem?> RunOnceAsync(CancellationToken cancellationToken)
    {
        var workItem = await GetNextWorkItemAsync();
        if (workItem is null)
        {
            Logger.WriteLine("No matching work items");
            return null;
        }

        if (HasLabel(workItem, _config.Labels.ResetLabel))
        {
            await ResetWorkItemAsync(workItem);
            return workItem;
        }

        // Check if this workflow is already in progress
        if (_checkpointStore.IsWorkflowInProgress(workItem.Number))
        {
            Logger.WriteLine($"[Watcher] Issue #{workItem.Number} already has a workflow in progress, skipping");
            return workItem;
        }

        var stage = GetStageFromLabels(workItem);
        if (stage is null)
        {
            Logger.WriteLine($"No runnable stage for work item #{workItem.Number}.");
            return workItem;
        }

        var context = _contextFactory(workItem);
        await _runner.RunAsync(context, stage.Value, cancellationToken);
        return workItem;
    }

    private async Task<WorkItem?> GetNextWorkItemAsync()
    {
        var items = await _github.GetOpenWorkItemsAsync();
        Logger.WriteLine($"[Watcher] Fetched {items.Count} open issue(s) from GitHub");

        foreach (var item in items)
        {
            var labelsStr = string.Join(", ", item.Labels);
            Logger.WriteLine($"[Watcher] Checking issue #{item.Number}: '{item.Title}' (labels: {labelsStr})");

            if (HasLabel(item, _config.Labels.DoneLabel) || HasLabel(item, _config.Labels.BlockedLabel))
            {
                Logger.WriteLine($"[Watcher]   -> Skipped: Issue is done or blocked");
                continue;
            }

            if (HasAnyLabel(
                item,
                _config.Labels.WorkItemLabel,
                _config.Labels.PlannerLabel,
                _config.Labels.DorLabel,
                _config.Labels.TechLeadLabel,
                _config.Labels.SpecGateLabel,
                _config.Labels.DevLabel,
                _config.Labels.TestLabel,
                _config.Labels.ReleaseLabel,
                _config.Labels.CodeReviewNeededLabel,
                _config.Labels.CodeReviewChangesRequestedLabel,
                _config.Labels.ResetLabel))
            {
                Logger.WriteLine($"[Watcher]   -> Selected: Issue has matching workflow label");
                return item;
            }

            Logger.WriteLine($"[Watcher]   -> Skipped: No matching workflow labels (looking for: {_config.Labels.WorkItemLabel}, {_config.Labels.PlannerLabel}, ...)");
        }

        return null;
    }

    private async Task ResetWorkItemAsync(WorkItem item)
    {
        _checkpointStore.Reset(item.Number);

        var labelsToRemove = new[]
        {
            _config.Labels.PlannerLabel,
            _config.Labels.DorLabel,
            _config.Labels.TechLeadLabel,
            _config.Labels.SpecGateLabel,
            _config.Labels.DevLabel,
            _config.Labels.TestLabel,
            _config.Labels.ReleaseLabel,
            _config.Labels.CodeReviewNeededLabel,
            _config.Labels.CodeReviewApprovedLabel,
            _config.Labels.CodeReviewChangesRequestedLabel,
            _config.Labels.InProgressLabel,
            _config.Labels.UserReviewRequiredLabel,
            _config.Labels.ReviewNeededLabel,
            _config.Labels.ReviewedLabel,
            _config.Labels.ResetLabel
        };

        await _github.RemoveLabelsAsync(item.Number, labelsToRemove);
        await _github.AddLabelsAsync(item.Number, _config.Labels.WorkItemLabel);
    }

    private static int ComputePollIntervalSeconds(OrchestratorConfig cfg, WorkItem? workItem)
    {
        if (workItem == null)
        {
            return cfg.Workflow.PollIntervalSeconds;
        }

        // Check if work item has any "active" labels indicating work in progress
        if (HasAnyLabel(
            workItem,
            cfg.Labels.PlannerLabel,
            cfg.Labels.DorLabel,
            cfg.Labels.TechLeadLabel,
            cfg.Labels.SpecGateLabel,
            cfg.Labels.DevLabel,
            cfg.Labels.TestLabel,
            cfg.Labels.ReleaseLabel,
            cfg.Labels.CodeReviewNeededLabel,
            cfg.Labels.CodeReviewChangesRequestedLabel))
        {
            return cfg.Workflow.FastPollIntervalSeconds;
        }

        return cfg.Workflow.PollIntervalSeconds;
    }

    private static bool HasLabel(WorkItem item, string label)
    {
        return item.Labels.Contains(label, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasAnyLabel(WorkItem item, params string[] labels)
    {
        return labels.Any(label => HasLabel(item, label));
    }

    private WorkflowStage? GetStageFromLabels(WorkItem item)
    {
        Logger.Debug($"[Watcher] Determining stage from labels for issue #{item.Number}");
        Logger.Debug($"[Watcher] Labels: {string.Join(", ", item.Labels)}");
        Logger.Debug($"[Watcher] Checking DevLabel='{_config.Labels.DevLabel}', HasLabel={HasLabel(item, _config.Labels.DevLabel)}");

        // Check stage-specific labels FIRST before generic ready-for-agents
        // This prevents race conditions where both labels might be present temporarily
        if (HasLabel(item, _config.Labels.ReleaseLabel))
        {
            return WorkflowStage.Release;
        }

        if (HasLabel(item, _config.Labels.CodeReviewNeededLabel) || HasLabel(item, _config.Labels.CodeReviewChangesRequestedLabel))
        {
            return WorkflowStage.CodeReview;
        }

        if (HasLabel(item, _config.Labels.TestLabel))
        {
            return WorkflowStage.DoD;
        }

        if (HasLabel(item, _config.Labels.DevLabel))
        {
            Logger.Debug($"[Watcher] Selected stage: Dev (from DevLabel='{_config.Labels.DevLabel}')");
            return WorkflowStage.Dev;
        }

        if (HasLabel(item, _config.Labels.SpecGateLabel))
        {
            return WorkflowStage.SpecGate;
        }

        if (HasLabel(item, _config.Labels.TechLeadLabel))
        {
            return WorkflowStage.TechLead;
        }

        if (HasLabel(item, _config.Labels.DorLabel))
        {
            return WorkflowStage.DoR;
        }

        if (HasLabel(item, _config.Labels.PlannerLabel))
        {
            return WorkflowStage.Refinement;
        }

        // Check generic ready-for-agents label LAST as fallback
        if (HasLabel(item, _config.Labels.WorkItemLabel))
        {
            Logger.Debug($"[Watcher] Selected stage: ContextBuilder (from WorkItemLabel='{_config.Labels.WorkItemLabel}')");
            return WorkflowStage.ContextBuilder;
        }

        Logger.Debug($"[Watcher] No matching stage label found, returning null");
        return null;
    }
}
