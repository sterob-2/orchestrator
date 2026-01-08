namespace Orchestrator.App.Workflows;

internal sealed class LabelSyncHandler
{
    private readonly IGitHubClient _github;
    private readonly LabelConfig _labels;

    public LabelSyncHandler(IGitHubClient github, LabelConfig labels)
    {
        _github = github;
        _labels = labels;
    }

    public async Task ApplyAsync(WorkItem item, WorkflowOutput output)
    {
        if (output.NextStage is null)
        {
            if (output.Success)
            {
                await ResetStageLabelsAsync(item.Number);
                await _github.AddLabelsAsync(item.Number, _labels.DoneLabel);
            }

            return;
        }

        var nextLabel = StageLabel(output.NextStage.Value);
        var labelsToRemove = StageLabels()
            .Where(label => !string.Equals(label, nextLabel, StringComparison.OrdinalIgnoreCase))
            .Concat(new[] { _labels.WorkItemLabel, _labels.ResetLabel })
            .ToArray();

        await _github.RemoveLabelsAsync(item.Number, labelsToRemove);
        await _github.AddLabelsAsync(item.Number, _labels.InProgressLabel, nextLabel);
    }

    public async Task ResetStageLabelsAsync(int issueNumber)
    {
        await _github.RemoveLabelsAsync(issueNumber, StageLabels());
        await _github.RemoveLabelsAsync(issueNumber, _labels.InProgressLabel, _labels.ResetLabel);
    }

    private string StageLabel(WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.Refinement => _labels.PlannerLabel,
            WorkflowStage.DoR => _labels.DorLabel,
            WorkflowStage.TechLead => _labels.TechLeadLabel,
            WorkflowStage.SpecGate => _labels.SpecGateLabel,
            WorkflowStage.Dev => _labels.DevLabel,
            WorkflowStage.CodeReview => _labels.CodeReviewNeededLabel,
            WorkflowStage.DoD => _labels.TestLabel,
            WorkflowStage.Release => _labels.ReleaseLabel,
            _ => _labels.DevLabel
        };
    }

    private string[] StageLabels()
    {
        return new[]
        {
            _labels.PlannerLabel,
            _labels.DorLabel,
            _labels.TechLeadLabel,
            _labels.SpecGateLabel,
            _labels.DevLabel,
            _labels.TestLabel,
            _labels.ReleaseLabel,
            _labels.CodeReviewNeededLabel,
            _labels.CodeReviewApprovedLabel,
            _labels.CodeReviewChangesRequestedLabel
        };
    }
}
