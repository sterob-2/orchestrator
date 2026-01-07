namespace Orchestrator.App.Workflows;

internal sealed class DevStage : IWorkflowStage
{
    public string Name => "Dev";

    public Task<WorkflowStageResult> RunAsync(WorkContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WorkflowStageResult(true, "Dev stage stub."));
    }
}
