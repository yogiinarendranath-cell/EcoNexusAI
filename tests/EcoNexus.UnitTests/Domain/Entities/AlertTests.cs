using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Entities;

public sealed class AlertTests
{
    [Fact]
    public void Raise_WithValidInput_ReturnsAlertInNewStatus()
    {
        var alert = Alert.Raise(
            Guid.NewGuid(),
            AlertSeverity.Warning,
            "Station fill exceeded 90%",
            DateTimeOffset.UtcNow);

        Assert.Equal(ReportStatus.New, alert.Status);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.Equal("Station fill exceeded 90%", alert.Message);
        Assert.Null(alert.AcknowledgedAt);
        Assert.Null(alert.ResolvedAt);
    }

    [Fact]
    public void Raise_WithNullStationId_Succeeds()
    {
        var alert = Alert.Raise(null, AlertSeverity.Info, "System notice", DateTimeOffset.UtcNow);

        Assert.Null(alert.StationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Raise_WithEmptyMessage_Throws(string message)
    {
        Assert.Throws<ArgumentException>(() =>
            Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, message, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Raise_WithTooLongMessage_Throws()
    {
        var huge = new string('x', 1001);

        Assert.Throws<ArgumentException>(() =>
            Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, huge, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Raise_TrimsMessageAndRaisesEvent()
    {
        var alert = Alert.Raise(
            Guid.NewGuid(),
            AlertSeverity.Critical,
            "   Fill critical   ",
            DateTimeOffset.UtcNow);

        Assert.Equal("Fill critical", alert.Message);
        Assert.Single(alert.DomainEvents);
        Assert.IsType<AlertRaisedEvent>(alert.DomainEvents.First());
    }

    [Fact]
    public void Acknowledge_FromNew_TransitionsToAcknowledged()
    {
        var alert = Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, "msg", DateTimeOffset.UtcNow);
        var at = DateTimeOffset.UtcNow;

        alert.Acknowledge("operator-1", at);

        Assert.Equal(ReportStatus.Acknowledged, alert.Status);
        Assert.Equal("operator-1", alert.AcknowledgedBy);
        Assert.Equal(at, alert.AcknowledgedAt);
    }

    [Fact]
    public void Acknowledge_WhenNotNew_Throws()
    {
        var alert = Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, "msg", DateTimeOffset.UtcNow);
        alert.Acknowledge("op-1", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            alert.Acknowledge("op-2", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Acknowledge_WithEmptyOperator_Throws()
    {
        var alert = Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, "msg", DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            alert.Acknowledge("   ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Resolve_FromNew_TransitionsToResolved()
    {
        var alert = Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, "msg", DateTimeOffset.UtcNow);
        var at = DateTimeOffset.UtcNow;

        alert.Resolve("op-1", at);

        Assert.Equal(ReportStatus.Resolved, alert.Status);
        Assert.Equal("op-1", alert.ResolvedBy);
        Assert.Equal(at, alert.ResolvedAt);
    }

    [Fact]
    public void Resolve_FromAcknowledged_TransitionsToResolved()
    {
        var alert = Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, "msg", DateTimeOffset.UtcNow);
        alert.Acknowledge("op-1", DateTimeOffset.UtcNow);

        alert.Resolve("op-1", DateTimeOffset.UtcNow);

        Assert.Equal(ReportStatus.Resolved, alert.Status);
    }

    [Fact]
    public void Resolve_WhenAlreadyResolved_Throws()
    {
        var alert = Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, "msg", DateTimeOffset.UtcNow);
        alert.Resolve("op-1", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            alert.Resolve("op-2", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Resolve_WithEmptyOperator_Throws()
    {
        var alert = Alert.Raise(Guid.NewGuid(), AlertSeverity.Warning, "msg", DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            alert.Resolve("", DateTimeOffset.UtcNow));
    }
}
