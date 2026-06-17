using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace DotNuxt;

internal sealed class DirectAnswerExecutor(AIAgent agent) : Executor<RouterDecision, string>("DirectAnswer")
{
    private readonly AIAgent agent = agent;

    [MessageHandler]
    public override async ValueTask<string> HandleAsync(RouterDecision decision, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine(decision.Message);

        await context.AddEventAsync(new CodeBuilderProgressEvent("Answering question directly..."), cancellationToken);

        var response = await agent.RunAsync(decision.Message, cancellationToken: cancellationToken);

        await context.AddEventAsync(new CodeBuilderProgressEvent(response.Text), cancellationToken);

        return response.Text ?? "(No answer generated)";
    }
}
