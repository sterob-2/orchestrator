using System;
using System.Threading.Tasks;
using Orchestrator.App;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class McpClientManagerTests
{
    [Fact]
    public void Tools_InitiallyEmpty()
    {
        var manager = new McpClientManager();

        Assert.Empty(manager.Tools);
    }

    [Fact]
    public void GetToolsByServer_NoTools_ReturnsEmpty()
    {
        var manager = new McpClientManager();

        var tools = manager.GetToolsByServer("filesystem");

        Assert.Empty(tools);
    }

    [Fact]
    public void Constructor_InitializesSuccessfully()
    {
        var manager = new McpClientManager();

        Assert.NotNull(manager);
        Assert.Empty(manager.Tools);
    }


    [Fact]
    public async Task CallToolAsync_ToolNotFound_ThrowsException()
    {
        var manager = new McpClientManager();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.CallToolAsync("nonexistent_tool", new System.Collections.Generic.Dictionary<string, object?>())
        );

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_DisposesCleanly()
    {
        var manager = new McpClientManager();

        await manager.DisposeAsync();

        Assert.Empty(manager.Tools);
    }
}
