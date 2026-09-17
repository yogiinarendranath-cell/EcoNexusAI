using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// An operational alert. Aggregate root with a simple lifecycle:
/// New -> Acknowledged -> Resolved.
/// </summary>
public sealed class Alert : AggregateRoot
{
    public Guid? StationId { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public string Message { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTimeOffset RaisedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public string? ResolvedBy { get; private set; }

    // Required by EF Core
    private Alert()
    {
        Message = null!;
    }

    private Alert(Guid? stationId, AlertSeverity severity, string message, DateTimeOffset raisedAt)
    {
        StationId = stationId;
        Severity = severity;
        Message = message;
        Status = ReportStatus.New;
        RaisedAt = raisedAt;
    }

    public static Alert Raise(
        Guid? stationId,
        AlertSeverity severity,
        string message,
        DateTimeOffset raisedAt)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Alert message must not be empty.", nameof(message));
        }

        if (message.Length > 1000)
        {
            throw new ArgumentException("Alert message must be 1000 characters or fewer.", nameof(message));
        }

        var alert = new Alert(stationId, severity, message.Trim(), raisedAt);

        alert.RaiseDomainEvent(new AlertRaisedEvent(
            alert.Id,
            alert.StationId,
            alert.Severity,
            alert.Message,
            alert.RaisedAt));

        return alert;
    }

    /// <summary>
    /// Transitions from New to Acknowledged.
    /// </summary>
    public void Acknowledge(string acknowledgedBy, DateTimeOffset at)
    {
        if (Status != ReportStatus.New)
        {
            throw new InvalidOperationException($"Cannot acknowledge an alert in status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(acknowledgedBy))
        {
            throw new ArgumentException("AcknowledgedBy must not be empty.", nameof(acknowledgedBy));
        }

        Status = ReportStatus.Acknowledged;
        AcknowledgedBy = acknowledgedBy;
        AcknowledgedAt = at;
    }

    /// <summary>
    /// Transitions from New or Acknowledged to Resolved.
    /// </summary>
    public void Resolve(string resolvedBy, DateTimeOffset at)
    {
        if (Status == ReportStatus.Resolved)
        {
            throw new InvalidOperationException("Alert is already resolved.");
        }

        if (string.IsNullOrWhiteSpace(resolvedBy))
        {
            throw new ArgumentException("ResolvedBy must not be empty.", nameof(resolvedBy));
        }

        Status = ReportStatus.Resolved;
        ResolvedBy = resolvedBy;
        ResolvedAt = at;
    }
}
