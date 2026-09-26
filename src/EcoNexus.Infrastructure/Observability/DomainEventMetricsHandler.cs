using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Domain.DomainEvents;
using MediatR;

namespace EcoNexus.Infrastructure.Observability;

/// <summary>
/// MediatR notification handlers that observe domain events and increment
/// business metrics. This is a pure side-channel: it never modifies state,
/// never throws, and never blocks the caller.
///
/// One handler class per event type keeps each method tiny and makes it
/// obvious which event drives which counter. All handlers share the same
/// meter via EcoNexusMeters.
/// </summary>
internal sealed class WasteStationFillLevelChangedMetricsHandler
    : INotificationHandler<DomainEventNotification<WasteStationFillLevelChangedEvent>>
{
    private readonly EcoNexusMeters _meters;

    public WasteStationFillLevelChangedMetricsHandler(EcoNexusMeters meters)
    {
        _meters = meters;
    }

    public Task Handle(
        DomainEventNotification<WasteStationFillLevelChangedEvent> notification,
        CancellationToken cancellationToken)
    {
        _meters.StationReadingsRecorded.Add(1);
        return Task.CompletedTask;
    }
}

internal sealed class WasteStationReachedCriticalFillMetricsHandler
    : INotificationHandler<DomainEventNotification<WasteStationReachedCriticalFillEvent>>
{
    private readonly EcoNexusMeters _meters;

    public WasteStationReachedCriticalFillMetricsHandler(EcoNexusMeters meters)
    {
        _meters = meters;
    }

    public Task Handle(
        DomainEventNotification<WasteStationReachedCriticalFillEvent> notification,
        CancellationToken cancellationToken)
    {
        _meters.StationCriticalFillReached.Add(1);
        return Task.CompletedTask;
    }
}

internal sealed class WasteStationCollectedMetricsHandler
    : INotificationHandler<DomainEventNotification<WasteStationCollectedEvent>>
{
    private readonly EcoNexusMeters _meters;

    public WasteStationCollectedMetricsHandler(EcoNexusMeters meters)
    {
        _meters = meters;
    }

    public Task Handle(
        DomainEventNotification<WasteStationCollectedEvent> notification,
        CancellationToken cancellationToken)
    {
        _meters.StationCollected.Add(1);
        return Task.CompletedTask;
    }
}

internal sealed class CitizenReportFiledMetricsHandler
    : INotificationHandler<DomainEventNotification<CitizenReportFiledEvent>>
{
    private readonly EcoNexusMeters _meters;

    public CitizenReportFiledMetricsHandler(EcoNexusMeters meters)
    {
        _meters = meters;
    }

    public Task Handle(
        DomainEventNotification<CitizenReportFiledEvent> notification,
        CancellationToken cancellationToken)
    {
        _meters.CitizenReportsFiled.Add(1);
        return Task.CompletedTask;
    }
}

internal sealed class RecyclingIntakeRecordedMetricsHandler
    : INotificationHandler<DomainEventNotification<RecyclingIntakeRecordedEvent>>
{
    private readonly EcoNexusMeters _meters;

    public RecyclingIntakeRecordedMetricsHandler(EcoNexusMeters meters)
    {
        _meters = meters;
    }

    public Task Handle(
        DomainEventNotification<RecyclingIntakeRecordedEvent> notification,
        CancellationToken cancellationToken)
    {
        _meters.RecyclingIntakesRecorded.Add(1);
        return Task.CompletedTask;
    }
}

internal sealed class RecyclingIntakeAdvancedMetricsHandler
    : INotificationHandler<DomainEventNotification<RecyclingIntakeAdvancedEvent>>
{
    private readonly EcoNexusMeters _meters;

    public RecyclingIntakeAdvancedMetricsHandler(EcoNexusMeters meters)
    {
        _meters = meters;
    }

    public Task Handle(
        DomainEventNotification<RecyclingIntakeAdvancedEvent> notification,
        CancellationToken cancellationToken)
    {
        _meters.RecyclingIntakesAdvanced.Add(1);
        return Task.CompletedTask;
    }
}

internal sealed class GreenPointsEarnedMetricsHandler
    : INotificationHandler<DomainEventNotification<GreenPointsEarnedEvent>>
{
    private readonly EcoNexusMeters _meters;

    public GreenPointsEarnedMetricsHandler(EcoNexusMeters meters)
    {
        _meters = meters;
    }

    public Task Handle(
        DomainEventNotification<GreenPointsEarnedEvent> notification,
        CancellationToken cancellationToken)
    {
        var points = Convert.ToInt64(
            notification.DomainEvent.GetType()
                .GetProperty("Points")?
                .GetValue(notification.DomainEvent)
            ?? 0L);

        _meters.GreenPointsEarned.Add(points);
        return Task.CompletedTask;
    }
}
