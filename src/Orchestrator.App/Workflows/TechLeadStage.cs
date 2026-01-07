namespace Orchestrator.App.Workflows;

internal sealed class TechLeadStage : IWorkflowStage
{
    public string Name => "TechLead";

    public Task<WorkflowStageResult> RunAsync(WorkContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WorkflowStageResult(true, "TechLead stage stub."));
    }
}
