using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Core.Adapters;

internal sealed class RepoGitAdapter : IRepoGit
{
    private readonly global::Orchestrator.App.RepoGit _repo;

    public RepoGitAdapter(global::Orchestrator.App.RepoGit repo)
    {
        _repo = repo;
    }

    public void EnsureConfigured() => _repo.EnsureConfigured();

    public bool IsGitRepo() => _repo.IsGitRepo();

    public void EnsureBranch(string branchName, string baseBranch) => _repo.EnsureBranch(branchName, baseBranch);

    public void HardResetToRemote(string branchName) => _repo.HardResetToRemote(branchName);

    public bool CommitAndPush(string branchName, string message, IEnumerable<string> paths)
    {
        return _repo.CommitAndPush(branchName, message, paths);
    }
}
