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
            return (false, "DoR gate failed: missing refinement output.");
        }

        var result = DorGateValidator.Evaluate(input.WorkItem, refinement, WorkContext.Config.Labels);
        if (!result.Passed && result.Failures.Count > 0)
        {
            WorkContext.Metrics?.RecordGateFailures(result.Failures);
        }
        var notes = result.Passed
            ? "DoR gate passed."
            : $"DoR gate failed: {string.Join(" ", result.Failures)}";

        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.DorGateResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.DorGateResult] = serializedResult;
        return (result.Passed, notes);
    }
}
