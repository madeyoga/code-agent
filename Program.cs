using DotNetEnv;
using DotNuxt;
using Microsoft.Agents.AI.Workflows;

ApplyWorkingDirectory(args);
LoadEnvironment();

var modelId = Env.GetString("MODEL_ID", "qwen3.6:35b-a3b");
var workspaceDir = Directory.GetCurrentDirectory();

var skillsDir = Path.Combine(AppContext.BaseDirectory, "skills");

Console.WriteLine("dotnuxt - .NET Coding Agent (Microsoft Agent Framework)");
Console.WriteLine($"Model: {modelId}");
Console.WriteLine($"Workspace: {workspaceDir}");
Console.WriteLine($"Skills directory: {skillsDir}");
Console.WriteLine();
Console.WriteLine("Usage: /build <prompt>  — Write code, create files, build projects");
Console.WriteLine("       /ask   <prompt>  — Answer questions, explain concepts, provide guidance");
Console.WriteLine("       /skills          — List available skills");
Console.WriteLine("       /plugins         — List available plugins");
Console.WriteLine("       /exit            — Quit\n");

// --- Single main prompt loop ---
var builderAgent = BuilderAgentFactory.Create(skillsDir);
var session = await builderAgent.CreateSessionAsync();
var questionAgent = BuilderAgentFactory.CreateQuestionAgent();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;

    var trimmed = input.Trim();
    if (IsCommand(trimmed, "exit")) break;
    if (IsCommand(trimmed, "skills")) { ListSkills(skillsDir); continue; }
    if (IsCommand(trimmed, "plugins")) { ListPlugins(skillsDir); continue; }

    // Parse command prefix
    var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    var command = parts[0].ToLowerInvariant();
    var prompt = parts.Length > 1 ? parts[1] : string.Empty;

    switch (command)
    {
        case "/build":
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Console.WriteLine("Error: /build requires a prompt. Example: /build Create a new ASP.NET Core controller");
                continue;
            }
            await foreach (var update in builderAgent.RunStreamingAsync(prompt, session))
            {
                Console.Write(update);
            }
            Console.WriteLine("\n");
            break;

        case "/ask":
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Console.WriteLine("Error: /ask requires a prompt. Example: /ask What is the tech stack of this project?");
                continue;
            }
            Console.WriteLine("\n---");
            await foreach (var update in questionAgent.RunStreamingAsync(prompt))
            {
                Console.Write(update);
            }
            Console.WriteLine("\n---\n");
            break;

        default:
            Console.WriteLine("Error: Unknown or missing command prefix. Use /build, /ask, /skills, /plugins, or /exit.");
            Console.WriteLine("Usage: /build <prompt>  — Write code, create files, build projects");
            Console.WriteLine("       /ask   <prompt>  — Answer questions, explain concepts, provide guidance");
            break;
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

static bool IsCommand(string input, string command)
{
    return input.Equals(command, StringComparison.OrdinalIgnoreCase)
        || input.Equals($"/{command}", StringComparison.OrdinalIgnoreCase);
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

static void ApplyWorkingDirectory(string[] args)
{
    var cwdIndex = Array.FindIndex(args, arg => arg.Equals("--cwd", StringComparison.OrdinalIgnoreCase));
    if (cwdIndex < 0)
        return;

    if (cwdIndex + 1 >= args.Length || string.IsNullOrWhiteSpace(args[cwdIndex + 1]))
        throw new ArgumentException("--cwd requires a directory path.");

    var requestedPath = Path.GetFullPath(args[cwdIndex + 1]);
    if (!Directory.Exists(requestedPath))
        throw new DirectoryNotFoundException($"Workspace directory not found: {requestedPath}");

    Directory.SetCurrentDirectory(requestedPath);
}

static void LoadEnvironment()
{
    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    if (!string.IsNullOrWhiteSpace(homeDir))
        LoadEnvFileIfPresent(Path.Combine(homeDir, ".dotnuxt", ".env"));

    LoadEnvFileIfPresent(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
}

static void LoadEnvFileIfPresent(string path)
{
    if (File.Exists(path))
        Env.Load(path);
}
