using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace DotNuxt;

public static class AgentClientFactory
{
    public static IChatClient CreateChatClient(string? modelId = null)
    {
        modelId ??= DotNetEnv.Env.GetString("MODEL_ID", "qwen3.6:35b-a3b");
        var apiKey = DotNetEnv.Env.GetString("API_KEY", "ollama");
        var endpoint = DotNetEnv.Env.GetString("OPENAI_ENDPOINT", "http://localhost:11434/v1/");

        var openAiClient = !string.IsNullOrEmpty(endpoint)
            ? new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
            : new OpenAIClient(new ApiKeyCredential(apiKey));

        return openAiClient.GetChatClient(modelId).AsIChatClient();
    }
}
