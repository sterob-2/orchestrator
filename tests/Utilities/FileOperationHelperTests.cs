using System.Threading.Tasks;
using Moq;
using Orchestrator.App;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Utilities;
using Xunit;

namespace Orchestrator.App.Tests.Utilities;

public class FileOperationHelperTests
{
    [Fact]
    public async Task ExistsAsync_WithMcp_UsesMcpFiles()
    {
        var mockMcp = new Mock<McpClientManager>();
        mockMcp.Setup(m => m.CallToolAsync("get_file_info", It.IsAny<System.Collections.Generic.IDictionary<string, object?>>()))
            .ReturnsAsync("file info");

        var ctx = MockWorkContext.Create();
        var ctxWithMcp = ctx with { Mcp = mockMcp.Object };

        var result = await FileOperationHelper.ExistsAsync(ctxWithMcp, "test.txt");

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithoutMcp_UsesWorkspace()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        System.IO.File.WriteAllText(System.IO.Path.Combine(workspace.Root, "test.txt"), "content");

        var ctx = MockWorkContext.Create(workspace: workspace);
        var ctxWithoutMcp = ctx with { Mcp = null };

        var result = await FileOperationHelper.ExistsAsync(ctxWithoutMcp, "test.txt");

        Assert.True(result);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task ReadAllTextAsync_WithMcp_UsesMcpFiles()
    {
        var mockMcp = new Mock<McpClientManager>();
        mockMcp.Setup(m => m.CallToolAsync("read_file", It.IsAny<System.Collections.Generic.IDictionary<string, object?>>()))
            .ReturnsAsync("file content from MCP");

        var ctx = MockWorkContext.Create();
        var ctxWithMcp = ctx with { Mcp = mockMcp.Object };

        var result = await FileOperationHelper.ReadAllTextAsync(ctxWithMcp, "test.txt");

        Assert.Equal("file content from MCP", result);
    }

    [Fact]
    public async Task ReadAllTextAsync_WithoutMcp_UsesWorkspace()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var filePath = System.IO.Path.Combine(workspace.Root, "test.txt");
        System.IO.File.WriteAllText(filePath, "workspace content");

        var ctx = MockWorkContext.Create(workspace: workspace);
        var ctxWithoutMcp = ctx with { Mcp = null };

        var result = await FileOperationHelper.ReadAllTextAsync(ctxWithoutMcp, "test.txt");

        Assert.Equal("workspace content", result);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task WriteAllTextAsync_WithMcp_UsesMcpFiles()
    {
        var mockMcp = new Mock<McpClientManager>();
        mockMcp.Setup(m => m.CallToolAsync("write_file", It.IsAny<System.Collections.Generic.IDictionary<string, object?>>()))
            .ReturnsAsync(string.Empty);

        var ctx = MockWorkContext.Create();
        var ctxWithMcp = ctx with { Mcp = mockMcp.Object };

        await FileOperationHelper.WriteAllTextAsync(ctxWithMcp, "test.txt", "new content");

        mockMcp.Verify(m => m.CallToolAsync("write_file",
            It.Is<System.Collections.Generic.IDictionary<string, object?>>(d =>
                d["path"]!.ToString() == "test.txt" &&
                d["content"]!.ToString() == "new content")),
            Times.Once);
    }

    [Fact]
    public async Task WriteAllTextAsync_WithoutMcp_UsesWorkspace()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var ctx = MockWorkContext.Create(workspace: workspace);
        var ctxWithoutMcp = ctx with { Mcp = null };

        await FileOperationHelper.WriteAllTextAsync(ctxWithoutMcp, "test.txt", "workspace write");

        var written = workspace.ReadAllText("test.txt");
        Assert.Equal("workspace write", written);

        // Cleanup
        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task ReadAllTextIfExistsAsync_FileExists_ReturnsContent()
    {
        var mockMcp = new Mock<McpClientManager>();
        mockMcp.Setup(m => m.CallToolAsync("get_file_info", It.IsAny<System.Collections.Generic.IDictionary<string, object?>>()))
            .ReturnsAsync("file info");
        mockMcp.Setup(m => m.CallToolAsync("read_file", It.IsAny<System.Collections.Generic.IDictionary<string, object?>>()))
            .ReturnsAsync("file content");

        var ctx = MockWorkContext.Create();
        var ctxWithMcp = ctx with { Mcp = mockMcp.Object };

        var result = await FileOperationHelper.ReadAllTextIfExistsAsync(ctxWithMcp, "test.txt");

        Assert.NotNull(result);
        Assert.Equal("file content", result);
    }

    [Fact]
    public async Task ReadAllTextIfExistsAsync_FileDoesNotExist_ReturnsNull()
    {
        var mockMcp = new Mock<McpClientManager>();
        mockMcp.Setup(m => m.CallToolAsync("get_file_info", It.IsAny<System.Collections.Generic.IDictionary<string, object?>>()))
            .ThrowsAsync(new System.InvalidOperationException("File not found"));

        var ctx = MockWorkContext.Create();
        var ctxWithMcp = ctx with { Mcp = mockMcp.Object };

        var result = await FileOperationHelper.ReadAllTextIfExistsAsync(ctxWithMcp, "nonexistent.txt");

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WithUnsafePath_Throws()
    {
        var ctx = MockWorkContext.Create();

        await Assert.ThrowsAsync<ArgumentException>(
            () => FileOperationHelper.ExistsAsync(ctx, "../secrets.txt"));
    }
}
