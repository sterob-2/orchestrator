using Orchestrator.App.Infrastructure.Filesystem;

namespace Orchestrator.App.Tests.Workflows;

public class WorkflowMetricsStoreTests
{
    [Fact]
    public async Task InMemoryStore_ReturnsRecentRuns()
    {
        var store = new InMemoryWorkflowMetricsStore();
        await store.AppendAsync(BuildMetrics(1), CancellationToken.None);
        await store.AppendAsync(BuildMetrics(2), CancellationToken.None);
        await store.AppendAsync(BuildMetrics(3), CancellationToken.None);

        var recent = store.GetRecent(2);

        Assert.Equal(2, recent.Count);
        Assert.Equal(2, recent[0].IssueNumber);
        Assert.Equal(3, recent[1].IssueNumber);
    }

    [Fact]
    public async Task FileStore_AppendsAndReadsRecentRuns()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var workspace = new RepoWorkspace(tempDir);
            var store = new FileWorkflowMetricsStore(workspace, "metrics/metrics.jsonl");

            await store.AppendAsync(BuildMetrics(10), CancellationToken.None);
            await store.AppendAsync(BuildMetrics(20), CancellationToken.None);

            var recent = store.GetRecent(1);

            Assert.Single(recent);
            Assert.Equal(20, recent[0].IssueNumber);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FileStore_SwallowsWriteFailures()
    {
        var store = new FileWorkflowMetricsStore(new ThrowingWorkspace(), "metrics/metrics.jsonl");

        var exception = await Record.ExceptionAsync(() => store.AppendAsync(BuildMetrics(1), CancellationToken.None));

        Assert.Null(exception);
    }

    private static WorkflowRunMetrics BuildMetrics(int issueNumber)
    {
        return new WorkflowRunMetrics(
            IssueNumber: issueNumber,
            RepoOwner: "owner",
            RepoName: "repo",
            Stage: WorkflowStage.Dev,
            Mode: "default",
            StartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            EndedAt: DateTimeOffset.UtcNow,
            DurationMilliseconds: 1000,
            Success: true,
            NextStage: WorkflowStage.CodeReview,
            CodeReviewFindings: null,
            Approved: null,
            Iterations: new Dictionary<WorkflowStage, int> { [WorkflowStage.Dev] = 1 },
            GateFailures: Array.Empty<string>(),
            LlmCalls: Array.Empty<LlmCallMetrics>());
    }

    private sealed class ThrowingWorkspace : IRepoWorkspace
    {
        public string Root => "/invalid";
        public string ResolvePath(string relativePath) => throw new InvalidOperationException("boom");
        public bool Exists(string relativePath) => false;
        public string ReadAllText(string relativePath) => "";
        public void WriteAllText(string relativePath, string content) { }
        public IEnumerable<string> ListFiles(string relativeRoot, string searchPattern, int max) => Array.Empty<string>();
        public string ReadOrTemplate(string relativePath, string templatePath, Dictionary<string, string> tokens) => "";
    }
}
