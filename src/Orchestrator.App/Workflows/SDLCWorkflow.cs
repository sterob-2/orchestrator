using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Workflows;

/// <summary>
/// Builder for SDLC workflow using Microsoft Agent Framework
/// </summary>
internal static class SDLCWorkflow
{
    /// <summary>
    /// Executes the workflow for a given work item
    /// </summary>
    public static async Task<WorkflowOutput?> RunWorkflowAsync(
        Workflow workflow,
        WorkflowInput input,
        Func<WorkflowOutput, Task>? onOutput = null)
    {
        WorkflowOutput? finalOutput = null;

        Logger.WriteLine($"[Workflow] Starting workflow for issue #{input.WorkItem.Number}...");

        try
        {
            // Execute workflow
            Run run = await InProcessExecution.RunAsync(workflow, input);

            // Process events
            foreach (WorkflowEvent evt in run.NewEvents)
            {
                switch (evt)
                {
                    case ExecutorCompletedEvent completedEvt:
                        Logger.WriteLine($"[Workflow] Executor completed: {completedEvt.ExecutorId}");
                        if (completedEvt.Data is WorkflowOutput output)
                        {
                            finalOutput = output;
                            if (onOutput != null)
                            {
                                await onOutput(output);
                            }
                            Logger.WriteLine($"   Success: {output.Success}");
                            Logger.WriteLine($"   Notes: {output.Notes}");
                            if (output.NextStage is not null)
                            {
                                Logger.WriteLine($"   Next Stage: {output.NextStage}");
                            }
                        }
                        break;
                    case ExecutorFailedEvent failedEvt:
                        Logger.WriteLine($"[Workflow] Executor failed: {failedEvt.ExecutorId}");
                        Logger.WriteLine($"[Workflow] Error: {failedEvt.Error}");
                        break;
                    case WorkflowErrorEvent errorEvt:
                        Logger.WriteLine($"[Workflow] Workflow error: {errorEvt.Error}");
                        break;
                    default:
                        Logger.WriteLine($"[Workflow] Unhandled event: {evt.GetType().Name}");
                        break;
                }
            }

            Logger.WriteLine($"[Workflow] Workflow completed!");
            return finalOutput;
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"[Workflow] EXCEPTION during workflow execution for issue #{input.WorkItem.Number}: {ex}");
            throw;
        }
    }
}
