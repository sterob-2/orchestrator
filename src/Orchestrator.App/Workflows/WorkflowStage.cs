namespace Orchestrator.App.Workflows;

internal interface IWorkflowStage
{
    string Name { get; }

    Task<WorkflowStageResult> RunAsync(WorkContext context, CancellationToken cancellationToken);
}

internal sealed record WorkflowStageResult(bool Success, string Notes);
