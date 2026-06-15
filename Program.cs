using System.ClientModel;
using DotNetEnv;
using DotNuxt.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

Env.Load();

var modelId = Env.GetString("MODEL_ID", "deepseek-v4-flash");
var apiKey = Env.GetString("API_KEY", "");
var endpoint = Env.GetString("OPENAI_ENDPOINT", "");

var skillsDir = Path.Combine(AppContext.BaseDirectory, "skills");

var openAiClient = !string.IsNullOrEmpty(endpoint)
    ? new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    : new OpenAIClient(new ApiKeyCredential(apiKey));

var chatClient = openAiClient.GetChatClient(modelId).AsIChatClient();

#pragma warning disable MAAI001
var skillsProvider = new AgentSkillsProvider(
    Path.Combine(AppContext.BaseDirectory, "skills")
);
#pragma warning restore MAAI001

var agent = chatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = BuildSystemPrompt(skillsDir),
            Tools = [
                AIFunctionFactory.Create(AgentTools.GetSkill), 
                AIFunctionFactory.Create(AgentTools.Grep), 
                AIFunctionFactory.Create(AgentTools.ReadFile)
            ],
        },
        AIContextProviders = [skillsProvider]
    }
);


Console.WriteLine("dotnuxt - .NET Coding Agent (Microsoft Agent Framework)");
Console.WriteLine($"Model: {modelId}");
Console.WriteLine($"Skills directory: {skillsDir}");
Console.WriteLine("Type 'exit' to quit, 'skills' to list, 'plugins' to list plugins.\n");

var session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    if (input.Equals("skills", StringComparison.OrdinalIgnoreCase)) { ListSkills(skillsDir); continue; }
    if (input.Equals("plugins", StringComparison.OrdinalIgnoreCase)) { ListPlugins(skillsDir); continue; }

    await foreach (var update in agent.RunStreamingAsync(input))
    {
        Console.Write(update);
    }
    Console.WriteLine();
}

static string BuildSystemPrompt(string skillsDir)
{
    var pluginCatalog = AgentTools.GetSkill(skillsDir);
    return $$"""
        You are an expert .NET coding assistant. You have access to official .NET skills
        that provide detailed guidance on specific .NET topics. 

        When a user asks about something .NET-related:
        0. Answer concisely. Do not answer when its not .net-related question.
        1. First use `get_skill` to load the relevant skill(s)
        2. Follow the skill's guidance carefully
        3. Write clean, idiomatic C# code following modern .NET conventions

        If no skill matches the request, use your general .NET knowledge.
        """;
}

static void ListSkills(string skillsDir)
{
    if (!Directory.Exists(skillsDir)) { Console.WriteLine("No skills directory.\n"); return; }
    Console.WriteLine();
    foreach (var f in Directory.GetFiles(skillsDir, "SKILL.md", SearchOption.AllDirectories))
        Console.WriteLine($"  {Path.GetRelativePath(skillsDir, Path.GetDirectoryName(f)!)}");
    Console.WriteLine();
}

static void ListPlugins(string skillsDir)
{
    if (!Directory.Exists(skillsDir)) { Console.WriteLine("No skills directory.\n"); return; }
    Console.WriteLine();
    foreach (var pluginDir in Directory.GetDirectories(skillsDir))
    {
        var name = Path.GetFileName(pluginDir);
        var count = Directory.GetFiles(pluginDir, "SKILL.md", SearchOption.AllDirectories).Length;
        Console.WriteLine($"  {name} ({count} skills)");
    }
    Console.WriteLine();
}

