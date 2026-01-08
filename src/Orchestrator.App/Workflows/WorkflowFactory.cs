using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Workflows;

internal static class WorkflowFactory
{
    public static Workflow Build(WorkflowStage stage, WorkContext workContext)
    {
        var executor = CreateExecutor(stage, workContext);
        return new WorkflowBuilder(executor)
            .WithOutputFrom(executor)
            .Build();
    }

    public static Workflow BuildGraph(WorkContext workContext, WorkflowStage? startOverride)
    {
        var workflowConfig = workContext.Config.Workflow;
        var labels = workContext.Config.Labels;
        var contextBuilder = new ContextBuilderExecutor(workContext, workflowConfig, labels, startOverride);
        var refinement = new RefinementExecutor(workContext, workflowConfig);
        var dorGate = new DorExecutor(workContext, workflowConfig);
        var techLead = new TechLeadExecutor(workContext, workflowConfig);
        var specGate = new SpecGateExecutor(workContext, workflowConfig);
        var dev = new DevExecutor(workContext, workflowConfig);
        var codeReview = new CodeReviewExecutor(workContext, workflowConfig);
        var dodGate = new DodExecutor(workContext, workflowConfig);
        var release = new ReleaseExecutor(workContext, workflowConfig);

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
        WorkContext workContext)
    {
        var workflowConfig = workContext.Config.Workflow;
        var labels = workContext.Config.Labels;
        return stage switch
        {
            WorkflowStage.ContextBuilder => new ContextBuilderExecutor(workContext, workflowConfig, labels, null),
            WorkflowStage.Refinement => new RefinementExecutor(workContext, workflowConfig),
            WorkflowStage.DoR => new DorExecutor(workContext, workflowConfig),
            WorkflowStage.TechLead => new TechLeadExecutor(workContext, workflowConfig),
            WorkflowStage.SpecGate => new SpecGateExecutor(workContext, workflowConfig),
            WorkflowStage.Dev => new DevExecutor(workContext, workflowConfig),
            WorkflowStage.CodeReview => new CodeReviewExecutor(workContext, workflowConfig),
            WorkflowStage.DoD => new DodExecutor(workContext, workflowConfig),
            WorkflowStage.Release => new ReleaseExecutor(workContext, workflowConfig),
            _ => new RefinementExecutor(workContext, workflowConfig)
        };
    }
}
