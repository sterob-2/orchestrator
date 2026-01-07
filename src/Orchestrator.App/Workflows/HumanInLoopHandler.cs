namespace Orchestrator.App.Workflows;

internal sealed class HumanInLoopHandler
{
    public Task ApplyAsync(WorkItem item, WorkflowOutput output)
    {
        return Task.CompletedTask;
    }
}
