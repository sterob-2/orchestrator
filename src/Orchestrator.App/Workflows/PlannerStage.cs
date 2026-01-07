namespace Orchestrator.App.Workflows;

internal sealed class PlannerStage : IWorkflowStage
{
    public string Name => "Planner";

    public Task<WorkflowStageResult> RunAsync(WorkContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WorkflowStageResult(true, "Planner stage stub."));
    }
}
