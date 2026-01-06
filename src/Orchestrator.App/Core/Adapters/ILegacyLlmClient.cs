using ModelContextProtocol.Client;

namespace Orchestrator.App.Core.Adapters;

internal interface ILegacyLlmClient
{
    Task<string> GetUpdatedFileAsync(string model, string systemPrompt, string userPrompt);
    Task<string> CompleteChatWithMcpToolsAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        IEnumerable<McpClientTool> mcpTools,
        global::Orchestrator.App.McpClientManager mcpManager);
}
