namespace Orchestrator.App.Watcher;

internal interface IWorkItemRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}

internal sealed class GitHubIssueWatcher
{
    private readonly IWorkItemRunner _runner;

    public GitHubIssueWatcher(IWorkItemRunner runner)
    {
        _runner = runner;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        return _runner.RunAsync(cancellationToken);
    }
}
