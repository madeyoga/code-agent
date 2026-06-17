using DotNetEnv;
using DotNuxt;
using Microsoft.Agents.AI.Workflows;

Env.Load();

var modelId = Env.GetString("MODEL_ID", "gemma4:12b");

var skillsDir = Path.Combine(AppContext.BaseDirectory, "skills");

Console.WriteLine("dotnuxt - .NET Coding Agent (Microsoft Agent Framework)");
Console.WriteLine($"Model: {modelId}");
Console.WriteLine($"Skills directory: {skillsDir}");
Console.WriteLine("Type 'exit' to quit, 'skills' to list, 'plugins' to list plugins.\n");

var plannerAgent = PlannerAgentFactory.Create();
var routerAgent = PlannerAgentFactory.CreateRouterAgent();
var builderAgent = BuilderAgentFactory.Create(skillsDir);
var questionAgent = BuilderAgentFactory.CreateQuestionAgent();
var routerExecutor = new RouterExecutor(routerAgent);
var codePlannerExecutor = new CodePlannerExecutor(plannerAgent);
var codeBuilderExecutor = new CodeBuilderExecutor(builderAgent);
var directAnswerExecutor = new DirectAnswerExecutor(questionAgent);

var workflow = new WorkflowBuilder(routerExecutor)
    .AddSwitch(routerExecutor, sw => sw
        .AddCase<RouterDecision>(d => d.IsCodeChange, codePlannerExecutor)
        .WithDefault(directAnswerExecutor))
    .AddEdge(codePlannerExecutor, codeBuilderExecutor)
    .Build();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    if (input.Equals("skills", StringComparison.OrdinalIgnoreCase)) { ListSkills(skillsDir); continue; }
    if (input.Equals("plugins", StringComparison.OrdinalIgnoreCase)) { ListPlugins(skillsDir); continue; }

    StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);
    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        switch (evt)
        {
            case WorkflowStartedEvent:
                Console.WriteLine("Workflow started");
                break;

            case WorkflowOutputEvent outputEvt:
                Console.WriteLine($"Workflow output: {outputEvt.Data}");
                break;

            case WorkflowErrorEvent errorEvt:
                Console.WriteLine($"Workflow error: {errorEvt.Exception?.Message}");
                break;

            case WorkflowWarningEvent warningEvt:
                Console.WriteLine($"Workflow warning: {warningEvt.Data}");
                break;

            case ExecutorInvokedEvent invokeEvt:
                Console.WriteLine($"{invokeEvt.ExecutorId} invoked");
                break;

            case ExecutorCompletedEvent executorComplete:
                Console.WriteLine($"{executorComplete.ExecutorId} complete: {executorComplete.Data}");
                break;

            case ExecutorFailedEvent failedEvt:
                Console.WriteLine($"{failedEvt.ExecutorId} failed: {failedEvt.Data}");
                break;

            case CodePlannerProgressEvent plannerProgress:
                Console.WriteLine($"[Planner] {plannerProgress.Data}");
                break;

            case CodeBuilderProgressEvent builderProgress:
                Console.WriteLine($"[Builder] {builderProgress.Data}");
                break;

            case RouterProgressEvent routerProgress:
                Console.WriteLine($"[Router] {routerProgress.Data}");
                break;
        }
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

