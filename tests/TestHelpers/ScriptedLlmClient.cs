using ModelContextProtocol.Client;
using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Infrastructure.Mcp;

namespace Orchestrator.App.Tests.TestHelpers;

public class ScriptedLlmClient : ILlmClient
{
    private readonly Func<string, string, string> _responseFactory;

    public ScriptedLlmClient(Func<string, string, string> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    public Task<string> GetUpdatedFileAsync(string model, string systemPrompt, string userPrompt)
    {
        var response = _responseFactory(systemPrompt, userPrompt);
        return Task.FromResult(response);
    }

    public Task<string> CompleteChatWithMcpToolsAsync(string model, string systemPrompt, string userPrompt, IEnumerable<McpClientTool> mcpTools, McpClientManager mcpManager)
    {
        var response = _responseFactory(systemPrompt, userPrompt);
        return Task.FromResult(response);
    }
}
