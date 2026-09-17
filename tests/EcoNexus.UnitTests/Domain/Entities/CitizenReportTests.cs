using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Entities;

public sealed class CitizenReportTests
{
    private static CitizenReport FileReport(
        string description = "Bin overflowing",
        string? photoUrl = null)
    {
        return CitizenReport.File(
            Guid.NewGuid(),
            Guid.NewGuid(),
            description,
            photoUrl,
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public void File_WithValidInput_ReturnsReportInNewStatus()
    {
        var report = FileReport();

        Assert.Equal(ReportStatus.New, report.Status);
        Assert.Equal("Bin overflowing", report.Description);
        Assert.Null(report.PhotoUrl);
        Assert.Null(report.AcknowledgedAt);
        Assert.Null(report.ResolvedAt);
        Assert.Null(report.ResolutionNote);
    }

    [Fact]
    public void File_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CitizenReport.File(Guid.Empty, Guid.NewGuid(), "desc", null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void File_WithEmptyStationId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CitizenReport.File(Guid.NewGuid(), Guid.Empty, "desc", null, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void File_WithEmptyDescription_Throws(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            CitizenReport.File(Guid.NewGuid(), Guid.NewGuid(), description, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void File_WithTooLongDescription_Throws()
    {
        var huge = new string('x', 2001);

        Assert.Throws<ArgumentException>(() =>
            CitizenReport.File(Guid.NewGuid(), Guid.NewGuid(), huge, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void File_TrimsDescriptionAndRaisesEvent()
    {
        var report = FileReport(description: "   Overflow   ");

        Assert.Equal("Overflow", report.Description);
        Assert.Single(report.DomainEvents);
        Assert.IsType<CitizenReportFiledEvent>(report.DomainEvents.First());
    }

    [Fact]
    public void File_WithWhitespacePhotoUrl_StoresNull()
    {
        var report = FileReport(photoUrl: "   ");

        Assert.Null(report.PhotoUrl);
    }

    [Fact]
    public void File_WithPhotoUrl_StoresTrimmed()
    {
        var report = FileReport(photoUrl: "  https://example.com/p.jpg  ");

        Assert.Equal("https://example.com/p.jpg", report.PhotoUrl);
    }

    [Fact]
    public void Acknowledge_FromNew_TransitionsToAcknowledged()
    {
        var report = FileReport();
        var at = DateTimeOffset.UtcNow;

        report.Acknowledge(at);

        Assert.Equal(ReportStatus.Acknowledged, report.Status);
        Assert.Equal(at, report.AcknowledgedAt);
    }

    [Fact]
    public void Acknowledge_WhenNotNew_Throws()
    {
        var report = FileReport();
        report.Acknowledge(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            report.Acknowledge(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Resolve_FromNew_TransitionsToResolvedWithNote()
    {
        var report = FileReport();
        var at = DateTimeOffset.UtcNow;

        report.Resolve("Bin was emptied", at);

        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.Equal("Bin was emptied", report.ResolutionNote);
        Assert.Equal(at, report.ResolvedAt);
    }

    [Fact]
    public void Resolve_FromAcknowledged_TransitionsToResolved()
    {
        var report = FileReport();
        report.Acknowledge(DateTimeOffset.UtcNow);

        report.Resolve("Handled", DateTimeOffset.UtcNow);

        Assert.Equal(ReportStatus.Resolved, report.Status);
    }

    [Fact]
    public void Resolve_WithEmptyNote_Throws()
    {
        var report = FileReport();

        Assert.Throws<ArgumentException>(() =>
            report.Resolve("   ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Resolve_WhenAlreadyResolved_Throws()
    {
        var report = FileReport();
        report.Resolve("Handled", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            report.Resolve("Again", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reject_FromNew_TransitionsToRejectedWithReason()
    {
        var report = FileReport();
        var at = DateTimeOffset.UtcNow;

        report.Reject("Not a valid report", at);

        Assert.Equal(ReportStatus.Rejected, report.Status);
        Assert.Equal("Not a valid report", report.ResolutionNote);
        Assert.Equal(at, report.ResolvedAt);
    }

    [Fact]
    public void Reject_FromAcknowledged_TransitionsToRejected()
    {
        var report = FileReport();
        report.Acknowledge(DateTimeOffset.UtcNow);

        report.Reject("Duplicate", DateTimeOffset.UtcNow);

        Assert.Equal(ReportStatus.Rejected, report.Status);
    }

    [Fact]
    public void Reject_WithEmptyReason_Throws()
    {
        var report = FileReport();

        Assert.Throws<ArgumentException>(() =>
            report.Reject("", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reject_WhenAlreadyResolved_Throws()
    {
        var report = FileReport();
        report.Resolve("Handled", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            report.Reject("Nope", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_Throws()
    {
        var report = FileReport();
        report.Reject("Spam", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            report.Reject("Spam again", DateTimeOffset.UtcNow));
    }
}
