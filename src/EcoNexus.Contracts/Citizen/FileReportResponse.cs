namespace EcoNexus.Contracts.Citizen;

/// <summary>Response returned after a report is filed.</summary>
public sealed record FileReportResponse(
    Guid Id,
    Guid StationId,
    string ReportType,
    string Status,
    DateTimeOffset FiledAt);
