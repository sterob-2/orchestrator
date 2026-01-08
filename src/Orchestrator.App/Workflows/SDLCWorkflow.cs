using Microsoft.Agents.AI.Workflows;

namespace Orchestrator.App.Workflows;

/// <summary>
/// Builder for SDLC workflow using Microsoft Agent Framework
/// </summary>
internal static class SDLCWorkflow
{
    /// <summary>
    /// Creates a single-stage workflow for the requested stage.
    /// </summary>
    public static Workflow BuildStageWorkflow(WorkflowStage stage, WorkflowConfig workflowConfig, LabelConfig labels)
    {
        return WorkflowFactory.Build(stage, workflowConfig, labels);
    }

    /// <summary>
    /// Executes the workflow for a given work item
    /// </summary>
    public static async Task<WorkflowOutput?> RunWorkflowAsync(
        Workflow workflow,
        WorkflowInput input,
        Func<WorkflowOutput, Task>? onOutput = null)
    {
        WorkflowOutput? finalOutput = null;

        Console.WriteLine($"[Workflow] Starting workflow for issue #{input.WorkItem.Number}...");

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
                        if (onOutput != null)
                        {
                            await onOutput(output);
                        }
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
