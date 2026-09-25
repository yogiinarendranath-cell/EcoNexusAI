using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class GetWasteByCategoryTool : IAssistantTool
{
    private readonly IWasteStationRepository _stations;

    public GetWasteByCategoryTool(IWasteStationRepository stations)
        => _stations = stations;

    public ToolDescriptor Descriptor { get; } = new(
        "GetWasteByCategory",
        "Returns a breakdown of how many stations exist per waste category (Plastic, Organic, Paper, Glass, Metal, EWaste, General, Hazardous). Use when the operator asks about waste types, category distribution, or the mix of waste handled.",
        """{ "type": "object", "properties": {} }""");

    public async Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        // Page through the full station list. Cheap: repository uses indexes and returns codes only.
        var query = new StationListQuery { Page = 1, PageSize = 100 };
        var (items, _) = await _stations.ListAsync(query, cancellationToken);

        var byCategory = items
            .GroupBy(s => s.PrimaryCategory.ToString())
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new AssistantToolResult(
            Descriptor.Name,
            new
            {
                TotalStations = items.Count,
                ByCategory = byCategory,
            });
    }
}
