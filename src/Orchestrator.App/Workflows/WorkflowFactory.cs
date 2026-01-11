using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Workflows;

internal static class WorkflowFactory
{
    /// <summary>
    /// Builds the full workflow graph starting from the specified stage.
    /// All executors are included so messages can route between them.
    /// </summary>
    /// <param name="workContext">The work context</param>
    /// <param name="startStage">The stage to start execution from. If null or ContextBuilder, starts from ContextBuilder.</param>
    public static Workflow BuildGraph(WorkContext workContext, WorkflowStage? startStage)
    {
        var workflowConfig = workContext.Config.Workflow;
        var labels = workContext.Config.Labels;

        // Create all executors
        var contextBuilder = new ContextBuilderExecutor(workContext, workflowConfig, labels, null);
        var refinement = new RefinementExecutor(workContext, workflowConfig);
        var questionClassifier = new QuestionClassifierExecutor(workContext, workflowConfig);
        var productOwner = new ProductOwnerExecutor(workContext, workflowConfig);
        var technicalAdvisor = new TechnicalAdvisorExecutor(workContext, workflowConfig);
        var dorGate = new DorExecutor(workContext, workflowConfig);
        var techLead = new TechLeadExecutor(workContext, workflowConfig);
        var specGate = new SpecGateExecutor(workContext, workflowConfig);
        var dev = new DevExecutor(workContext, workflowConfig);
        var codeReview = new CodeReviewExecutor(workContext, workflowConfig);
        var dodGate = new DodExecutor(workContext, workflowConfig);

        // Select entry executor based on startStage
        // This determines where execution begins in the graph
        Executor<WorkflowInput, WorkflowOutput> entryExecutor = startStage switch
        {
            null or WorkflowStage.ContextBuilder => contextBuilder,
            WorkflowStage.Refinement => refinement,
            WorkflowStage.QuestionClassifier => questionClassifier,
            WorkflowStage.ProductOwner => productOwner,
            WorkflowStage.TechnicalAdvisor => technicalAdvisor,
            WorkflowStage.DoR => dorGate,
            WorkflowStage.TechLead => techLead,
            WorkflowStage.SpecGate => specGate,
            WorkflowStage.Dev => dev,
            WorkflowStage.CodeReview => codeReview,
            WorkflowStage.DoD => dodGate,
            _ => contextBuilder
        };

        var builder = new WorkflowBuilder(entryExecutor)
            .WithOutputFrom(contextBuilder)
            .WithOutputFrom(refinement)
            .WithOutputFrom(questionClassifier)
            .WithOutputFrom(productOwner)
            .WithOutputFrom(technicalAdvisor)
            .WithOutputFrom(dorGate)
            .WithOutputFrom(techLead)
            .WithOutputFrom(specGate)
            .WithOutputFrom(dev)
            .WithOutputFrom(codeReview)
            .WithOutputFrom(dodGate)
            .AddEdge(contextBuilder, refinement)
            .AddEdge(contextBuilder, dorGate)
            .AddEdge(contextBuilder, techLead)
            .AddEdge(contextBuilder, specGate)
            .AddEdge(contextBuilder, dev)
            .AddEdge(contextBuilder, codeReview)
            .AddEdge(contextBuilder, dodGate)
            .AddEdge(refinement, dorGate)
            .AddEdge(refinement, questionClassifier)
            .AddEdge(questionClassifier, refinement)
            .AddEdge(questionClassifier, technicalAdvisor)
            .AddEdge(questionClassifier, productOwner)
            .AddEdge(productOwner, refinement)
            .AddEdge(technicalAdvisor, refinement)
            .AddEdge(dorGate, techLead)
            .AddEdge(dorGate, refinement)
            .AddEdge(techLead, specGate)
            .AddEdge(specGate, dev)
            .AddEdge(specGate, techLead)
            .AddEdge(dev, codeReview)
            .AddEdge(codeReview, dodGate)
            .AddEdge(codeReview, dev)
            .AddEdge(dodGate, dev);

        // Skip orphan validation when starting from mid-graph stage (e.g., after restart)
        // Earlier stages become "unreachable" but that's expected for re-entry scenarios
        return builder.Build(validateOrphans: false);
    }
}
