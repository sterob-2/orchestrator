namespace Orchestrator.App.Workflows;

internal interface IWorkflowRunner
{
    Task<WorkflowOutput?> RunAsync(WorkContext context, WorkflowStage stage, CancellationToken cancellationToken);
}
