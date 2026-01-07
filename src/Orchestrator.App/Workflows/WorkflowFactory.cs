namespace Orchestrator.App.Workflows;

internal sealed class WorkflowFactory
{
    public IReadOnlyList<IWorkflowStage> BuildDefaultStages(WorkContext context)
    {
        return new IWorkflowStage[]
        {
            new PlannerStage(),
            new TechLeadStage(),
            new DevStage(),
            new CodeReviewStage(),
            new TestStage(),
            new ReleaseStage()
        };
    }
}
