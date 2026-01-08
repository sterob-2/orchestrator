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

    public GitHubIssueWatcher(
        OrchestratorConfig config,
        IGitHubClient github,
        IWorkflowRunner runner,
        Func<WorkItem, WorkContext> contextFactory,
        IWorkflowCheckpointStore checkpointStore)
    {
        _config = config;
        _github = github;
        _runner = runner;
        _contextFactory = contextFactory;
        _checkpointStore = checkpointStore;
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

        RequestScan();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _scanSignals.Reader.ReadAsync(cancellationToken);

                while (_scanSignals.Reader.TryRead(out _))
                {
                }

                await RunOnceAsync(cancellationToken);
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
        _scanSignals.Writer.TryWrite(true);
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
        if (HasLabel(item, _config.Labels.WorkItemLabel))
        {
            return WorkflowStage.ContextBuilder;
        }

        if (HasLabel(item, _config.Labels.PlannerLabel))
        {
            return WorkflowStage.Refinement;
        }

        if (HasLabel(item, _config.Labels.DorLabel))
        {
            return WorkflowStage.DoR;
        }

        if (HasLabel(item, _config.Labels.TechLeadLabel))
        {
            return WorkflowStage.TechLead;
        }

        if (HasLabel(item, _config.Labels.SpecGateLabel))
        {
            return WorkflowStage.SpecGate;
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
