using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Abstractions.AI;

/// <summary>
/// Application-layer abstraction over the LLM that powers the operations
/// assistant. The LLM does two jobs:
///
///   1. Choose which tool (from a whitelist) answers a question, and
///      with what parameters (ResolveIntentAsync).
///   2. Format the tool's structured result into natural language
///      (ShapeAnswerAsync).
///
/// The LLM never sees raw database rows and cannot execute SQL. It can
/// only pick from the descriptors it is given.
/// </summary>
public interface IAssistantLlm
{
    /// <summary>
    /// Chooses the tool that should answer the question, and fills in
    /// the tool's parameters. Implementations MUST return a ToolCall
    /// with a ToolName that appears in <paramref name="availableTools"/>
    /// — never invent a tool name.
    /// </summary>
    Task<ToolCall> ResolveIntentAsync(
        string question,
        IReadOnlyList<ToolDescriptor> availableTools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats the tool's structured result into a natural-language
    /// answer for the operator. Implementations MUST NOT invent numbers
    /// that are not present in <paramref name="toolResult"/>.
    /// </summary>
    Task<string> ShapeAnswerAsync(
        string question,
        string toolName,
        object toolResult,
        CancellationToken cancellationToken = default);

    /// <summary>Identifier of the provider ("Mock", "Ollama", "AzureOpenAI").</summary>
    string ProviderName { get; }
}
