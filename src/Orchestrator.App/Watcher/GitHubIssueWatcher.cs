namespace Orchestrator.App.Watcher;

internal sealed class GitHubIssueWatcher
{
    private readonly OrchestratorConfig _config;
    private readonly IGitHubClient _github;
    private readonly IWorkflowRunner _runner;
    private readonly Func<WorkItem, WorkContext> _contextFactory;
    private readonly IWorkflowCheckpointStore _checkpointStore;
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
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        WorkItem? lastWorkItem = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                lastWorkItem = await RunOnceAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.ToString());
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var interval = ComputePollIntervalSeconds(_config, lastWorkItem);
                await _delay(TimeSpan.FromSeconds(interval), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        Logger.WriteLine("Orchestrator stopped");
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
        foreach (var item in items)
        {
            if (HasLabel(item, _config.Labels.DoneLabel) || HasLabel(item, _config.Labels.BlockedLabel))
            {
                continue;
            }

            if (HasAnyLabel(
                item,
                _config.Labels.WorkItemLabel,
                _config.Labels.PlannerLabel,
                _config.Labels.TechLeadLabel,
                _config.Labels.DevLabel,
                _config.Labels.TestLabel,
                _config.Labels.ReleaseLabel,
                _config.Labels.CodeReviewNeededLabel,
                _config.Labels.CodeReviewChangesRequestedLabel,
                _config.Labels.ResetLabel))
            {
                return item;
            }
        }

        return null;
    }

    private async Task ResetWorkItemAsync(WorkItem item)
    {
        _checkpointStore.Reset(item.Number);

        var labelsToRemove = new[]
        {
            _config.Labels.PlannerLabel,
            _config.Labels.TechLeadLabel,
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

        if (HasAnyLabel(
            workItem,
            cfg.Labels.PlannerLabel,
            cfg.Labels.TechLeadLabel,
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
        foreach (var label in labels)
        {
            if (HasLabel(item, label))
            {
                return true;
            }
        }

        return false;
    }

    private WorkflowStage? GetStageFromLabels(WorkItem item)
    {
        if (HasLabel(item, _config.Labels.WorkItemLabel) || HasLabel(item, _config.Labels.PlannerLabel))
        {
            return WorkflowStage.Refinement;
        }

        if (HasLabel(item, _config.Labels.TechLeadLabel))
        {
            return WorkflowStage.TechLead;
        }

        if (HasLabel(item, _config.Labels.DevLabel))
        {
            return WorkflowStage.Dev;
        }

        if (HasLabel(item, _config.Labels.TestLabel))
        {
            return WorkflowStage.DoD;
        }

        if (HasLabel(item, _config.Labels.CodeReviewNeededLabel) || HasLabel(item, _config.Labels.CodeReviewChangesRequestedLabel))
        {
            return WorkflowStage.CodeReview;
        }

        if (HasLabel(item, _config.Labels.ReleaseLabel))
        {
            return WorkflowStage.Release;
        }

        return null;
    }
}
