namespace Orchestrator.App.Workflows;

internal static class WorkflowStageGraph
{
    public static WorkflowStage? NextStageFor(WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.ContextBuilder => WorkflowStage.Refinement,
            WorkflowStage.Refinement => WorkflowStage.DoR,
            WorkflowStage.DoR => WorkflowStage.TechLead,
            WorkflowStage.TechLead => WorkflowStage.SpecGate,
            WorkflowStage.SpecGate => WorkflowStage.Dev,
            WorkflowStage.Dev => WorkflowStage.CodeReview,
            WorkflowStage.CodeReview => WorkflowStage.DoD,
            WorkflowStage.DoD => WorkflowStage.Release,
            WorkflowStage.Release => null,
            _ => null
        };
    }
}
