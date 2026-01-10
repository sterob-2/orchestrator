using System.Collections.Generic;

namespace Orchestrator.App.Workflows;

internal interface IWorkflowCheckpointStore
{
    int IncrementStage(int issueNumber, WorkflowStage stage);
    int GetStageAttempts(int issueNumber, WorkflowStage stage);
    void Reset(int issueNumber);
    bool TryBeginWorkflow(int issueNumber);
    void CompleteWorkflow(int issueNumber);
    bool IsWorkflowInProgress(int issueNumber);
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

    public bool TryBeginWorkflow(int issueNumber)
    {
        lock (_lock)
        {
            var checkpoint = GetOrCreate(issueNumber);
            if (checkpoint.IsInProgress)
            {
                return false; // Already in progress
            }

            checkpoint.IsInProgress = true;
            return true; // Successfully started
        }
    }

    public void CompleteWorkflow(int issueNumber)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(issueNumber, out var checkpoint))
            {
                checkpoint.IsInProgress = false;
            }
        }
    }

    public bool IsWorkflowInProgress(int issueNumber)
    {
        lock (_lock)
        {
            return _store.TryGetValue(issueNumber, out var checkpoint) && checkpoint.IsInProgress;
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
        public bool IsInProgress { get; set; }
    }
}
