using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class SpecGateExecutor : WorkflowStageExecutor
{
    public SpecGateExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("SpecGate", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.SpecGate;
    protected override string Notes => "Spec gate evaluated.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
        var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, specPath);
        if (string.IsNullOrWhiteSpace(specContent))
        {
            return (false, $"Spec gate failed: missing spec at {specPath}.");
        }

        var playbookContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.PlaybookPath) ?? "";
        var playbook = new PlaybookParser().Parse(playbookContent);
        var parsedSpec = new SpecParser().Parse(specContent);
        var result = SpecGateValidator.Evaluate(parsedSpec, playbook, WorkContext.Workspace);
        if (!result.Passed && result.Failures.Count > 0)
        {
            WorkContext.Metrics?.RecordGateFailures(result.Failures);
        }

        if (result.Passed)
        {
            var updatedSpec = TemplateUtil.UpdateStatus(specContent, "COMPLETE");
            if (!string.Equals(updatedSpec, specContent, StringComparison.Ordinal))
            {
                await FileOperationHelper.WriteAllTextAsync(WorkContext, specPath, updatedSpec);
            }
        }

        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.SpecGateResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.SpecGateResult] = serializedResult;

        var notes = result.Passed
            ? "Spec gate passed."
            : $"Spec gate failed: {string.Join(" ", result.Failures)}";
        return (result.Passed, notes);
    }
}
