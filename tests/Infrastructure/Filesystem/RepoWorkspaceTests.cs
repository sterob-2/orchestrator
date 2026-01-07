using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orchestrator.App.Infrastructure.Filesystem;
using Xunit;

namespace Orchestrator.App.Tests.Infrastructure.Filesystem;

public class RepoWorkspaceTests
{
    [Fact]
    public void Constructor_SetsRoot()
    {
        var root = "/test/path";

        var workspace = new RepoWorkspace(root);

        Assert.Equal(root, workspace.Root);
    }

    [Fact]
    public void ResolvePath_CombinesRootAndRelativePath()
    {
        var workspace = new RepoWorkspace("/root");

        var result = workspace.ResolvePath("folder/file.txt");

        Assert.Equal(Path.Combine("/root", "folder", "file.txt"), result);
    }

    [Fact]
    public void ResolvePath_HandlesForwardSlashes()
    {
        var workspace = new RepoWorkspace("/root");

        var result = workspace.ResolvePath("folder/subfolder/file.txt");

        var expected = Path.Combine("/root", "folder", "subfolder", "file.txt");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolvePath_RejectsPathTraversal()
    {
        var workspace = new RepoWorkspace("/root");

        Assert.Throws<InvalidOperationException>(() => workspace.ResolvePath("../secrets.txt"));
    }

    [Fact]
    public void Exists_WithExistingFile_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var testFile = "test.txt";
        var fullPath = Path.Combine(tempDir, testFile);
        File.WriteAllText(fullPath, "content");

        try
        {
            var workspace = new RepoWorkspace(tempDir);

            var result = workspace.Exists(testFile);

            Assert.True(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Exists_WithNonExistentFile_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var workspace = new RepoWorkspace(tempDir);

            var result = workspace.Exists("nonexistent.txt");

            Assert.False(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ReadAllText_ReadsFileContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var testFile = "test.txt";
        var content = "test content";
        File.WriteAllText(Path.Combine(tempDir, testFile), content);

        try
        {
            var workspace = new RepoWorkspace(tempDir);

            var result = workspace.ReadAllText(testFile);

            Assert.Equal(content, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void WriteAllText_CreatesFileWithContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var testFile = "test.txt";
        var content = "new content";

        try
        {
            var workspace = new RepoWorkspace(tempDir);

            workspace.WriteAllText(testFile, content);

            var writtenContent = File.ReadAllText(Path.Combine(tempDir, testFile));
            Assert.Equal(content, writtenContent);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void WriteAllText_CreatesDirectoryIfNotExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var testFile = "subfolder/test.txt";
        var content = "content";

        try
        {
            var workspace = new RepoWorkspace(tempDir);

            workspace.WriteAllText(testFile, content);

            Assert.True(File.Exists(Path.Combine(tempDir, "subfolder", "test.txt")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ListFiles_ReturnsMatchingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "test1.txt"), "");
        File.WriteAllText(Path.Combine(tempDir, "test2.txt"), "");
        File.WriteAllText(Path.Combine(tempDir, "other.md"), "");

        try
        {
            var workspace = new RepoWorkspace(tempDir);

            var files = workspace.ListFiles("", "*.txt", 10).ToList();

            Assert.Equal(2, files.Count);
            Assert.Contains(files, f => f.Contains("test1.txt"));
            Assert.Contains(files, f => f.Contains("test2.txt"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ListFiles_RespectsMaxLimit()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        for (int i = 0; i < 5; i++)
        {
            File.WriteAllText(Path.Combine(tempDir, $"test{i}.txt"), "");
        }

        try
        {
            var workspace = new RepoWorkspace(tempDir);

            var files = workspace.ListFiles("", "*.txt", 3).ToList();

            Assert.Equal(3, files.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ListFiles_WithNonExistentDirectory_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var workspace = new RepoWorkspace(tempDir);

            var files = workspace.ListFiles("nonexistent", "*.txt", 10);

            Assert.Empty(files);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ReadOrTemplate_WithExistingFile_ReadsFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "existing.txt"), "{{TOKEN}} value");
        File.WriteAllText(Path.Combine(tempDir, "template.txt"), "template");

        try
        {
            var workspace = new RepoWorkspace(tempDir);
            var tokens = new Dictionary<string, string> { { "{{TOKEN}}", "replaced" } };

            var result = workspace.ReadOrTemplate("existing.txt", "template.txt", tokens);

            Assert.Equal("replaced value", result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ReadOrTemplate_WithNonExistentFile_ReadsTemplate()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "template.txt"), "{{TOKEN}} template");

        try
        {
            var workspace = new RepoWorkspace(tempDir);
            var tokens = new Dictionary<string, string> { { "{{TOKEN}}", "replaced" } };

            var result = workspace.ReadOrTemplate("nonexistent.txt", "template.txt", tokens);

            Assert.Equal("replaced template", result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
