using Microsoft.Agents.AI.Workflows;

namespace Orchestrator.App.Workflows;

internal static class WorkflowFactory
{
    public static Workflow Build(WorkflowStage stage)
    {
        var executor = CreateExecutor(stage);
        return new WorkflowBuilder(executor)
            .WithOutputFrom(executor)
            .Build();
    }

    private static Executor<WorkflowInput, WorkflowOutput> CreateExecutor(WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.Refinement => new RefinementExecutor(),
            WorkflowStage.DoR => new DorExecutor(),
            WorkflowStage.TechLead => new TechLeadExecutor(),
            WorkflowStage.SpecGate => new SpecGateExecutor(),
            WorkflowStage.Dev => new DevExecutor(),
            WorkflowStage.CodeReview => new CodeReviewExecutor(),
            WorkflowStage.DoD => new DodExecutor(),
            WorkflowStage.Release => new ReleaseExecutor(),
            _ => new RefinementExecutor()
        };
    }
}
