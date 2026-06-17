using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace DotNuxt;

internal sealed class CodeBuilderExecutor(AIAgent agent) : Executor<TodoList, string>("CodeBuilder")
{
    private readonly AIAgent agent = agent;

    [MessageHandler]
    public override async ValueTask<string> HandleAsync(TodoList todo, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string lastAnswer = string.Empty;
        AgentSession session = await agent.CreateSessionAsync(cancellationToken);
        foreach (var item in todo.Items)
        {
            var task = item.Content;
            await context.AddEventAsync(new CodeBuilderProgressEvent($"Processing task {item.EntryNumber}: {task}"), cancellationToken);

            // Console.WriteLine(JsonSerializer.Serialize(task));
            var response = await agent.RunAsync(task, session, cancellationToken: cancellationToken);
            // Console.WriteLine(JsonSerializer.Serialize(response));
            lastAnswer = response.Text;
            await context.AddEventAsync(new CodeBuilderProgressEvent(response.Text), cancellationToken);

            item.IsDone = true;

            await context.AddEventAsync(new CodeBuilderProgressEvent($"Completed task {item.EntryNumber}\n\n"), cancellationToken);
        }

        return lastAnswer;

        // var serialized = await agent.SerializeSessionAsync(session);
        // var serializedString = JsonSerializer.Serialize(serialized);
    }
}