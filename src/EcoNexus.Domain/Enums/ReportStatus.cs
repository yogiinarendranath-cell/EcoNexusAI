namespace EcoNexus.Domain.Enums;

/// <summary>
/// The resolution state of a citizen report.
/// </summary>
public enum ReportStatus
{
    New = 0,
    Acknowledged = 1,
    Resolved = 2,
    Rejected = 3
}
