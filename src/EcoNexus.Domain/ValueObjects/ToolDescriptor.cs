namespace EcoNexus.Domain.ValueObjects;

/// <summary>
/// Immutable description of a tool that the operations assistant can
/// invoke. Descriptors are supplied to the LLM so it can choose the
/// appropriate tool for a given question. They are the whitelist:
/// if a tool is not in the descriptor set, the LLM cannot call it.
/// </summary>
public sealed record ToolDescriptor
{
    public string Name { get; }
    public string Description { get; }
    public string ParametersJsonSchema { get; }

    public ToolDescriptor(
        string name,
        string description,
        string parametersJsonSchema)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tool name must not be empty.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Tool name must be 100 characters or fewer.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Tool description must not be empty.",
                nameof(description));
        }

        if (description.Length > 500)
        {
            throw new ArgumentException(
                "Tool description must be 500 characters or fewer.",
                nameof(description));
        }

        if (string.IsNullOrWhiteSpace(parametersJsonSchema))
        {
            throw new ArgumentException(
                "Parameters JSON schema must not be empty.",
                nameof(parametersJsonSchema));
        }

        Name = name;
        Description = description;
        ParametersJsonSchema = parametersJsonSchema;
    }
}
