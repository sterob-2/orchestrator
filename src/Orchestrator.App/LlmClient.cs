using System;
using System.ClientModel;
using System.Threading.Tasks;
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
}
