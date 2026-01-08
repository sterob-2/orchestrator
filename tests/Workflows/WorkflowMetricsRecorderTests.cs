using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Workflows;

public class WorkflowMetricsRecorderTests
{
    [Fact]
    public async Task Recorder_WritesCompletedRunWithDetails()
    {
        var config = MockWorkContext.CreateConfig();
        var store = new InMemoryWorkflowMetricsStore();
        var recorder = new WorkflowMetricsRecorder(store, config);
        var item = new WorkItem(42, "Title", "Body", "url", new List<string>());

        recorder.BeginRun(item, WorkflowStage.Dev, "batch");
        recorder.RecordIteration(WorkflowStage.Dev, 2);
        recorder.RecordLlmCall(new LlmCallMetrics("gpt-5", 10, 20, 12.3, null));
        recorder.RecordGateFailures(new[] { "Spec-01: Goal missing." });
        recorder.RecordCodeReview(3, approved: false);

        await recorder.CompleteRunAsync(
            item,
            WorkflowStage.Dev,
            "batch",
            success: false,
            nextStage: null,
            new Dictionary<WorkflowStage, int>(),
            CancellationToken.None);

        var recent = store.GetRecent(1);
        Assert.Single(recent);

        var run = recent[0];
        Assert.Equal(42, run.IssueNumber);
        Assert.Equal("batch", run.Mode);
        Assert.Equal(WorkflowStage.Dev, run.Stage);
        Assert.False(run.Success);
        Assert.Equal(3, run.CodeReviewFindings);
        Assert.False(run.Approved);
        Assert.Equal(2, run.Iterations[WorkflowStage.Dev]);
        Assert.Single(run.GateFailures);
        Assert.Single(run.LlmCalls);
    }
}
