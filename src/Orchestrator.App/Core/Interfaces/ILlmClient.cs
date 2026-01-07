using ModelContextProtocol.Client;
using Orchestrator.App.Infrastructure.Mcp;

namespace Orchestrator.App.Core.Interfaces;

internal interface ILlmClient
{
    Task<string> GetUpdatedFileAsync(string model, string systemPrompt, string userPrompt);
    Task<string> CompleteChatWithMcpToolsAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        IEnumerable<McpClientTool> mcpTools,
        McpClientManager mcpManager);
}
