using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class GetFacilityOverviewTool : IAssistantTool
{
    private readonly IRecyclingFacilityRepository _facilities;

    public GetFacilityOverviewTool(IRecyclingFacilityRepository facilities)
        => _facilities = facilities;

    public ToolDescriptor Descriptor { get; } = new(
        "GetFacilityOverview",
        "Returns an overview of all recycling facilities: total count, count by status (Online/Offline/Maintenance/Decommissioned), and total intake batches. Use when the operator asks about facility status, availability, or overall facility state.",
        """{ "type": "object", "properties": {} }""");

    public async Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var all = await _facilities.GetAllAsync(cancellationToken);

        var byStatus = all
            .GroupBy(f => f.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new AssistantToolResult(
            Descriptor.Name,
            new
            {
                Total = all.Count,
                ByStatus = byStatus,
                TotalIntakeBatches = all.Sum(f => f.Intakes.Count),
            });
    }
}
