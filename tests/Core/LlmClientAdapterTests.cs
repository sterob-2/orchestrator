using System.Collections.Generic;
using Moq;
using ModelContextProtocol.Client;
using Orchestrator.App.Core.Adapters;
using Xunit;

namespace Orchestrator.App.Tests.Core;

public class LlmClientAdapterTests
{
    [Fact]
    public async Task Adapter_ForwardsLlmCalls()
    {
        var legacy = new Mock<ILegacyLlmClient>(MockBehavior.Strict);
        legacy.Setup(c => c.GetUpdatedFileAsync("model", "system", "user"))
            .ReturnsAsync("updated");
        legacy.Setup(c => c.CompleteChatWithMcpToolsAsync(
                "model2",
                "system2",
                "user2",
                It.IsAny<IEnumerable<McpClientTool>>(),
                It.IsAny<global::Orchestrator.App.McpClientManager>()))
            .ReturnsAsync("completed");

        var adapter = new LlmClientAdapter(legacy.Object);

        var updated = await adapter.GetUpdatedFileAsync("model", "system", "user");
        Assert.Equal("updated", updated);

        var tools = Array.Empty<McpClientTool>();
        var manager = new global::Orchestrator.App.McpClientManager();
        var completed = await adapter.CompleteChatWithMcpToolsAsync("model2", "system2", "user2", tools, manager);
        Assert.Equal("completed", completed);

        legacy.VerifyAll();
    }
}
