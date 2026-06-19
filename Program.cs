using DotNetEnv;
using DotNuxt;
using Microsoft.Agents.AI.Workflows;

Env.Load();

var modelId = Env.GetString("MODEL_ID", null) ?? throw new InvalidOperationException("NULL model id");

var skillsDir = Path.Combine(AppContext.BaseDirectory, "skills");

Console.WriteLine("dotnuxt - .NET Coding Agent (Microsoft Agent Framework)");
Console.WriteLine($"Model: {modelId}");
Console.WriteLine($"Skills directory: {skillsDir}");
Console.WriteLine();

// --- Menu prompt ---
Console.Write("Choose agent mode: [B] Coding/Builder only, [A] Ask/question (default B): ");
var choice = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "C";
if (!choice.StartsWith('B') && !choice.StartsWith('A'))
{
    Console.WriteLine("Invalid choice. Defaulting to Coding mode.");
    choice = "B";
}

Console.WriteLine();

// --- Route to selected agent mode ---
switch (choice[0])
{
    case 'B':
        await RunBuilderMode(skillsDir);
        break;
    case 'A':
        await RunAskMode();
        break;
}

// --- Builder-only mode: direct builder loop ---
static async Task RunBuilderMode(string skillsDir)
{
    var builderAgent = BuilderAgentFactory.Create(skillsDir);
    var session = await builderAgent.CreateSessionAsync();
    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) continue;
        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
        if (input.Equals("skills", StringComparison.OrdinalIgnoreCase)) { ListSkills(skillsDir); continue; }
        if (input.Equals("plugins", StringComparison.OrdinalIgnoreCase)) { ListPlugins(skillsDir); continue; }

        await foreach (var update in builderAgent.RunStreamingAsync(input, session))
        {
            Console.Write(update);
        }
        Console.WriteLine("\n");
    }
}

// --- Ask/question mode: direct answer executor only ---
static async Task RunAskMode()
{
    var questionAgent = BuilderAgentFactory.CreateQuestionAgent();
    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) continue;
        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

        Console.WriteLine("\n---");
        await foreach (var update in questionAgent.RunStreamingAsync(input))
        {
            Console.Write(update);
        }
        Console.WriteLine("\n---\n");
    }
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

