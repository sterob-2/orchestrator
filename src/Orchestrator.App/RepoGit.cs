using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace Orchestrator.App;

internal sealed class RepoGit
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
                    repo.Reset(ResetMode.Hard, baseRef.Tip);
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

        // Stage files
        foreach (var path in pathList)
        {
            Commands.Stage(repo, path);
        }

        // Check if there are staged changes
        var status = repo.RetrieveStatus();
        if (!status.Any(s => s.State != FileStatus.Ignored && s.State != FileStatus.Unaltered))
        {
            Logger.WriteLine("No changes to commit.");
            return false;
        }

        // Commit
        var signature = new Signature(_cfg.GitAuthorName, _cfg.GitAuthorEmail, DateTimeOffset.Now);
        repo.Commit(message, signature, signature);

        // Push
        try
        {
            var localBranch = repo.Branches[branchName];
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
            // Pull with rebase and retry push
            try
            {
                var localBranch = repo.Branches[branchName];
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
                (path.StartsWith("orchestrator/plans/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("orchestrator/specs/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("orchestrator/reviews/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("orchestrator/questions/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("orchestrator/release/", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("orchestrator/tests/", StringComparison.OrdinalIgnoreCase)))
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
