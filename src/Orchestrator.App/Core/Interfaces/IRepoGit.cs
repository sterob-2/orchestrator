namespace Orchestrator.App.Core.Interfaces;

internal interface IRepoGit
{
    void EnsureConfigured();
    bool IsGitRepo();
    void EnsureBranch(string branchName, string baseBranch);
    void HardResetToRemote(string branchName);
    bool CommitAndPush(string branchName, string message, IEnumerable<string> paths);
}
