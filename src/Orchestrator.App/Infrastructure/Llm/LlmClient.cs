using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;

namespace Orchestrator.App.Infrastructure.Llm;

public sealed class LlmClient
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

        var messages = new OpenAI.Chat.ChatMessage[]
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
    /// Uses Microsoft.Extensions.AI for automatic function invocation.
    /// </summary>
    public async Task<string> CompleteChatWithMcpToolsAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        IEnumerable<McpClientTool> mcpTools,
        McpClientManager mcpManager)
    {
        var toolsList = mcpTools.ToList();
        if (toolsList.Count == 0)
        {
            return await GetUpdatedFileAsync(model, systemPrompt, userPrompt);
        }

        Logger.WriteLine($"[LLM] Using MCP tool calling with {toolsList.Count} tools");

        // Convert MCP tools to AIFunction objects
        var aiFunctions = new List<AIFunction>();
        foreach (var tool in toolsList)
        {
            // Create a function that calls the MCP tool via the manager
            var function = AIFunctionFactory.Create(
                async (IDictionary<string, object?> args) =>
                {
                    Logger.WriteLine($"[LLM] Invoking MCP tool: {tool.Name}");
                    try
                    {
                        var result = await mcpManager.CallToolAsync(tool.Name, args);
                        Logger.WriteLine($"[LLM] Tool {tool.Name} result: {result.Substring(0, Math.Min(100, result.Length))}...");
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        // Preserve cancellation semantics (includes TaskCanceledException)
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"[LLM] Error invoking tool {tool.Name}: {ex.Message}");
                        return $"Error: {ex.Message}";
                    }
                },
                tool.Name,
                tool.Description ?? $"MCP tool: {tool.Name}");

            aiFunctions.Add(function);
        }

        // Create IChatClient with function invocation enabled
        var baseChatClient = _openAiClient.GetChatClient(model).AsIChatClient();
        var chatClient = new ChatClientBuilder(baseChatClient)
            .UseFunctionInvocation()
            .Build();

        // Prepare messages
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        // Prepare options with tools
        var temperature = model.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase) ? 1f : 0.2f;
        var options = new ChatOptions
        {
            Temperature = temperature,
            Tools = aiFunctions.Cast<AITool>().ToList()
        };

        // Get completion with automatic tool invocation
        var response = await chatClient.GetResponseAsync(messages, options);

        return response.Text ?? "";
    }
}
