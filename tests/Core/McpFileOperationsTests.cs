using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Orchestrator.App;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class McpFileOperationsTests
{
    [Fact]
    public void Constructor_NullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new McpFileOperations(null!));
    }

    [Fact]
    public async Task ReadAllTextAsync_CallsReadFileTool()
    {
        var mockManager = new Mock<McpClientManager>();
        var expectedContent = "file content";

        mockManager.Setup(m => m.CallToolAsync(
            "read_file",
            It.Is<IDictionary<string, object?>>(args => args["path"]!.ToString() == "test.txt")))
            .ReturnsAsync(expectedContent);

        var fileOps = new McpFileOperations(mockManager.Object);
        var result = await fileOps.ReadAllTextAsync("test.txt");

        Assert.Equal(expectedContent, result);
        mockManager.Verify(m => m.CallToolAsync(
            "read_file",
            It.Is<IDictionary<string, object?>>(args => args["path"]!.ToString() == "test.txt")),
            Times.Once);
    }

    [Fact]
    public async Task WriteAllTextAsync_CallsWriteFileTool()
    {
        var mockManager = new Mock<McpClientManager>();

        mockManager.Setup(m => m.CallToolAsync(
            "write_file",
            It.Is<IDictionary<string, object?>>(args =>
                args["path"]!.ToString() == "test.txt" &&
                args["content"]!.ToString() == "new content")))
            .ReturnsAsync(string.Empty);

        var fileOps = new McpFileOperations(mockManager.Object);
        await fileOps.WriteAllTextAsync("test.txt", "new content");

        mockManager.Verify(m => m.CallToolAsync(
            "write_file",
            It.Is<IDictionary<string, object?>>(args =>
                args["path"]!.ToString() == "test.txt" &&
                args["content"]!.ToString() == "new content")),
            Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_FileExists_ReturnsTrue()
    {
        var mockManager = new Mock<McpClientManager>();

        mockManager.Setup(m => m.CallToolAsync(
            "get_file_info",
            It.Is<IDictionary<string, object?>>(args => args["path"]!.ToString() == "test.txt")))
            .ReturnsAsync("file info");

        var fileOps = new McpFileOperations(mockManager.Object);
        var result = await fileOps.ExistsAsync("test.txt");

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_FileDoesNotExist_ReturnsFalse()
    {
        var mockManager = new Mock<McpClientManager>();

        mockManager.Setup(m => m.CallToolAsync(
            "get_file_info",
            It.Is<IDictionary<string, object?>>(args => args["path"]!.ToString() == "nonexistent.txt")))
            .ThrowsAsync(new InvalidOperationException("File not found"));

        var fileOps = new McpFileOperations(mockManager.Object);
        var result = await fileOps.ExistsAsync("nonexistent.txt");

        Assert.False(result);
    }

    [Fact]
    public async Task ListFilesAsync_ReturnsFileList()
    {
        var mockManager = new Mock<McpClientManager>();
        var directoryListing = "file1.txt\nfile2.txt\nfile3.txt";

        mockManager.Setup(m => m.CallToolAsync(
            "list_directory",
            It.Is<IDictionary<string, object?>>(args => args["path"]!.ToString() == "/test")))
            .ReturnsAsync(directoryListing);

        var fileOps = new McpFileOperations(mockManager.Object);
        var result = await fileOps.ListFilesAsync("/test");

        Assert.Equal(3, result.Length);
        Assert.Equal("file1.txt", result[0]);
        Assert.Equal("file2.txt", result[1]);
        Assert.Equal("file3.txt", result[2]);
    }

    [Fact]
    public async Task ListFilesAsync_EmptyDirectory_ReturnsEmptyArray()
    {
        var mockManager = new Mock<McpClientManager>();

        mockManager.Setup(m => m.CallToolAsync(
            "list_directory",
            It.Is<IDictionary<string, object?>>(args => args["path"]!.ToString() == "/empty")))
            .ReturnsAsync(string.Empty);

        var fileOps = new McpFileOperations(mockManager.Object);
        var result = await fileOps.ListFilesAsync("/empty");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListFilesAsync_WithPattern_PassesPathParameter()
    {
        var mockManager = new Mock<McpClientManager>();

        mockManager.Setup(m => m.CallToolAsync(
            "list_directory",
            It.Is<IDictionary<string, object?>>(args => args["path"]!.ToString() == "/test")))
            .ReturnsAsync("file1.cs\nfile2.cs");

        var fileOps = new McpFileOperations(mockManager.Object);
        var result = await fileOps.ListFilesAsync("/test", "*.cs");

        Assert.Equal(2, result.Length);
        mockManager.Verify(m => m.CallToolAsync(
            "list_directory",
            It.Is<IDictionary<string, object?>>(args => args["path"]!.ToString() == "/test")),
            Times.Once);
    }
}
