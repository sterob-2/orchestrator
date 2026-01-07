namespace Orchestrator.App.Workflows;

internal sealed class CodeReviewStage : IWorkflowStage
{
    public string Name => "CodeReview";

    public Task<WorkflowStageResult> RunAsync(WorkContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WorkflowStageResult(true, "Code review stage stub."));
    }
}
