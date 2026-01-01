using Microsoft.Agents.AI.Workflows;

namespace Orchestrator.App;

/// <summary>
/// Builder for SDLC workflow using Microsoft Agent Framework
/// </summary>
internal static class SDLCWorkflow
{
    /// <summary>
    /// Creates a minimal workflow with just the Planner stage for prototype testing
    /// </summary>
    public static Workflow BuildPlannerOnlyWorkflow(WorkContext context)
    {
        // Create the planner executor
        var plannerExecutor = new PlannerExecutor(context);

        // Build workflow with single executor
        var workflow = new WorkflowBuilder(plannerExecutor)
            .WithOutputFrom(plannerExecutor)
            .Build();

        return workflow;
    }

    /// <summary>
    /// Executes the workflow for a given work item
    /// </summary>
    public static async Task<WorkflowOutput?> RunWorkflowAsync(
        Workflow workflow,
        WorkflowInput input)
    {
        WorkflowOutput? finalOutput = null;

        Console.WriteLine($"[Workflow] Starting workflow for issue #{input.IssueNumber}...");

        // Execute workflow
        Run run = await InProcessExecution.RunAsync(workflow, input);

        // Process events
        foreach (WorkflowEvent evt in run.NewEvents)
        {
            switch (evt)
            {
                case ExecutorCompletedEvent completedEvt:
                    Console.WriteLine($"[Workflow] Executor completed: {completedEvt.ExecutorId}");
                    if (completedEvt.Data is WorkflowOutput output)
                    {
                        finalOutput = output;
                        Console.WriteLine($"   Success: {output.Success}");
                        Console.WriteLine($"   Notes: {output.Notes}");
                        if (output.NextStage is not null)
                        {
                            Console.WriteLine($"   Next Stage: {output.NextStage}");
                        }
                    }
                    break;
            }
        }

        Console.WriteLine($"[Workflow] Workflow completed!");
        return finalOutput;
    }
}
