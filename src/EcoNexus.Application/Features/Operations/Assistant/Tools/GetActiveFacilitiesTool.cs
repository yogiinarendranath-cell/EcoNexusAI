using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class GetActiveFacilitiesTool : IAssistantTool
{
    private readonly IRecyclingFacilityRepository _facilities;

    public GetActiveFacilitiesTool(IRecyclingFacilityRepository facilities)
        => _facilities = facilities;

    public ToolDescriptor Descriptor { get; } = new(
        "GetActiveFacilities",
        "Returns recycling facilities that are currently online, with their name, location, and intake count. Use when the operator asks which recycling facilities are operational or available.",
        """{ "type": "object", "properties": {} }""");

    public async Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var all = await _facilities.GetAllAsync(cancellationToken);

        var online = all
            .Where(f => f.Status == FacilityStatus.Online)
            .Select(f => new
            {
                Name = f.Name,
                Latitude = f.Location.Latitude,
                Longitude = f.Location.Longitude,
                IntakeCount = f.Intakes.Count,
                DailyCapacityKilograms = f.DailyCapacity.Kilograms,
            })
            .ToList();

        return new AssistantToolResult(
            Descriptor.Name,
            new
            {
                Count = online.Count,
                Facilities = online,
            });
    }
}
