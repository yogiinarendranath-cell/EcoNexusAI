using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class GetRecyclingMetricsTool : IAssistantTool
{
    private readonly IRecyclingFacilityRepository _facilities;

    public GetRecyclingMetricsTool(IRecyclingFacilityRepository facilities)
        => _facilities = facilities;

    public ToolDescriptor Descriptor { get; } = new(
        "GetRecyclingMetrics",
        "Returns aggregate recycling metrics across all facilities: total received/recovered/landfilled kilograms, recycling rate, landfill diversion, and CO2 saved. Use when the operator asks about recycling performance, recovery rates, or environmental impact.",
        """{ "type": "object", "properties": {} }""");

    public async Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var facilities = await _facilities.GetAllAsync(cancellationToken);

        // Aggregate across every facility's intake history.
        var allIntakes = facilities.SelectMany(f => f.Intakes).ToList();
        var metrics = Domain.Services.RecyclingMetricsCalculator.Compute(allIntakes);

        return new AssistantToolResult(
            Descriptor.Name,
            new
            {
                FacilityCount = facilities.Count,
                TotalBatches = metrics.TotalBatches,
                ReceivedKilograms = Math.Round(metrics.ReceivedKilograms, 1),
                RecoveredKilograms = Math.Round(metrics.RecoveredKilograms, 1),
                LandfilledKilograms = Math.Round(metrics.LandfilledKilograms, 1),
                RecyclingRatePercent = Math.Round(metrics.RecyclingRate * 100, 1),
                LandfillDiversionPercent = Math.Round(metrics.LandfillDiversion * 100, 1),
                Co2SavedKilograms = Math.Round(metrics.Co2SavedKilograms, 1),
            });
    }
}
