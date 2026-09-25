namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

/// <summary>
/// The structured result returned by an <see cref="IAssistantTool"/>.
/// Carries the tool name (for the audit trail) and an arbitrary
/// data payload that the LLM will later format into prose.
/// </summary>
public sealed record AssistantToolResult(string ToolName, object Data);
