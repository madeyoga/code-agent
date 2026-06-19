using DotNuxt.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DotNuxt;

public class BuilderAgentFactory
{
    public static AIAgent Create(string skillsDir)
    {
        var chatClient = AgentClientFactory.CreateChatClient();

#pragma warning disable MAAI001
        var skillsProvider = new AgentSkillsProvider(skillsDir);
#pragma warning restore MAAI001

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
                        AIFunctionFactory.Create(AgentTools.CreateFileOrOverwrite)
                    ],
                },
                AIContextProviders = [skillsProvider]
            }
        );
    }

    private static string BuildSystemPrompt()
    {
        return """
            You are an expert software engineer specializing in .NET, Nuxt, and Python.
            You receive a task. Complete it fully before responding.
            """;
    }

    public static AIAgent CreateQuestionAgent()
    {
        var chatClient = AgentClientFactory.CreateChatClient();

        return chatClient.AsAIAgent(
            new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    Instructions = BuildQuestionPrompt(),
                    Tools = [
                        AIFunctionFactory.Create(AgentTools.Grep),
                        AIFunctionFactory.Create(AgentTools.ReadFile),
                        AIFunctionFactory.Create(AgentTools.ExecuteShellCommand)
                    ],
                }
            }
        );
    }

    private static string BuildQuestionPrompt()
    {
        return """
            You are a helpful assistant that answers questions about this codebase.
            Use your tools to explore the code and give accurate, concise answers.
            Do not guess. Do not write code or create files. Only answer the question.
            """;
    }
}
