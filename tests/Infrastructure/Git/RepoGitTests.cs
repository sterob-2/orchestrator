using System.IO;
using Orchestrator.App.Infrastructure.Git;
using Orchestrator.App.Tests.TestHelpers;
using LibGit2Sharp;
using Xunit;

namespace Orchestrator.App.Tests.Infrastructure.Git;

public class RepoGitTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var config = MockWorkContext.CreateConfig();
        var root = Directory.GetCurrentDirectory();

        var repoGit = new RepoGit(config, root);

        Assert.NotNull(repoGit);
    }

    [Fact]
    public void IsGitRepo_WithValidGitDirectory_ReturnsTrue()
    {
        var config = MockWorkContext.CreateConfig();
        var root = FindGitRoot();

        if (root != null)
        {
            var repoGit = new RepoGit(config, root);
            var result = repoGit.IsGitRepo();
            Assert.True(result);
        }
    }

    [Fact]
    public void IsGitRepo_WithNonGitDirectory_ReturnsFalse()
    {
        var config = MockWorkContext.CreateConfig();
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var repoGit = new RepoGit(config, tempDir);

            var result = repoGit.IsGitRepo();

            Assert.False(result);
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
    public void EnsureConfigured_WithNonGitDirectory_DoesNotThrow()
    {
        var config = MockWorkContext.CreateConfig();
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var repoGit = new RepoGit(config, tempDir);

            // Should not throw even if not a git repo
            repoGit.EnsureConfigured();

            Assert.True(true); // If we got here, no exception was thrown
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
    public void EnsureConfigured_WithValidGitRepo_ConfiguresUserInfo()
    {
        var gitRoot = FindGitRoot();
        if (gitRoot == null)
        {
            // Skip if not in git repo
            return;
        }

        var config = MockWorkContext.CreateConfig();
        var repoGit = new RepoGit(config, gitRoot);

        string? originalRemoteUrl;
        string? originalUserName;
        string? originalUserEmail;
        using (var repo = new Repository(gitRoot))
        {
            var origin = repo.Network.Remotes["origin"];
            originalRemoteUrl = origin?.Url;
            originalUserName = repo.Config.Get<string>("user.name")?.Value;
            originalUserEmail = repo.Config.Get<string>("user.email")?.Value;
        }

        repoGit.EnsureConfigured();

        // Verify configuration was set
        try
        {
            using var repo = new Repository(gitRoot);
            var userName = repo.Config.Get<string>("user.name");
            var userEmail = repo.Config.Get<string>("user.email");

            Assert.NotNull(userName);
            Assert.NotNull(userEmail);
        }
        finally
        {
            using var repo = new Repository(gitRoot);
            if (string.IsNullOrWhiteSpace(originalUserName))
            {
                repo.Config.Unset("user.name");
            }
            else
            {
                repo.Config.Set("user.name", originalUserName);
            }

            if (string.IsNullOrWhiteSpace(originalUserEmail))
            {
                repo.Config.Unset("user.email");
            }
            else
            {
                repo.Config.Set("user.email", originalUserEmail);
            }

            var origin = repo.Network.Remotes["origin"];
            if (origin != null)
            {
                if (string.IsNullOrWhiteSpace(originalRemoteUrl))
                {
                    repo.Network.Remotes.Remove("origin");
                }
                else
                {
                    repo.Network.Remotes.Update("origin", r => r.Url = originalRemoteUrl);
                }
            }
        }
    }

    private static string? FindGitRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }
}
