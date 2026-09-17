using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a new operational alert is created.
/// </summary>
public sealed record AlertRaisedEvent(
    Guid AlertId,
    Guid? StationId,
    AlertSeverity Severity,
    string Message,
    DateTimeOffset RaisedAt) : IDomainEvent;
