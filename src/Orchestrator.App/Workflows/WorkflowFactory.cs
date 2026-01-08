using System.Collections.Generic;
using Microsoft.Agents.AI.Workflows;

namespace Orchestrator.App.Workflows;

internal static class WorkflowFactory
{
    public static Workflow Build(WorkflowStage stage)
    {
        return BuildGraph(stage);
    }

    public static Workflow BuildGraph(WorkflowStage startStage)
    {
        var executors = CreateExecutors();
        var contextBuilder = executors[WorkflowStage.ContextBuilder];
        var refinement = executors[WorkflowStage.Refinement];
        var dorGate = executors[WorkflowStage.DoR];
        var techLead = executors[WorkflowStage.TechLead];
        var specGate = executors[WorkflowStage.SpecGate];
        var dev = executors[WorkflowStage.Dev];
        var codeReview = executors[WorkflowStage.CodeReview];
        var dodGate = executors[WorkflowStage.DoD];
        var release = executors[WorkflowStage.Release];

        var builder = new WorkflowBuilder(executors[startStage])
            .WithOutputFrom(executors[startStage])
            .AddEdge(contextBuilder, refinement)
            .AddEdge(refinement, dorGate)
            .AddEdge<WorkflowOutput>(dorGate, techLead, output => output is { Success: true })
            .AddEdge<WorkflowOutput>(dorGate, refinement, output => output is { Success: false })
            .AddEdge(techLead, specGate)
            .AddEdge<WorkflowOutput>(specGate, dev, output => output is { Success: true })
            .AddEdge<WorkflowOutput>(specGate, techLead, output => output is { Success: false })
            .AddEdge(dev, codeReview)
            .AddEdge<WorkflowOutput>(codeReview, dodGate, output => output is { Success: true })
            .AddEdge<WorkflowOutput>(codeReview, dev, output => output is { Success: false })
            .AddEdge<WorkflowOutput>(dodGate, release, output => output is { Success: true })
            .AddEdge<WorkflowOutput>(dodGate, dev, output => output is { Success: false });

        return builder.Build(validateOrphans: false);
    }

    private static Dictionary<WorkflowStage, Executor<WorkflowInput, WorkflowOutput>> CreateExecutors()
    {
        return new Dictionary<WorkflowStage, Executor<WorkflowInput, WorkflowOutput>>
        {
            [WorkflowStage.ContextBuilder] = new ContextBuilderExecutor(),
            [WorkflowStage.Refinement] = new RefinementExecutor(),
            [WorkflowStage.DoR] = new DorExecutor(),
            [WorkflowStage.TechLead] = new TechLeadExecutor(),
            [WorkflowStage.SpecGate] = new SpecGateExecutor(),
            [WorkflowStage.Dev] = new DevExecutor(),
            [WorkflowStage.CodeReview] = new CodeReviewExecutor(),
            [WorkflowStage.DoD] = new DodExecutor(),
            [WorkflowStage.Release] = new ReleaseExecutor()
        };
    }
}
