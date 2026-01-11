using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Orchestrator.App.Core.Configuration;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Infrastructure.Git;

internal sealed class RepoGit : IRepoGit
{
    private readonly OrchestratorConfig _cfg;
    private readonly string _root;

    public RepoGit(OrchestratorConfig cfg, string root)
    {
        _cfg = cfg;
        _root = root;
    }

    public void EnsureConfigured()
    {
        if (!IsGitRepo() || !IsGitWorkTree())
        {
            Logger.WriteLine($"Workspace is not a git repo: {_root}");
            return;
        }

        using var repo = new Repository(_root);

        repo.Config.Set("user.name", _cfg.GitAuthorName);
        repo.Config.Set("user.email", _cfg.GitAuthorEmail);

        var remoteUrl = ResolveRemoteUrl();
        if (!string.IsNullOrWhiteSpace(remoteUrl))
        {
            try
            {
                var origin = repo.Network.Remotes["origin"];
                if (origin != null)
                {
                    repo.Network.Remotes.Update("origin", r => r.Url = remoteUrl);
                }
                else
                {
                    repo.Network.Remotes.Add("origin", remoteUrl);
                }
            }
            catch
            {
                // Ignore remote configuration errors
            }
        }
    }

    public bool IsGitRepo()
    {
        return Repository.IsValid(_root);
    }

    private bool IsGitWorkTree()
    {
        try
        {
            using var repo = new Repository(_root);
            return !repo.Info.IsBare;
        }
        catch
        {
            return false;
        }
    }

    public void CleanWorkingTree()
    {
        using var repo = new Repository(_root);

        MoveUntrackedGeneratedFiles();

        // Clean working tree - only called by ContextBuilder at workflow start
        repo.Reset(ResetMode.Hard);
        repo.RemoveUntrackedFiles();
    }

    public void EnsureBranch(string branchName, string baseBranch)
    {
        using var repo = new Repository(_root);

        // Fetch from remote
        try
        {
            var remote = repo.Network.Remotes["origin"];
            if (remote != null)
            {
                Commands.Fetch(repo, remote.Name, Array.Empty<string>(), GetFetchOptions(), null);
            }
        }
        catch
        {
            // Ignore fetch errors
        }

        MoveUntrackedGeneratedFiles();

        // Check if remote branch exists
        var remoteBranchName = $"origin/{branchName}";
        var remoteBranch = repo.Branches[remoteBranchName];

        if (remoteBranch != null)
        {
            // Checkout existing remote branch
            var localBranch = repo.Branches[branchName];
            if (localBranch != null)
            {
                Commands.Checkout(repo, localBranch);
                repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
            }
            else
            {
                localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
                Commands.Checkout(repo, localBranch);
                repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
            }
        }
        else
        {
            // Create new branch from base branch
            var baseBranchName = $"origin/{baseBranch}";
            var baseRef = repo.Branches[baseBranchName];

            if (baseRef != null)
            {
                var localBranch = repo.Branches[branchName];
                if (localBranch != null)
                {
                    Commands.Checkout(repo, localBranch);
                }
                else
                {
                    localBranch = repo.CreateBranch(branchName, baseRef.Tip);
                    Commands.Checkout(repo, localBranch);
                }
            }
            else
            {
                // Fallback: create branch from HEAD
                var localBranch = repo.Branches[branchName];
                if (localBranch == null)
                {
                    localBranch = repo.CreateBranch(branchName);
                }
                Commands.Checkout(repo, localBranch);
            }
        }
    }

    public void HardResetToRemote(string branchName)
    {
        using var repo = new Repository(_root);

        // Fetch from remote
        try
        {
            var remote = repo.Network.Remotes["origin"];
            if (remote != null)
            {
                Commands.Fetch(repo, remote.Name, Array.Empty<string>(), GetFetchOptions(), null);
            }
        }
        catch
        {
            // Ignore fetch errors
        }

        var remoteBranchName = $"origin/{branchName}";
        var remoteBranch = repo.Branches[remoteBranchName];

        if (remoteBranch != null)
        {
            var localBranch = repo.Branches[branchName];
            if (localBranch != null)
            {
                Commands.Checkout(repo, localBranch);
            }
            else
            {
                localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
                Commands.Checkout(repo, localBranch);
            }

            repo.Reset(ResetMode.Hard, remoteBranch.Tip);
            repo.RemoveUntrackedFiles();
        }
    }

