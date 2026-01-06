using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orchestrator.App;
using Orchestrator.App.Agents;
using Orchestrator.App.Tests.TestHelpers;
using Xunit;

namespace Orchestrator.App.Tests.Agents;

public class ToolEnabledAgentBaseTests
{
    private class TestAgent : ToolEnabledAgentBase
    {
        public override Task<AgentResult> RunAsync(WorkContext ctx)
        {
            return Task.FromResult(AgentResult.Ok("Test completed"));
        }

        public string TestBuildSystemPromptWithTools(string basePrompt)
        {
            return BuildSystemPromptWithTools(basePrompt, Enumerable.Empty<ModelContextProtocol.Client.McpClientTool>());
        }
    }

    [Fact]
    public void BuildSystemPromptWithTools_NoTools_ReturnsBasePrompt()
    {
        var agent = new TestAgent();
        var basePrompt = "Base system prompt";

        var result = agent.TestBuildSystemPromptWithTools(basePrompt);

        Assert.Equal(basePrompt, result);
    }

    [Fact]
    public async Task RunAsync_ReturnsExpectedResult()
    {
        var agent = new TestAgent();
        var ctx = MockWorkContext.Create();

        var result = await agent.RunAsync(ctx);

        Assert.True(result.Success);
        Assert.Equal("Test completed", result.Notes);
    }
}
