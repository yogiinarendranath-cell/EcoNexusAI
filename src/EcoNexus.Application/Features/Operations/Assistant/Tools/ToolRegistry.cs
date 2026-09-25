using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class ToolRegistry : IToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAssistantTool> _byName;

    public ToolRegistry(IEnumerable<IAssistantTool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        _byName = tools.ToDictionary(
            t => t.Descriptor.Name,
            StringComparer.OrdinalIgnoreCase);

        Descriptors = _byName.Values
            .Select(t => t.Descriptor)
            .ToList();
    }

    public IReadOnlyList<ToolDescriptor> Descriptors { get; }

    public IAssistantTool? Find(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return null;
        }

        return _byName.TryGetValue(toolName, out var tool) ? tool : null;
    }
}
