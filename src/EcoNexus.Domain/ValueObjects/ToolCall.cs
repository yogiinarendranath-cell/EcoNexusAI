namespace EcoNexus.Domain.ValueObjects;

/// <summary>
/// The assistant's decision to invoke a specific tool with specific
/// parameters. Produced by the LLM (or the mock), then validated
/// against the tool's schema before execution.
///
/// Immutable value object.
/// </summary>
public sealed record ToolCall
{
    public string ToolName { get; }
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    public ToolCall(
        string toolName,
        IReadOnlyDictionary<string, object?> parameters)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException(
                "Tool name must not be empty.",
                nameof(toolName));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ToolName = toolName;
        Parameters = parameters;
    }

    /// <summary>
    /// Convenience factory for a tool call with no parameters.
    /// </summary>
    public static ToolCall NoParameters(string toolName)
        => new(toolName, new Dictionary<string, object?>());
}
