using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace DotNuxt;

public class TodoList
{
    public List<TodoItem> Items { get; set; } = [];
}

public class TodoItem
{
    public int EntryNumber { get; set; }
    public required string Content { get; set; }
    public bool IsDone { get; set; } = false;
}

internal sealed class CodePlannerExecutor(AIAgent agent) : Executor<RouterDecision, TodoList>("CodePlanner")
{
    private readonly AIAgent agent = agent;

    [MessageHandler]
    public override async ValueTask<TodoList> HandleAsync(RouterDecision decision, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await context.AddEventAsync(new CodePlannerProgressEvent("Analyzing request"), cancellationToken);

        var message = decision.Message;
        var response = await agent.RunAsync(message, cancellationToken: cancellationToken);
        var text = response.Text ?? "";
        await context.AddEventAsync(new CodePlannerProgressEvent(text), cancellationToken);

        var todo = ParseTodoList(text);

        if (todo.Items.Count == 0)
        {
            await context.AddEventAsync(new CodePlannerProgressEvent("Planner returned no tasks, wrapping input as single task"), cancellationToken);
            todo.Items.Add(new TodoItem { EntryNumber = 1, Content = message });
        }

        await context.AddEventAsync(new CodePlannerProgressEvent($"Created {todo.Items.Count} tasks"), cancellationToken);

        return todo;
    }

    private static TodoList ParseTodoList(string text)
    {
        var todo = new TodoList();
        if (string.IsNullOrWhiteSpace(text)) return todo;

        // Try JSON first (in case the model does return JSON)
        try
        {
            var jsonTodo = System.Text.Json.JsonSerializer.Deserialize<TodoList>(text, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (jsonTodo?.Items.Count > 0) return jsonTodo;
        }
        catch { }

        // Parse numbered list: "1. Do something" or "1) Do something"
        var matches = Regex.Matches(text, @"^\s*(\d+)[.)]\s+(.+)$", RegexOptions.Multiline);
        foreach (Match m in matches)
        {
            if (int.TryParse(m.Groups[1].Value, out int num))
            {
                todo.Items.Add(new TodoItem { EntryNumber = num, Content = m.Groups[2].Value.Trim() });
            }
        }

        return todo;
    }
}