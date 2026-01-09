using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class DorExecutor : GateExecutor<(RefinementResult Refinement, WorkItem WorkItem)>
{
    public DorExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("DoR", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoR;
    protected override string Notes => "DoR gate evaluated.";

    protected override async Task<GateInputResult<(RefinementResult Refinement, WorkItem WorkItem)>> LoadGateInputAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        Logger.Info($"[DoR] Evaluating DoR gate for issue #{input.WorkItem.Number}");

        var refinementJson = await ReadStateWithFallbackAsync(
            context,
            WorkflowStateKeys.RefinementResult,
            cancellationToken);

        if (!WorkflowJson.TryDeserialize(refinementJson, out RefinementResult? refinement) || refinement is null)
        {
            Logger.Warning($"[DoR] No refinement result found in workflow state");
            return GateInputResult<(RefinementResult, WorkItem)>.Fail("DoR gate failed: missing refinement output.");
        }

        Logger.Debug($"[DoR] Validating refinement: {refinement.AcceptanceCriteria.Count} criteria, {refinement.OpenQuestions.Count} questions");
        return GateInputResult<(RefinementResult, WorkItem)>.Ok((refinement, input.WorkItem));
    }

    protected override GateResult EvaluateGate(
        (RefinementResult Refinement, WorkItem WorkItem) gateInput,
        WorkflowInput workflowInput)
    {
        var result = DorGateValidator.Evaluate(gateInput.WorkItem, gateInput.Refinement, WorkContext.Config.Labels);

        if (!result.Passed && result.Failures.Count > 0)
        {
            Logger.Info($"[DoR] Gate failed with {result.Failures.Count} failure(s): {string.Join(", ", result.Failures)}");
        }
        else
        {
            Logger.Info($"[DoR] Gate passed");
        }

        return result;
    }

    protected override string GetResultStateKey() => WorkflowStateKeys.DorGateResult;

    protected override string BuildResultNotes(GateResult result)
    {
        return result.Passed
            ? "DoR gate passed."
            : $"DoR gate failed: {string.Join(" ", result.Failures)}";
    }

    protected override async Task HandleGateFailureAsync(
        WorkflowInput input,
        (RefinementResult Refinement, WorkItem WorkItem) gateInput,
        GateResult result,
        CancellationToken cancellationToken)
    {
        // Write DoR result to file
        var dorPath = WorkflowPaths.DorResultPath(input.WorkItem.Number);
        Logger.Debug($"[DoR] Writing DoR result to {dorPath}");
        await WriteDorResultFileAsync(input.WorkItem, gateInput.Refinement, result, dorPath);
        Logger.Info($"[DoR] Wrote DoR result to {dorPath}");

        // Commit the DoR result file
        var commitMessage = $"dor: DoR gate failed for issue #{input.WorkItem.Number}\n\n" +
                           $"Failures:\n" +
                           string.Join("\n", result.Failures.Select(f => $"- {f}"));
        await TryCommitAndPushAsync(input.WorkItem, dorPath, commitMessage);

        // Post simple pointer comment to GitHub
        await PostDorFailurePointerAsync(input.WorkItem, dorPath, result);
    }

    private async Task WriteDorResultFileAsync(WorkItem workItem, RefinementResult refinement, GateResult gateResult, string filePath)
    {
        var content = new System.Text.StringBuilder();
        content.AppendLine($"# DoR Result: Issue #{workItem.Number} - {workItem.Title}");
        content.AppendLine();
        content.AppendLine($"**Status**: {(gateResult.Passed ? "✅ PASSED" : "❌ FAILED")}");
        content.AppendLine($"**Evaluated**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        content.AppendLine();

        if (!gateResult.Passed)
        {
            content.AppendLine("## Failures");
            content.AppendLine();
            foreach (var failure in gateResult.Failures)
            {
                content.AppendLine($"- {failure}");
            }
            content.AppendLine();
        }

        if (refinement.OpenQuestions.Count > 0)
        {
            content.AppendLine($"## Open Questions ({refinement.OpenQuestions.Count})");
            content.AppendLine();
            content.AppendLine("*These questions must be answered before the DoR gate can pass.*");
            content.AppendLine();
            RefinementMarkdownBuilder.AppendOpenQuestions(content, refinement.OpenQuestions);
        }

        RefinementMarkdownBuilder.AppendClarifiedStory(content, refinement.ClarifiedStory);
        RefinementMarkdownBuilder.AppendAcceptanceCriteria(content, refinement.AcceptanceCriteria);

        content.AppendLine("---");
        content.AppendLine("*See also: [Refinement Output](../refinement/issue-" + workItem.Number + ".md)*");

        await FileOperationHelper.WriteAllTextAsync(WorkContext, filePath, content.ToString());
    }

    private async Task PostDorFailurePointerAsync(WorkItem workItem, string dorFilePath, GateResult gateResult)
    {
        var comment = $"## 🚧 DoR Gate Failed\n\n" +
                     $"The Definition of Ready gate has {gateResult.Failures.Count} failure(s).\n\n" +
                     $"**Details**: See [`{dorFilePath}`](../../blob/issue-{workItem.Number}/{dorFilePath})\n\n" +
                     $"*Once issues are resolved, remove the `blocked` label and add the `dor` label to re-evaluate.*";

        try
        {
            await WorkContext.GitHub.CommentOnWorkItemAsync(workItem.Number, comment);
            Logger.Info($"[DoR] Posted DoR failure pointer to GitHub issue #{workItem.Number}");
        }
        catch (Octokit.ApiException ex)
        {
            Logger.Error($"[DoR] GitHub API error posting comment: {ex.Message}");
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            Logger.Error($"[DoR] Network error posting comment to GitHub: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            Logger.Error($"[DoR] Timeout posting comment to GitHub: {ex.Message}");
        }
    }
}
