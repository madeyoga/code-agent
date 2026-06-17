using Microsoft.Agents.AI.Workflows;

namespace DotNuxt;

public sealed class CodePlannerProgressEvent(string step) : WorkflowEvent(step) { }