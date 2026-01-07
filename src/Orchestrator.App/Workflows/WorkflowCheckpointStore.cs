using System.Collections.Generic;

namespace Orchestrator.App.Workflows;

internal interface IWorkflowCheckpointStore
{
    int IncrementStage(int issueNumber, WorkflowStage stage);
    int GetStageAttempts(int issueNumber, WorkflowStage stage);
    void Reset(int issueNumber);
}

internal sealed class InMemoryWorkflowCheckpointStore : IWorkflowCheckpointStore
{
    private readonly Dictionary<int, WorkflowCheckpoint> _store = new();
    private readonly object _lock = new();

    public int IncrementStage(int issueNumber, WorkflowStage stage)
    {
        lock (_lock)
        {
            var checkpoint = GetOrCreate(issueNumber);
            var next = checkpoint.StageAttempts.TryGetValue(stage, out var count) ? count + 1 : 1;
            checkpoint.StageAttempts[stage] = next;
            return next;
        }
    }

    public int GetStageAttempts(int issueNumber, WorkflowStage stage)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(issueNumber, out var checkpoint) &&
                checkpoint.StageAttempts.TryGetValue(stage, out var count))
            {
                return count;
            }

            return 0;
        }
    }

    public void Reset(int issueNumber)
    {
        lock (_lock)
        {
            _store.Remove(issueNumber);
        }
    }

    private WorkflowCheckpoint GetOrCreate(int issueNumber)
    {
        if (!_store.TryGetValue(issueNumber, out var checkpoint))
        {
            checkpoint = new WorkflowCheckpoint(issueNumber);
            _store[issueNumber] = checkpoint;
        }

        return checkpoint;
    }

    private sealed class WorkflowCheckpoint
    {
        public WorkflowCheckpoint(int issueNumber)
        {
            IssueNumber = issueNumber;
        }

        public int IssueNumber { get; }
        public Dictionary<WorkflowStage, int> StageAttempts { get; } = new();
    }
}
