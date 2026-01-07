namespace Orchestrator.App.Workflows;

internal sealed class TestStage : IWorkflowStage
{
    public string Name => "Test";

    public Task<WorkflowStageResult> RunAsync(WorkContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WorkflowStageResult(true, "Test stage stub."));
    }
}
