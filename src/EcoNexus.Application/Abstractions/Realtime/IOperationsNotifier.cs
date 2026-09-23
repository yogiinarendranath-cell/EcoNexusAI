using EcoNexus.Contracts.Realtime;

namespace EcoNexus.Application.Abstractions.Realtime;

/// <summary>
/// Abstraction over the real-time push mechanism used by the operations
/// dashboard. The Application layer depends on this interface; the
/// Infrastructure layer provides a SignalR-backed implementation.
///
/// Keeping this abstraction here (rather than referencing SignalR directly)
/// preserves Clean Architecture: Application does not depend on transport.
/// </summary>
public interface IOperationsNotifier
{
    /// <summary>
    /// Pushes a fill-level-changed notification to all subscribed operations
    /// clients.
    /// </summary>
    Task NotifyStationFillLevelChangedAsync(
        StationFillLevelChangedNotification notification,
        CancellationToken cancellationToken = default);
}
