using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace DotNuxt;

internal sealed class RouterExecutor(AIAgent agent) : Executor<string, RouterDecision>("Router")
{
    private readonly AIAgent agent = agent;

    [MessageHandler]
    public override async ValueTask<RouterDecision> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await context.AddEventAsync(new RouterProgressEvent("Classifying request..."), cancellationToken);

        var prompt = $"""
            You are a classifier. Reply with exactly one word: YES or NO.
            Does this request require code changes (create, fix, update, add, remove files)?
            If it is a question or explanation request, reply NO.

            Request: {message}
            """;

        var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);
        var verdict = (response.Text ?? "").Trim().ToUpperInvariant();
        var isCodeChange = verdict.StartsWith("YES");

        await context.AddEventAsync(new RouterProgressEvent(isCodeChange ? "Code change detected" : "Question detected"), cancellationToken);

        return new RouterDecision(message, isCodeChange);
    }
}
