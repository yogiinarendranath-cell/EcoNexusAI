using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

/// <summary>
/// A single tool the operations assistant can invoke. Each tool wraps
/// an existing CQRS query (or a small new query) and returns structured
/// data — never prose.
///
/// Implementations are registered in DI and discovered by the
/// <see cref="IToolRegistry"/>. The set of registered tools IS the
/// whitelist the LLM may choose from.
/// </summary>
public interface IAssistantTool
{
    /// <summary>Metadata the LLM sees when choosing a tool.</summary>
    ToolDescriptor Descriptor { get; }

    /// <summary>
    /// Executes the tool with the given parameters. Implementations
    /// should validate parameters themselves and throw
    /// ArgumentException on malformed input.
    /// </summary>
    Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
