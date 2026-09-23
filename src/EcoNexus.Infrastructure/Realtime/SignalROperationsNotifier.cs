using EcoNexus.Application.Abstractions.Realtime;
using EcoNexus.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace EcoNexus.Infrastructure.Realtime;

/// <summary>
/// SignalR-backed implementation of <see cref="IOperationsNotifier"/>.
/// Pushes notifications to all connected clients of <see cref="OperationsHub"/>
/// using the group-less broadcast pattern (all clients receive all events).
///
/// Broadcasting to all clients is intentional for v1: the ops dashboard is a
/// single shared view. If we later need role-scoped broadcasts, we'll switch
/// to SignalR groups here — the Application layer won't change.
/// </summary>
internal sealed class SignalROperationsNotifier : IOperationsNotifier
{
    /// <summary>
    /// Client-side method name. The React dashboard subscribes to this exact
    /// name. Kept as a constant so client and server stay in sync.
    /// </summary>
    public const string FillLevelChangedClientMethod = "StationFillLevelChanged";

    private readonly IHubContext<OperationsHub> _hubContext;

    public SignalROperationsNotifier(IHubContext<OperationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyStationFillLevelChangedAsync(
        StationFillLevelChangedNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return _hubContext.Clients.All.SendAsync(
            FillLevelChangedClientMethod,
            notification,
            cancellationToken);
    }
}
