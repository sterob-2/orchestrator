using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Core.Interfaces;

internal interface IWorkflowMetricsStore
{
    Task AppendAsync(WorkflowRunMetrics metrics, CancellationToken cancellationToken);
    IReadOnlyList<WorkflowRunMetrics> GetRecent(int count);
}
