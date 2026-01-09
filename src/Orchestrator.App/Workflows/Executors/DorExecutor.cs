using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class DorExecutor : WorkflowStageExecutor
{
    public DorExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("DoR", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoR;
    protected override string Notes => "DoR gate evaluated.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        Logger.Info($"[DoR] Evaluating DoR gate for issue #{input.WorkItem.Number}");

        var refinementJson = await context.ReadOrInitStateAsync(
            WorkflowStateKeys.RefinementResult,
            () => string.Empty,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(refinementJson) && WorkContext.State.TryGetValue(WorkflowStateKeys.RefinementResult, out var fallbackJson))
        {
            refinementJson = fallbackJson;
        }

        if (!WorkflowJson.TryDeserialize(refinementJson, out RefinementResult? refinement) || refinement is null)
        {
            Logger.Warning($"[DoR] No refinement result found in workflow state");
            return (false, "DoR gate failed: missing refinement output.");
        }

        Logger.Debug($"[DoR] Validating refinement: {refinement.AcceptanceCriteria.Count} criteria, {refinement.OpenQuestions.Count} questions");

        var result = DorGateValidator.Evaluate(input.WorkItem, refinement, WorkContext.Config.Labels);
        if (!result.Passed && result.Failures.Count > 0)
        {
            Logger.Info($"[DoR] Gate failed with {result.Failures.Count} failure(s): {string.Join(", ", result.Failures)}");
            WorkContext.Metrics?.RecordGateFailures(result.Failures);

            // Post refinement details to GitHub when DoR fails
            Logger.Debug($"[DoR] Posting refinement details to GitHub");
            await PostRefinementToGitHubAsync(input.WorkItem, refinement, result);
        }
        else
        {
            Logger.Info($"[DoR] Gate passed");
        }

        var notes = result.Passed
            ? "DoR gate passed."
            : $"DoR gate failed: {string.Join(" ", result.Failures)}";

        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.DorGateResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.DorGateResult] = serializedResult;
        return (result.Passed, notes);
    }

    private async Task PostRefinementToGitHubAsync(WorkItem workItem, RefinementResult refinement, GateResult gateResult)
    {
        var commentBuilder = new System.Text.StringBuilder();
        commentBuilder.AppendLine("## 🚧 Definition of Ready (DoR) Gate Failed");
        commentBuilder.AppendLine();
        commentBuilder.AppendLine("The DoR gate evaluation has failed. Please review and address the following issues:");
        commentBuilder.AppendLine();

        // List failures
        commentBuilder.AppendLine("### ❌ Failures");
        foreach (var failure in gateResult.Failures)
        {
            commentBuilder.AppendLine($"- {failure}");
        }
        commentBuilder.AppendLine();

        // Show open questions if they exist
        if (refinement.OpenQuestions.Count > 0)
        {
            commentBuilder.AppendLine("### ❓ Open Questions");
            commentBuilder.AppendLine("The following questions need to be answered before work can proceed:");
            commentBuilder.AppendLine();
            foreach (var question in refinement.OpenQuestions)
            {
                commentBuilder.AppendLine($"- {question}");
            }
            commentBuilder.AppendLine();
        }

        // Show clarified story if available
        if (!string.IsNullOrWhiteSpace(refinement.ClarifiedStory))
        {
            commentBuilder.AppendLine("### 📝 Clarified Story");
            commentBuilder.AppendLine(refinement.ClarifiedStory);
            commentBuilder.AppendLine();
        }

        // Show acceptance criteria
        if (refinement.AcceptanceCriteria.Count > 0)
        {
            commentBuilder.AppendLine("### ✅ Acceptance Criteria");
            foreach (var criterion in refinement.AcceptanceCriteria)
            {
                commentBuilder.AppendLine($"- {criterion}");
            }
            commentBuilder.AppendLine();
        }

        commentBuilder.AppendLine("---");
        commentBuilder.AppendLine("*Once the above issues are resolved, remove the `blocked` label and add the `dor` label to re-evaluate.*");

        var comment = commentBuilder.ToString();
        Logger.Debug($"[DoR] Comment length: {comment.Length} chars");

        try
        {
            await WorkContext.GitHub.CommentOnWorkItemAsync(workItem.Number, comment);
            Logger.Info($"[DoR] Posted refinement details to GitHub issue #{workItem.Number}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[DoR] Failed to post comment to GitHub: {ex.Message}");
        }
    }
}
