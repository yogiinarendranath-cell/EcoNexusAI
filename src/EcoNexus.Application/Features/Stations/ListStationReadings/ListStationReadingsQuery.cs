using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.ListStationReadings;

/// <summary>
/// Query: return the most recent readings for a station within the given window.
/// Used by the forecast chart UI.
/// </summary>
/// <param name="StationId">Station to load readings for.</param>
/// <param name="WindowHours">Lookback window in hours. Default 24. Must be 1-168.</param>
/// <param name="Limit">Maximum number of readings to return. Default 100. Must be 1-500.</param>
public sealed record ListStationReadingsQuery(
    Guid StationId,
    int WindowHours = 24,
    int Limit = 100) : IRequest<IReadOnlyList<StationReadingResponse>>;

