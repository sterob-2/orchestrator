using System.IO;
using Orchestrator.App.Infrastructure.Git;
using Orchestrator.App.Tests.TestHelpers;
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
        var root = FindGitRoot() ?? Directory.GetCurrentDirectory();

        var repoGit = new RepoGit(config, root);

        var result = repoGit.IsGitRepo();

        // Should be true if we're in a git repository
        Assert.True(result || !Directory.Exists(Path.Combine(root, ".git")));
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
