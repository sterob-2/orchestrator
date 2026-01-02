using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;

namespace Orchestrator.App;

internal sealed class LlmClient
{
    private readonly OpenAIClient _openAiClient;

    public LlmClient(OrchestratorConfig cfg)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(cfg.OpenAiBaseUrl)
        };

        _openAiClient = new OpenAIClient(new ApiKeyCredential(cfg.OpenAiApiKey), options);
    }

    /// <summary>
    /// Standard chat completion without tool calling.
    /// </summary>
    public async Task<string> GetUpdatedFileAsync(string model, string systemPrompt, string userPrompt)
    {
        var temperature = model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase) ? 1f : 0.2f;

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = temperature
        };

        var chatClient = _openAiClient.GetChatClient(model);
        var completion = await chatClient.CompleteChatAsync(messages, options);

        return completion.Value.Content[0].Text ?? "";
    }

    /// <summary>
    /// Chat completion with MCP tool calling support.
    /// NOTE: This is a stub implementation. Tool calling integration requires additional
    /// research into the MCP Client API for proper tool schema conversion and invocation.
    /// See docs/MCP_AGENT_MIGRATION_PLAN.md Phase 2.1 for implementation details.
    /// </summary>
    public async Task<string> CompleteChatWithMcpToolsAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        IEnumerable<McpClientTool> mcpTools)
    {
        // TODO: Implement tool calling
        // Current blockers:
        // 1. Need to convert McpClientTool to OpenAI ChatTool format (schema conversion)
        // 2. Need to properly invoke MCP tools with parsed arguments
        // 3. Need to handle tool call loop with message history
        //
        // For now, fall back to standard completion
        Logger.WriteLine("[LLM] Warning: MCP tool calling not yet implemented, using standard completion");

        return await GetUpdatedFileAsync(model, systemPrompt, userPrompt);
    }
}
