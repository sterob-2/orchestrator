using System.Threading.Tasks;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Utilities;
using Xunit;

namespace Orchestrator.App.Tests.Utilities;

public class FileOperationHelperTests
{
    [Fact]
    public async Task ExistsAsync_FileExists_ReturnsTrue()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        System.IO.File.WriteAllText(System.IO.Path.Combine(workspace.Root, "test.txt"), "content");

        var ctx = MockWorkContext.Create(workspace: workspace);

        var result = await FileOperationHelper.ExistsAsync(ctx, "test.txt");

        Assert.True(result);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task ExistsAsync_FileDoesNotExist_ReturnsFalse()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var ctx = MockWorkContext.Create(workspace: workspace);

        var result = await FileOperationHelper.ExistsAsync(ctx, "nonexistent.txt");

        Assert.False(result);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task ReadAllTextAsync_ReadsContent()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var filePath = System.IO.Path.Combine(workspace.Root, "test.txt");
        System.IO.File.WriteAllText(filePath, "workspace content");

        var ctx = MockWorkContext.Create(workspace: workspace);

        var result = await FileOperationHelper.ReadAllTextAsync(ctx, "test.txt");

        Assert.Equal("workspace content", result);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task WriteAllTextAsync_WritesContent()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var ctx = MockWorkContext.Create(workspace: workspace);

        await FileOperationHelper.WriteAllTextAsync(ctx, "test.txt", "workspace write");

        var written = workspace.ReadAllText("test.txt");
        Assert.Equal("workspace write", written);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task ReadAllTextIfExistsAsync_FileExists_ReturnsContent()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        System.IO.File.WriteAllText(System.IO.Path.Combine(workspace.Root, "test.txt"), "file content");

        var ctx = MockWorkContext.Create(workspace: workspace);

        var result = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, "test.txt");

        Assert.NotNull(result);
        Assert.Equal("file content", result);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task ReadAllTextIfExistsAsync_FileDoesNotExist_ReturnsNull()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var ctx = MockWorkContext.Create(workspace: workspace);

        var result = await FileOperationHelper.ReadAllTextIfExistsAsync(ctx, "nonexistent.txt");

        Assert.Null(result);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task DeleteAsync_DeletesFile()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        System.IO.File.WriteAllText(System.IO.Path.Combine(workspace.Root, "test.txt"), "content");

        var ctx = MockWorkContext.Create(workspace: workspace);

        await FileOperationHelper.DeleteAsync(ctx, "test.txt");

        Assert.False(workspace.Exists("test.txt"));

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task ExistsAsync_WithUnsafePath_Throws()
    {
        var ctx = MockWorkContext.Create();

        await Assert.ThrowsAsync<ArgumentException>(
            () => FileOperationHelper.ExistsAsync(ctx, "../secrets.txt"));
    }
}
