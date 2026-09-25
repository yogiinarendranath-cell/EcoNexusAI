namespace EcoNexus.Contracts.Citizen;

/// <summary>A citizen report as exposed to its own author.</summary>
public sealed record CitizenReportResponse(
    Guid Id,
    Guid StationId,
    string ReportType,
    string Description,
    string? PhotoUrl,
    string Status,
    DateTimeOffset FiledAt,
    DateTimeOffset? AcknowledgedAt,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNote);
