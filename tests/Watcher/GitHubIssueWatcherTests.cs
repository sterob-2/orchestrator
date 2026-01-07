using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Orchestrator.App.Tests.Watcher;

public class GitHubIssueWatcherTests
{
    [Fact]
    public async Task RunAsync_DelegatesToRunner()
    {
        var runner = new TestRunner();
        var watcher = new GitHubIssueWatcher(runner);
        using var cts = new CancellationTokenSource();

        await watcher.RunAsync(cts.Token);

        Assert.True(runner.Called);
        Assert.Equal(cts.Token, runner.Token);
    }

    private sealed class TestRunner : IWorkItemRunner
    {
        public bool Called { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            Called = true;
            Token = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
