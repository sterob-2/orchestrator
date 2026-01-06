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

        repoGit.EnsureConfigured();

        // Verify configuration was set
        using var repo = new Repository(gitRoot);
        var userName = repo.Config.Get<string>("user.name");
        var userEmail = repo.Config.Get<string>("user.email");

        Assert.NotNull(userName);
        Assert.NotNull(userEmail);
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
