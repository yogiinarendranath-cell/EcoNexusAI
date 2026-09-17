using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a citizen files a report about a station.
/// </summary>
public sealed record CitizenReportFiledEvent(
    Guid ReportId,
    Guid FiledByUserId,
    Guid StationId,
    DateTimeOffset FiledAt) : IDomainEvent;
