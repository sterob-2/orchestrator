using ModelContextProtocol.Client;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Core.Adapters;

internal sealed class LlmClientAdapter : ILlmClient
{
    private readonly global::Orchestrator.App.LlmClient _client;

    public LlmClientAdapter(global::Orchestrator.App.LlmClient client)
    {
        _client = client;
    }

    public Task<string> GetUpdatedFileAsync(string model, string systemPrompt, string userPrompt)
    {
        return _client.GetUpdatedFileAsync(model, systemPrompt, userPrompt);
    }

    public Task<string> CompleteChatWithMcpToolsAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        IEnumerable<McpClientTool> mcpTools,
        global::Orchestrator.App.McpClientManager mcpManager)
    {
        return _client.CompleteChatWithMcpToolsAsync(model, systemPrompt, userPrompt, mcpTools, mcpManager);
    }
}
