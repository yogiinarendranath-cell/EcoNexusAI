namespace EcoNexus.Contracts.Citizen;

/// <summary>Request body for filing a citizen report.</summary>
public sealed record FileReportRequest(
    Guid StationId,
    string ReportType,
    string Description,
    string? PhotoUrl);
