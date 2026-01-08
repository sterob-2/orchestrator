using Microsoft.Agents.AI.Workflows;

namespace Orchestrator.App.Workflows;

internal static class WorkflowFactory
{
    public static Workflow Build(WorkflowStage stage, WorkflowConfig workflowConfig, LabelConfig labels)
    {
        var executor = CreateExecutor(stage, workflowConfig, labels);
        return new WorkflowBuilder(executor)
            .WithOutputFrom(executor)
            .Build();
    }

    public static Workflow BuildGraph(WorkflowConfig workflowConfig, LabelConfig labels, WorkflowStage? startOverride)
    {
        var contextBuilder = new ContextBuilderExecutor(workflowConfig, labels, startOverride);
        var refinement = new RefinementExecutor(workflowConfig);
        var dorGate = new DorExecutor(workflowConfig);
        var techLead = new TechLeadExecutor(workflowConfig);
        var specGate = new SpecGateExecutor(workflowConfig);
        var dev = new DevExecutor(workflowConfig);
        var codeReview = new CodeReviewExecutor(workflowConfig);
        var dodGate = new DodExecutor(workflowConfig);
        var release = new ReleaseExecutor(workflowConfig);

        var builder = new WorkflowBuilder(contextBuilder)
            .WithOutputFrom(contextBuilder)
            .WithOutputFrom(refinement)
            .WithOutputFrom(dorGate)
            .WithOutputFrom(techLead)
            .WithOutputFrom(specGate)
            .WithOutputFrom(dev)
            .WithOutputFrom(codeReview)
            .WithOutputFrom(dodGate)
            .WithOutputFrom(release)
            .AddEdge(contextBuilder, refinement)
            .AddEdge(contextBuilder, dorGate)
            .AddEdge(contextBuilder, techLead)
            .AddEdge(contextBuilder, specGate)
            .AddEdge(contextBuilder, dev)
            .AddEdge(contextBuilder, codeReview)
            .AddEdge(contextBuilder, dodGate)
            .AddEdge(contextBuilder, release)
            .AddEdge(refinement, dorGate)
            .AddEdge(dorGate, techLead)
            .AddEdge(dorGate, refinement)
            .AddEdge(techLead, specGate)
            .AddEdge(specGate, dev)
            .AddEdge(specGate, techLead)
            .AddEdge(dev, codeReview)
            .AddEdge(codeReview, dodGate)
            .AddEdge(codeReview, dev)
            .AddEdge(dodGate, release)
            .AddEdge(dodGate, dev);

        return builder.Build();
    }

    private static Executor<WorkflowInput, WorkflowOutput> CreateExecutor(
        WorkflowStage stage,
        WorkflowConfig workflowConfig,
        LabelConfig labels)
    {
        return stage switch
        {
            WorkflowStage.ContextBuilder => new ContextBuilderExecutor(workflowConfig, labels, null),
            WorkflowStage.Refinement => new RefinementExecutor(workflowConfig),
            WorkflowStage.DoR => new DorExecutor(workflowConfig),
            WorkflowStage.TechLead => new TechLeadExecutor(workflowConfig),
            WorkflowStage.SpecGate => new SpecGateExecutor(workflowConfig),
            WorkflowStage.Dev => new DevExecutor(workflowConfig),
            WorkflowStage.CodeReview => new CodeReviewExecutor(workflowConfig),
            WorkflowStage.DoD => new DodExecutor(workflowConfig),
            WorkflowStage.Release => new ReleaseExecutor(workflowConfig),
            _ => new RefinementExecutor(workflowConfig)
        };
    }
}
