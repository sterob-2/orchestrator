using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class SpecGateExecutor : GateExecutor<(ParsedSpec ParsedSpec, Playbook Playbook, string SpecPath, string SpecContent)>
{
    public SpecGateExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("SpecGate", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.SpecGate;
    protected override string Notes => "Spec gate evaluated.";

    protected override async Task<GateInputResult<(ParsedSpec ParsedSpec, Playbook Playbook, string SpecPath, string SpecContent)>> LoadGateInputAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
        var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, specPath);
        if (string.IsNullOrWhiteSpace(specContent))
        {
            return GateInputResult<(ParsedSpec, Playbook, string, string)>.Fail($"Spec gate failed: missing spec at {specPath}.");
        }

        var playbookContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, WorkflowPaths.PlaybookPath) ?? "";
        var playbook = new PlaybookParser().Parse(playbookContent);
        var parsedSpec = new SpecParser().Parse(specContent);

        return GateInputResult<(ParsedSpec, Playbook, string, string)>.Ok((parsedSpec, playbook, specPath, specContent));
    }

    protected override GateResult EvaluateGate(
        (ParsedSpec ParsedSpec, Playbook Playbook, string SpecPath, string SpecContent) gateInput,
        WorkflowInput workflowInput)
    {
        return SpecGateValidator.Evaluate(gateInput.ParsedSpec, gateInput.Playbook, WorkContext.Workspace);
    }

    protected override string GetResultStateKey() => WorkflowStateKeys.SpecGateResult;

    protected override string BuildResultNotes(GateResult result)
    {
        return result.Passed
            ? "Spec gate passed."
            : $"Spec gate failed: {string.Join(" ", result.Failures)}";
    }

    protected override async Task HandleGateSuccessAsync(
        WorkflowInput input,
        (ParsedSpec ParsedSpec, Playbook Playbook, string SpecPath, string SpecContent) gateInput,
        GateResult result,
        CancellationToken cancellationToken)
    {
        var updatedSpec = TemplateUtil.UpdateStatus(gateInput.SpecContent, "COMPLETE");
        if (!string.Equals(updatedSpec, gateInput.SpecContent, StringComparison.Ordinal))
        {
            await FileOperationHelper.WriteAllTextAsync(WorkContext, gateInput.SpecPath, updatedSpec);
        }
    }
}
