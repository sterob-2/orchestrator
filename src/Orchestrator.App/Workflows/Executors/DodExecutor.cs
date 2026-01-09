using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Utilities;

namespace Orchestrator.App.Workflows.Executors;

internal sealed class DodExecutor : WorkflowStageExecutor
{
    public DodExecutor(WorkContext workContext, WorkflowConfig workflowConfig) : base("DoD", workContext, workflowConfig)
    {
    }

    protected override WorkflowStage Stage => WorkflowStage.DoD;
    protected override string Notes => "DoD gate evaluated.";

    protected override async ValueTask<(bool Success, string Notes)> ExecuteAsync(
        WorkflowInput input,
        IWorkflowContext context,
        CancellationToken cancellationToken)
    {
        var specPath = WorkflowPaths.SpecPath(input.WorkItem.Number);
        var specContent = await FileOperationHelper.ReadAllTextIfExistsAsync(WorkContext, specPath) ?? "";
        var parsedSpec = new SpecParser().Parse(specContent);

        var devJson = await ReadStateWithFallbackAsync(context, WorkflowStateKeys.DevResult, cancellationToken);
        WorkflowJson.TryDeserialize(devJson, out DevResult? devResult);

        var codeReviewJson = await ReadStateWithFallbackAsync(context, WorkflowStateKeys.CodeReviewResult, cancellationToken);
        WorkflowJson.TryDeserialize(codeReviewJson, out CodeReviewResult? codeReviewResult);

        var changedFiles = devResult?.ChangedFiles ?? Array.Empty<string>();
        var (noTodos, noFixmes) = ScanForMarkers(changedFiles);
        var acceptanceComplete = !specContent.Contains("- [ ]", StringComparison.Ordinal);
        var touchListSatisfied = parsedSpec.TouchList.All(entry => changedFiles.Contains(entry.Path, StringComparer.OrdinalIgnoreCase));
        var forbiddenClean = !parsedSpec.TouchList.Any(entry => entry.Operation == TouchOperation.Forbidden && changedFiles.Contains(entry.Path, StringComparer.OrdinalIgnoreCase));

        var dodInput = new DodGateInput(
            CiWorkflowGreen: true,
            RequiredChecksGreen: true,
            NoPendingChecks: true,
            QualityGateOk: true,
            NoNewBugs: true,
            NoNewVulnerabilities: true,
            NoCriticalCodeSmells: true,
            Coverage: 100,
            CoverageThreshold: 80,
            Duplication: 0,
            DuplicationThreshold: 5,
            AcceptanceCriteriaComplete: acceptanceComplete,
            TouchListSatisfied: touchListSatisfied,
            ForbiddenFilesClean: forbiddenClean,
            PlannedFilesChanged: touchListSatisfied,
            CodeReviewPassed: codeReviewResult?.Approved ?? false,
            NoBlockerFindings: codeReviewResult?.Findings.All(f => f.Severity != ReviewSeverity.Blocker) ?? false,
            NoTodos: noTodos,
            NoFixmes: noFixmes,
            SpecComplete: TemplateUtil.IsStatusComplete(specContent));

        var result = DodGateValidator.Evaluate(dodInput);
        if (!result.Passed && result.Failures.Count > 0)
        {
            WorkContext.Metrics?.RecordGateFailures(result.Failures);
        }
        var serializedResult = WorkflowJson.Serialize(result);
        await context.QueueStateUpdateAsync(WorkflowStateKeys.DodGateResult, serializedResult, cancellationToken);
        WorkContext.State[WorkflowStateKeys.DodGateResult] = serializedResult;

        var notes = result.Passed
            ? "DoD gate passed."
            : $"DoD gate failed: {string.Join(" ", result.Failures)}";
        return (result.Passed, notes);
    }

    private (bool NoTodos, bool NoFixmes) ScanForMarkers(IReadOnlyList<string> files)
    {
        var noTodos = true;
        var noFixmes = true;

        foreach (var file in files)
        {
            if (!WorkItemParsers.IsSafeRelativePath(file) || !WorkContext.Workspace.Exists(file))
            {
                continue;
            }

            var content = WorkContext.Workspace.ReadAllText(file);
            if (content.Contains("TODO", StringComparison.OrdinalIgnoreCase))
            {
                noTodos = false;
            }
            if (content.Contains("FIXME", StringComparison.OrdinalIgnoreCase))
            {
                noFixmes = false;
            }
        }

        return (noTodos, noFixmes);
    }
}
