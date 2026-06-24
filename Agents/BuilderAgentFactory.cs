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
                        AIFunctionFactory.Create(AgentTools.CreateFileOrOverwrite),
                        AIFunctionFactory.Create(AgentTools.ListDirectory),
                        AIFunctionFactory.Create(AgentTools.FindFiles),
                        AIFunctionFactory.Create(AgentTools.DotNetBuild),
                        AIFunctionFactory.Create(AgentTools.DotNetTest)
                    ],
                },
                AIContextProviders = [skillsProvider]
            }
        );
    }

    private static string BuildSystemPrompt()
    {
        return """
            You are an expert .NET developer. Follow these rules strictly:

            WORKFLOW RULES:
            1. ALWAYS read a file before modifying it. never guess at existing code
            2. Use ListDirectory or FindFiles to discover project structure before writing files

            FILE RULES:
            6. Never delete files without creating a backup first (copy to .bak)
            7. When creating new files, follow existing project conventions (naming, namespaces, formatting)
            8. Show your plan before executing multi-step changes

            ERROR HANDLING:
            9. If you encounter an error, analyze it carefully and retry with the fix
            10. Never ignore compiler errors or warnings that indicate real problems

            OUTPUT FORMAT:
            11. When completing a task, summarize what files were created/modified
            12. Report build/test results clearly (SUCCEEDED/FAILED)
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
