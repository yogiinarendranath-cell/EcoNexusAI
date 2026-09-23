using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Application.Abstractions.Realtime;
using EcoNexus.Contracts.Realtime;
using EcoNexus.Domain.DomainEvents;
using MediatR;

namespace EcoNexus.Application.Features.Stations.Events;

/// <summary>
/// Reacts to <see cref="WasteStationFillLevelChangedEvent"/> by pushing a
/// real-time notification to operations clients via
/// <see cref="IOperationsNotifier"/>.
///
/// The domain event is wrapped in <see cref="DomainEventNotification{T}"/>
/// by the infrastructure layer's dispatcher; this handler subscribes to the
/// wrapped form so the Application layer stays independent of the domain
/// event bus implementation.
/// </summary>
internal sealed class WasteStationFillLevelChangedHandler
    : INotificationHandler<DomainEventNotification<WasteStationFillLevelChangedEvent>>
{
    private readonly IOperationsNotifier _notifier;

    public WasteStationFillLevelChangedHandler(IOperationsNotifier notifier)
    {
        _notifier = notifier;
    }

    public async Task Handle(
        DomainEventNotification<WasteStationFillLevelChangedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        var payload = new StationFillLevelChangedNotification(
            StationId: evt.StationId,
            StationCode: evt.StationCode,
            FillLevelPercent: evt.NewFillLevel.Percent,
            IsCritical: evt.NewFillLevel.IsCritical,
            RecordedAt: evt.RecordedAt);

        await _notifier.NotifyStationFillLevelChangedAsync(payload, cancellationToken);
    }
}
