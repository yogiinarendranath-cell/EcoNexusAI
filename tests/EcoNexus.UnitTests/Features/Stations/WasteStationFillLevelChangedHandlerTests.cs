using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Application.Abstractions.Realtime;
using EcoNexus.Application.Features.Stations.Events;
using EcoNexus.Contracts.Realtime;
using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class WasteStationFillLevelChangedHandlerTests
{
    private readonly IOperationsNotifier _notifier = Substitute.For<IOperationsNotifier>();
    private readonly WasteStationFillLevelChangedHandler _handler;

    public WasteStationFillLevelChangedHandlerTests()
    {
        _handler = new WasteStationFillLevelChangedHandler(_notifier);
    }

    private static DomainEventNotification<WasteStationFillLevelChangedEvent> Notification(
        double fillPercent,
        string stationCode = "ST-0001",
        DateTimeOffset? recordedAt = null,
        Guid? stationId = null)
    {
        var evt = new WasteStationFillLevelChangedEvent(
            stationId ?? Guid.NewGuid(),
            stationCode,
            FillLevel.FromPercent(fillPercent),
            recordedAt ?? DateTimeOffset.UtcNow);

        return new DomainEventNotification<WasteStationFillLevelChangedEvent>(evt);
    }

    [Fact]
    public async Task Handle_NonCriticalFill_NotifiesWithMappedPayload()
    {
        var stationId = Guid.NewGuid();
        var recordedAt = DateTimeOffset.UtcNow;
        var notification = Notification(62.5, "ST-0001", recordedAt, stationId);

        await _handler.Handle(notification, CancellationToken.None);

        await _notifier.Received(1).NotifyStationFillLevelChangedAsync(
            Arg.Is<StationFillLevelChangedNotification>(n =>
                n.StationId == stationId &&
                n.StationCode == "ST-0001" &&
                n.FillLevelPercent == 62.5 &&
                n.IsCritical == false &&
                n.RecordedAt == recordedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CriticalFill_MarksPayloadAsCritical()
    {
        var notification = Notification(95.0);

        await _handler.Handle(notification, CancellationToken.None);

        await _notifier.Received(1).NotifyStationFillLevelChangedAsync(
            Arg.Is<StationFillLevelChangedNotification>(n =>
                n.FillLevelPercent == 95.0 && n.IsCritical),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AtCriticalThreshold_MarksPayloadAsCritical()
    {
        var notification = Notification(90.0);

        await _handler.Handle(notification, CancellationToken.None);

        await _notifier.Received(1).NotifyStationFillLevelChangedAsync(
            Arg.Is<StationFillLevelChangedNotification>(n => n.IsCritical),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var notification = Notification(42.0);

        await _handler.Handle(notification, cts.Token);

        await _notifier.Received(1).NotifyStationFillLevelChangedAsync(
            Arg.Any<StationFillLevelChangedNotification>(),
            cts.Token);
    }
}