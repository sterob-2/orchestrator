using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Workflows;

internal sealed class WorkflowMetricsRecorder : IMetricsRecorder
{
    private readonly IWorkflowMetricsStore _store;
    private readonly string _repoOwner;
    private readonly string _repoName;
    private readonly List<LlmCallMetrics> _llmCalls = new();
    private readonly List<string> _gateFailures = new();
    private readonly Dictionary<WorkflowStage, int> _iterations = new();
    private readonly object _lock = new();
    private DateTimeOffset? _startedAt;
    private int? _codeReviewFindings;
    private bool? _approved;

    public WorkflowMetricsRecorder(IWorkflowMetricsStore store, OrchestratorConfig config)
    {
        _store = store;
        _repoOwner = config.RepoOwner;
        _repoName = config.RepoName;
    }

    public void BeginRun(WorkItem item, WorkflowStage stage, string mode)
    {
        lock (_lock)
        {
            _startedAt ??= DateTimeOffset.UtcNow;
        }
    }

    public void RecordIteration(WorkflowStage stage, int attempt)
    {
        lock (_lock)
        {
            if (_iterations.TryGetValue(stage, out var current))
            {
                _iterations[stage] = Math.Max(current, attempt);
            }
            else
            {
                _iterations[stage] = attempt;
            }
        }
    }

    public void RecordLlmCall(LlmCallMetrics call)
    {
        lock (_lock)
        {
            _llmCalls.Add(call);
        }
    }

    public void RecordGateFailures(IEnumerable<string> failures)
    {
        lock (_lock)
        {
            _gateFailures.AddRange(failures);
        }
    }

    public void RecordCodeReview(int findingsCount, bool approved)
    {
        lock (_lock)
        {
            _codeReviewFindings = findingsCount;
            _approved = approved;
        }
    }

    public async Task CompleteRunAsync(
        WorkItem item,
        WorkflowStage stage,
        string mode,
        bool success,
        WorkflowStage? nextStage,
        IReadOnlyDictionary<WorkflowStage, int> iterations,
        CancellationToken cancellationToken)
    {
        WorkflowRunMetrics run;
        lock (_lock)
        {
            var startedAt = _startedAt ?? DateTimeOffset.UtcNow;
            var endedAt = DateTimeOffset.UtcNow;
            var duration = endedAt - startedAt;
            var iterationSnapshot = _iterations.Count > 0
                ? new Dictionary<WorkflowStage, int>(_iterations)
                : new Dictionary<WorkflowStage, int>(iterations);

            run = new WorkflowRunMetrics(
                IssueNumber: item.Number,
                RepoOwner: _repoOwner,
                RepoName: _repoName,
                Stage: stage,
                Mode: mode,
                StartedAt: startedAt,
                EndedAt: endedAt,
                DurationMilliseconds: duration.TotalMilliseconds,
                Success: success,
                NextStage: nextStage,
                CodeReviewFindings: _codeReviewFindings,
                Approved: _approved,
                Iterations: iterationSnapshot,
                GateFailures: _gateFailures.ToList(),
                LlmCalls: _llmCalls.ToList());
        }

        await _store.AppendAsync(run, cancellationToken);
    }
}
