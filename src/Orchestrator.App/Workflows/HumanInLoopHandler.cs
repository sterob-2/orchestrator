using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Workflows;

internal sealed class HumanInLoopHandler
{
    private readonly IGitHubClient _github;
    private readonly LabelConfig _labels;

    public HumanInLoopHandler(IGitHubClient github, LabelConfig labels)
    {
        _github = github;
        _labels = labels;
    }

    public async Task ApplyAsync(WorkItem item, WorkflowOutput output)
    {
        if (output.Success || output.NextStage is not null)
        {
            return;
        }

        var labelsToRemove = new[]
        {
            _labels.WorkItemLabel,
            _labels.PlannerLabel,
            _labels.DorLabel,
            _labels.TechLeadLabel,
            _labels.SpecGateLabel,
            _labels.DevLabel,
            _labels.TestLabel,
            _labels.ReleaseLabel,
            _labels.CodeReviewNeededLabel,
            _labels.CodeReviewApprovedLabel,
            _labels.CodeReviewChangesRequestedLabel,
            _labels.InProgressLabel,
            _labels.ReviewNeededLabel,
            _labels.ReviewedLabel,
            _labels.ResetLabel
        };

        await _github.RemoveLabelsAsync(item.Number, labelsToRemove);
        await _github.AddLabelsAsync(item.Number, _labels.BlockedLabel, _labels.UserReviewRequiredLabel);
    }
}
