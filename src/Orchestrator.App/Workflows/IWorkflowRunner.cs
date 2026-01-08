using Microsoft.Agents.AI.Workflows;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Workflows;

internal interface IWorkflowRunner
{
    Task<WorkflowOutput?> RunAsync(WorkContext context, WorkflowStage stage, CancellationToken cancellationToken);
}
