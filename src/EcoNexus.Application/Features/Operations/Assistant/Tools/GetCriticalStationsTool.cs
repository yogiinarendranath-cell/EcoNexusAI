using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class GetCriticalStationsTool : IAssistantTool
{
    private readonly IWasteStationRepository _stations;

    public GetCriticalStationsTool(IWasteStationRepository stations)
        => _stations = stations;

    public ToolDescriptor Descriptor { get; } = new(
        "GetCriticalStations",
        "Returns waste stations that are at or above 90% fill level, ordered by fill percentage (highest first). Use when the operator asks about stations needing collection, critical bins, overflowing stations, or which stations to prioritize.",
        """
        {
          "type": "object",
          "properties": {
            "take": { "type": "integer", "minimum": 1, "maximum": 100, "default": 20 }
          }
        }
        """);

    public async Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var take = ToolParameterHelpers.GetInt(parameters, "take", defaultValue: 20);

        var query = new StationListQuery
        {
            CriticalOnly = true,
            SortBy = "filllevel",
            SortDesc = true,
            Page = 1,
            PageSize = take,
        };

        var (items, total) = await _stations.ListAsync(query, cancellationToken);

        return new AssistantToolResult(
            Descriptor.Name,
            new
            {
                Count = items.Count,
                Total = total,
                Stations = items.Select(s => new
                {
                    Code = s.Code.Value,
                    FillPercent = s.CurrentFill.Percent,
                    Category = s.PrimaryCategory.ToString(),
                    Status = s.Status.ToString(),
                    Latitude = s.Location.Latitude,
                    Longitude = s.Location.Longitude,
                }),
            });
    }
}
