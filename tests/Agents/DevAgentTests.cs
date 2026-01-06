using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Orchestrator.App;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Agents;

public class DevAgentTests
{
    [Fact]
    public async Task RunAsync_SpecFileMissing_ReturnsSpecQuestionResult()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Test Issue");
        var ctx = MockWorkContext.Create(workspace: workspace, workItem: workItem);
        var agent = new DevAgent();

        var result = await agent.RunAsync(ctx);

        Assert.True(result.Success);
        Assert.NotNull(result.Notes);
        Assert.Contains(ctx.Config.SpecQuestionsLabel, result.AddLabels ?? Array.Empty<string>());

        System.IO.Directory.Delete(workspace.Root, true);
    }

    [Fact]
    public async Task RunAsync_WithMcp_SpecFileMissing_ReturnsSpecQuestionResult()
    {
        var workspace = MockWorkContext.CreateWorkspace();
        var mockMcp = new Mock<McpClientManager>();
        mockMcp.Setup(m => m.CallToolAsync("get_file_info", It.IsAny<IDictionary<string, object?>>()))
            .ThrowsAsync(new InvalidOperationException("File not found"));

        var workItem = MockWorkContext.CreateWorkItem(number: 1, title: "Test Issue");
        var ctx = MockWorkContext.Create(workspace: workspace, workItem: workItem);
        var ctxWithMcp = ctx with { Mcp = mockMcp.Object };
        var agent = new DevAgent();

        var result = await agent.RunAsync(ctxWithMcp);

        Assert.True(result.Success);
        Assert.NotNull(result.Notes);

        System.IO.Directory.Delete(workspace.Root, true);
    }
}
