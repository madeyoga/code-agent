using Microsoft.Agents.AI.Workflows;

namespace DotNuxt;

public sealed class CodeBuilderProgressEvent(string step) : WorkflowEvent(step) { }
