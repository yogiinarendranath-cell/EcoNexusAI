using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A report filed by a citizen about a waste station.
/// Aggregate root with lifecycle: New -> Acknowledged -> Resolved, or
/// New -> Rejected (spam, invalid).
/// </summary>
public sealed class CitizenReport : AggregateRoot
{
    public Guid FiledByUserId { get; private set; }
    public Guid StationId { get; private set; }
    public CitizenReportType? ReportType { get; private set; }
    public string Description { get; private set; }
    public string? PhotoUrl { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTimeOffset FiledAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionNote { get; private set; }

    // Required by EF Core
    private CitizenReport()
    {
        Description = null!;
    }

    private CitizenReport(
        Guid filedByUserId,
        Guid stationId,
        CitizenReportType reportType,
        string description,
        string? photoUrl,
        DateTimeOffset filedAt)
    {
        FiledByUserId = filedByUserId;
        StationId = stationId;
        ReportType = reportType;
        Description = description;
        PhotoUrl = photoUrl;
        Status = ReportStatus.New;
        FiledAt = filedAt;
    }

    public static CitizenReport File(
        Guid filedByUserId,
        Guid stationId,
        CitizenReportType reportType,
        string description,
        string? photoUrl,
        DateTimeOffset filedAt)
    {
        if (filedByUserId == Guid.Empty)
        {
            throw new ArgumentException("FiledByUserId must not be empty.", nameof(filedByUserId));
        }

        if (stationId == Guid.Empty)
        {
            throw new ArgumentException("StationId must not be empty.", nameof(stationId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description must not be empty.", nameof(description));
        }

        if (description.Length > 2000)
        {
            throw new ArgumentException("Description must be 2000 characters or fewer.", nameof(description));
        }

        var report = new CitizenReport(
            filedByUserId,
            stationId,
            reportType,
            description.Trim(),
            string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim(),
            filedAt);

        report.RaiseDomainEvent(new CitizenReportFiledEvent(
            report.Id,
            report.FiledByUserId,
            report.StationId,
            report.FiledAt));

        return report;
    }

    /// <summary>
    /// Transitions from New to Acknowledged.
    /// </summary>
    public void Acknowledge(DateTimeOffset at)
    {
        if (Status != ReportStatus.New)
        {
            throw new InvalidOperationException($"Cannot acknowledge a report in status {Status}.");
        }

        Status = ReportStatus.Acknowledged;
        AcknowledgedAt = at;
    }

    /// <summary>
    /// Transitions from New or Acknowledged to Resolved.
    /// </summary>
    public void Resolve(string resolutionNote, DateTimeOffset at)
    {
        if (Status == ReportStatus.Resolved || Status == ReportStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot resolve a report in status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(resolutionNote))
        {
            throw new ArgumentException("ResolutionNote must not be empty.", nameof(resolutionNote));
        }

        Status = ReportStatus.Resolved;
        ResolutionNote = resolutionNote.Trim();
        ResolvedAt = at;
    }

    /// <summary>
    /// Transitions from New or Acknowledged to Rejected.
    /// </summary>
    public void Reject(string reason, DateTimeOffset at)
    {
        if (Status == ReportStatus.Resolved || Status == ReportStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot reject a report in status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason must not be empty.", nameof(reason));
        }

        Status = ReportStatus.Rejected;
        ResolutionNote = reason.Trim();
        ResolvedAt = at;
    }
}
