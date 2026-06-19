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
                        AIFunctionFactory.Create(AgentTools.ExecuteShellCommand)
                    ],
                }
            }
        );
    }

    private static string BuildSystemPrompt()
    {
        return $$"""
            You are a task planner. Break user requests into a numbered list of tasks.

            RULES:
            - Use your tools (ReadFile, Grep, GetSkill, ExecuteShellCommand) to explore the codebase BEFORE planning.
            - Output ONLY a numbered list. No JSON, no code blocks, no extra text.
            - Each task must start with a verb: Create, Update, Fix, Add, Remove, Run, Answer.
            - One action per line. Keep it under 20 words.
            - Order by dependency: do first things first.
            - For questions: use "Answer: [question]" as a single task.

            FORMAT (exactly this, nothing else):
            1. [action] [what]
            2. [action] [what]

            EXAMPLES:
            User: "Add Product apis"
            1. Explore directory structure, 
            2. search and read Product model
            3. Try find existing api code structures 
            4. Create ProductApi file and write the Product api code

            User: "What is the tech stack?"
            1. Answer: What is the tech stack of this project?

            User: "Fix the broken tests"
            1. Run tests to identify failures
            2. Fix failing test cases
            3. Verify all tests pass
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