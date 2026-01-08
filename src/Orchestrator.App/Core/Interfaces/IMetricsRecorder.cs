using Orchestrator.App.Core.Models;
using Orchestrator.App.Workflows;

namespace Orchestrator.App.Core.Interfaces;

internal interface IMetricsRecorder
{
    void BeginRun(WorkItem item, WorkflowStage stage, string mode);
    void RecordIteration(WorkflowStage stage, int attempt);
    void RecordLlmCall(LlmCallMetrics call);
    void RecordGateFailures(IEnumerable<string> failures);
    void RecordCodeReview(int findingsCount, bool approved);
    Task CompleteRunAsync(
        WorkItem item,
        WorkflowStage stage,
        string mode,
        bool success,
        WorkflowStage? nextStage,
        IReadOnlyDictionary<WorkflowStage, int> iterations,
        CancellationToken cancellationToken);
}
