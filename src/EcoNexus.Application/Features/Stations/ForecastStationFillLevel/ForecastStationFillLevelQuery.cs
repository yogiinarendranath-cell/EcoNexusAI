using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.Stations.ForecastStationFillLevel;

/// <summary>
/// Query: forecast when the given station will overflow,
/// based on readings observed within the last <paramref name="WindowHours"/> hours.
/// </summary>
/// <param name="StationId">Station to forecast.</param>
/// <param name="WindowHours">Lookback window in hours. Default 24. Must be 1-168.</param>
public sealed record ForecastStationFillLevelQuery(
    Guid StationId,
    int WindowHours = 24) : IRequest<ForecastResult>;

