using DotNuxt.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DotNuxt;

public class PlannerAgentFactory
{
    public static AIAgent Create()
    {
        var chatClient = AgentClientFactory.CreateChatClient();

        return chatClient.AsAIAgent(
            new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    Instructions = BuildSystemPrompt(),
                    Tools = [
                        AIFunctionFactory.Create(AgentTools.Grep),
                        AIFunctionFactory.Create(AgentTools.ReadFile),
                        AIFunctionFactory.Create(AgentTools.ExecuteShellCommand),
                        AIFunctionFactory.Create(AgentTools.ListDirectory),
                        AIFunctionFactory.Create(AgentTools.FindFiles),
                    ],
                }
            }
        );
    }

    private static string BuildSystemPrompt()
    {
        return """
            You are a task planner. Break user requests into a numbered list of tasks.
            """;
    }

    public static AIAgent CreateRouterAgent()
    {
        var chatClient = AgentClientFactory.CreateChatClient();

        return chatClient.AsAIAgent(
            new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    Instructions = BuildRouterPrompt(),
                }
            }
        );
    }

    private static string BuildRouterPrompt()
    {
        return """
            You are a classifier. Reply with exactly one word: YES or NO.
            Does this request require code changes (create, fix, update, add, remove files)?
            If it is a question or explanation request, reply NO.
            """;
    }
}