    public bool CommitAndPush(string branchName, string message, IEnumerable<string> paths)
    {
        var pathList = paths.ToList();
        if (pathList.Count == 0)
        {
            return false;
        }

        using var repo = new Repository(_root);

        // Check if there are changes to commit BEFORE staging
        var status = repo.RetrieveStatus();
        var hasChanges = pathList.Any(path =>
        {
            var item = status.FirstOrDefault(s => s.FilePath == path);
            return item != null && item.State != FileStatus.Ignored && item.State != FileStatus.Unaltered;
        });

        if (!hasChanges)
        {
            Logger.WriteLine("No changes to commit.");
            return false;
        }

        // Fetch from remote BEFORE staging/committing to avoid rebase conflicts
        try
        {
            var remote = repo.Network.Remotes["origin"];
            if (remote != null)
            {
                Commands.Fetch(repo, remote.Name, Array.Empty<string>(), GetFetchOptions(), null);
            }
        }
        catch
        {
            // Ignore fetch errors
        }

        // Stash changes before rebase (rebase requires clean working tree)
        Stash? stash = null;
        var signature = new Signature(_cfg.GitAuthorName, _cfg.GitAuthorEmail, DateTimeOffset.Now);
        try
        {
            stash = repo.Stashes.Add(signature, "Pre-rebase stash", StashModifiers.IncludeUntracked);
        }
        catch
        {
            // Ignore stash errors (might be nothing to stash)
        }

        // Rebase local branch onto remote if remote has new commits
        var localBranch = repo.Branches[branchName];
        if (localBranch != null)
        {
            var remoteBranchName = $"origin/{branchName}";
            var remoteBranch = repo.Branches[remoteBranchName];

            if (remoteBranch != null && localBranch.Tip.Sha != remoteBranch.Tip.Sha)
            {
                try
                {
                    var rebaseOptions = new RebaseOptions
                    {
                        FileConflictStrategy = CheckoutFileConflictStrategy.Theirs
                    };

                    var identity = new Identity(_cfg.GitAuthorName, _cfg.GitAuthorEmail);
                    var rebaseResult = repo.Rebase.Start(localBranch, remoteBranch, null, identity, rebaseOptions);

                    if (rebaseResult.Status != RebaseStatus.Complete)
                    {
                        repo.Rebase.Abort();
                        throw new InvalidOperationException($"Rebase failed for {branchName} before commit");
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"Rebase before commit failed: {ex.Message}");
                    // Pop stash and abort - don't create divergent history
                    if (stash != null)
                    {
                        try
                        {
                            repo.Stashes.Pop(0);
                        }
                        catch
                        {
                            // Ignore pop errors
                        }
                    }
                    throw new InvalidOperationException($"git rebase failed for {branchName}: {ex.Message}", ex);
                }
            }
        }

        // Pop stash after successful rebase
        if (stash != null)
        {
            try
            {
                repo.Stashes.Pop(0);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Warning: Failed to pop stash: {ex.Message}");
            }
        }

        // Stage files AFTER fetch & rebase
        foreach (var path in pathList)
        {
            Commands.Stage(repo, path);
        }

        // Commit
        repo.Commit(message, signature, signature);

        // Push
        try
        {
            if (localBranch == null)
            {
                throw new InvalidOperationException($"Branch {branchName} not found");
            }

            repo.Branches.Update(localBranch, b => b.Remote = "origin", b => b.UpstreamBranch = $"refs/heads/{branchName}");

            var pushOptions = GetPushOptions();
            repo.Network.Push(localBranch, pushOptions);
        }
        catch (NonFastForwardException)
        {
            // Pull with rebase and retry push (fallback if pre-commit rebase failed)
            try
            {
                var remoteBranchName = $"origin/{branchName}";

                // Fetch
                var remote = repo.Network.Remotes["origin"];
                if (remote != null)
                {
                    Commands.Fetch(repo, remote.Name, Array.Empty<string>(), GetFetchOptions(), null);
                }

                var remoteBranch = repo.Branches[remoteBranchName];
                if (remoteBranch != null)
                {
                    var rebaseOptions = new RebaseOptions
                    {
                        FileConflictStrategy = CheckoutFileConflictStrategy.Theirs
                    };

                    var identity = new Identity(_cfg.GitAuthorName, _cfg.GitAuthorEmail);
                    var rebaseResult = repo.Rebase.Start(localBranch, remoteBranch, null, identity, rebaseOptions);

                    if (rebaseResult.Status != RebaseStatus.Complete)
                    {
                        repo.Rebase.Abort();
                        throw new InvalidOperationException($"git rebase failed for {branchName}");
                    }
                }

                var pushOptions = GetPushOptions();
                repo.Network.Push(localBranch, pushOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"git push failed for {branchName}: {ex.Message}", ex);
            }
        }

        return true;
    }

    private FetchOptions GetFetchOptions()
    {
        return new FetchOptions
        {
            CredentialsProvider = GetCredentialsHandler()
        };
    }

    private PushOptions GetPushOptions()
    {
        return new PushOptions
        {
            CredentialsProvider = GetCredentialsHandler()
        };
    }

    private CredentialsHandler GetCredentialsHandler()
    {
        return (url, user, cred) =>
        {
            var token = _cfg.GitHubToken;
            if (!string.IsNullOrWhiteSpace(token))
            {
                return new UsernamePasswordCredentials
                {
                    Username = "x-access-token",
                    Password = token
                };
            }
            return new DefaultCredentials();
        };
    }

    private string ResolveRemoteUrl()
    {
        if (!string.IsNullOrWhiteSpace(_cfg.GitRemoteUrl))
        {
            return _cfg.GitRemoteUrl;
        }

        if (!string.IsNullOrWhiteSpace(_cfg.GitHubToken) &&
            !string.IsNullOrWhiteSpace(_cfg.RepoOwner) &&
            !string.IsNullOrWhiteSpace(_cfg.RepoName))
        {
            return $"https://x-access-token:{_cfg.GitHubToken}@github.com/{_cfg.RepoOwner}/{_cfg.RepoName}.git";
        }

        return "";
    }

    private void MoveUntrackedGeneratedFiles()
    {
        if (!IsGitRepo())
        {
            return;
        }

        using var repo = new Repository(_root);
        var status = repo.RetrieveStatus();

        var candidates = status
            .Where(s => s.State == FileStatus.NewInWorkdir)
            .Select(s => s.FilePath)
            .Where(path =>
                !path.StartsWith(".orchestrator-backup/", StringComparison.OrdinalIgnoreCase) &&
                (path.StartsWith("plans/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("specs/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("reviews/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("questions/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("release/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var backupRoot = Path.Combine(_root, ".orchestrator-backup", stamp);
        Directory.CreateDirectory(backupRoot);

        foreach (var relative in candidates)
        {
            var source = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source))
            {
                continue;
            }

            var dest = Path.Combine(backupRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrWhiteSpace(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Move(source, dest, overwrite: true);
        }

        Logger.WriteLine($"Moved {candidates.Count} untracked orchestrator files to {backupRoot}.");
    }
}
