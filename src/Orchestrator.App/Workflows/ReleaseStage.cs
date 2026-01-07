namespace Orchestrator.App.Workflows;

internal sealed class ReleaseStage : IWorkflowStage
{
    public string Name => "Release";

    public Task<WorkflowStageResult> RunAsync(WorkContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WorkflowStageResult(true, "Release stage stub."));
    }
}
