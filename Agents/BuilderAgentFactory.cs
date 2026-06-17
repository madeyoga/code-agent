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
                        AIFunctionFactory.Create(AgentTools.GetSkill),
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
            You receive a single task. Complete it fully before responding.

            Guidelines:
            - ReadFile the relevant source files to understand existing patterns.
            - Use Grep to find related code, usages, and conventions.
            - When fixing a bug, read the file with the bug before changing it.
            - When adding a feature, read similar existing features to match the style.
            - After making changes, run build/test commands to verify.
            - You may call tools multiple times in any order.
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
                        AIFunctionFactory.Create(AgentTools.GetSkill),
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
