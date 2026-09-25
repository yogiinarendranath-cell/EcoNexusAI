using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

/// <summary>
/// Aggregates every registered <see cref="IAssistantTool"/>.
/// The handler uses this to (a) hand descriptors to the LLM and
/// (b) look up the tool the LLM chose.
/// </summary>
public interface IToolRegistry
{
    /// <summary>Descriptors of every registered tool, safe to hand to the LLM.</summary>
    IReadOnlyList<ToolDescriptor> Descriptors { get; }

    /// <summary>
    /// Looks up a tool by name. Returns null if the name is not in the
    /// whitelist — the handler MUST treat null as a rejection.
    /// </summary>
    IAssistantTool? Find(string toolName);
}
