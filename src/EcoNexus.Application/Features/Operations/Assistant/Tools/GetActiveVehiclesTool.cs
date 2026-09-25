using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class GetActiveVehiclesTool : IAssistantTool
{
    private readonly ICollectionVehicleRepository _vehicles;

    public GetActiveVehiclesTool(ICollectionVehicleRepository vehicles)
        => _vehicles = vehicles;

    public ToolDescriptor Descriptor { get; } = new(
        "GetActiveVehicles",
        "Returns the collection vehicles that are currently active, with registration, capacity, and status. Use when the operator asks about the fleet, active vehicles, or available capacity.",
        """{ "type": "object", "properties": {} }""");

    public async Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var all = await _vehicles.ListAsync(cancellationToken);

        var active = all
            .Where(v => v.IsActive)
            .Select(v => new
            {
                Registration = v.RegistrationNumber,
                CapacityKilograms = v.Capacity.Kilograms,
            })
            .ToList();

        return new AssistantToolResult(
            Descriptor.Name,
            new
            {
                ActiveCount = active.Count,
                TotalCount = all.Count,
                Vehicles = active,
            });
    }
}
