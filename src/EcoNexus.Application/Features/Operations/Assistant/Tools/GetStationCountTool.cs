using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class GetStationCountTool : IAssistantTool
{
    private readonly IWasteStationRepository _stations;

    public GetStationCountTool(IWasteStationRepository stations)
        => _stations = stations;

    public ToolDescriptor Descriptor { get; } = new(
        "GetStationCount",
        "Returns summary counts of waste stations: total, online, and critical. Use when the operator asks how many stations exist, how many are online, or the overall fleet status.",
        """{ "type": "object", "properties": {} }""");

    public async Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        // One wide query per bucket. Cheap because the repository uses indexes.
        var all = new StationListQuery { Page = 1, PageSize = 1 };
        var online = new StationListQuery { Status = "Online", Page = 1, PageSize = 1 };
        var critical = new StationListQuery { CriticalOnly = true, Page = 1, PageSize = 1 };

        var (_, totalAll) = await _stations.ListAsync(all, cancellationToken);
        var (_, totalOnline) = await _stations.ListAsync(online, cancellationToken);
        var (_, totalCritical) = await _stations.ListAsync(critical, cancellationToken);

        return new AssistantToolResult(
            Descriptor.Name,
            new
            {
                Total = totalAll,
                Online = totalOnline,
                Critical = totalCritical,
            });
    }
}
