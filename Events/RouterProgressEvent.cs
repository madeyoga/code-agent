using Microsoft.Agents.AI.Workflows;

namespace DotNuxt;

public sealed class RouterProgressEvent(string step) : WorkflowEvent(step) { }
