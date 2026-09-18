using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.DomainEvents;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace EcoNexus.Api.Hubs;

/// <summary>
/// Reacts to <see cref="WasteStationFillLevelChangedEvent"/> by pushing a
/// compact <see cref="StationUpdateDto"/> to every connected operations
/// dashboard client over SignalR.
///
/// The domain layer has no knowledge of SignalR; this class sits in the API
/// project and adapts domain events to the real-time transport.
/// </summary>
internal sealed class StationUpdateNotifier
    : INotificationHandler<DomainEventNotification<WasteStationFillLevelChangedEvent>>
{
    private readonly IHubContext<OperationsHub> _hub;

    public StationUpdateNotifier(IHubContext<OperationsHub> hub)
    {
        _hub = hub;
    }

    public Task Handle(
        DomainEventNotification<WasteStationFillLevelChangedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var dto = new StationUpdateDto(
            e.StationId,
            e.StationCode,
            e.NewFillLevel.Percent,
            e.NewFillLevel.IsCritical,
            e.RecordedAt);

        return _hub.Clients.All.SendAsync(
            "StationUpdated",
            dto,
            cancellationToken);
    }
}
