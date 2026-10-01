using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Citizen.ListMyReports;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class ListMyReportsHandlerTests
{
    private readonly ICitizenReportRepository _repository =
        Substitute.For<ICitizenReportRepository>();

    private readonly ListMyReportsHandler _handler;

    public ListMyReportsHandlerTests()
    {
        _handler = new ListMyReportsHandler(_repository);
    }

    private static CitizenReport NewReport(
        Guid userId,
        CitizenReportType type = CitizenReportType.OverflowingBin,
        string? photoUrl = null)
        => CitizenReport.File(
            filedByUserId: userId,
            stationId: Guid.NewGuid(),
            reportType: type,
            description: "Bin is overflowing near the park entrance.",
            photoUrl: photoUrl,
            filedAt: DateTimeOffset.UtcNow);

    [Fact]
    public async Task Handle_MultipleReports_ReturnsMappedResponses()
    {
        var userId = Guid.NewGuid();

        var newReport = NewReport(userId, CitizenReportType.OverflowingBin);

        var resolvedReport = NewReport(
            userId,
            CitizenReportType.IllegalDumping,
            photoUrl: "https://example.com/dump.jpg");
        var ackedAt    = DateTimeOffset.UtcNow.AddMinutes(-30);
        var resolvedAt = DateTimeOffset.UtcNow;
        resolvedReport.Acknowledge(ackedAt);
        resolvedReport.Resolve("Collection dispatched.", resolvedAt);

        _repository
            .ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { resolvedReport, newReport });

        var response = await _handler.Handle(
            new ListMyReportsQuery(userId),
            CancellationToken.None);

        Assert.Equal(2, response.Count);

        Assert.Equal(resolvedReport.Id,                 response[0].Id);
        Assert.Equal(resolvedReport.StationId,          response[0].StationId);
        Assert.Equal("IllegalDumping",                  response[0].ReportType);
        Assert.Equal(resolvedReport.Description,        response[0].Description);
        Assert.Equal("https://example.com/dump.jpg",    response[0].PhotoUrl);
        Assert.Equal("Resolved",                        response[0].Status);
        Assert.Equal(resolvedReport.FiledAt,            response[0].FiledAt);
        Assert.Equal(ackedAt,                           response[0].AcknowledgedAt);
        Assert.Equal(resolvedAt,                        response[0].ResolvedAt);
        Assert.Equal("Collection dispatched.",          response[0].ResolutionNote);

        Assert.Equal(newReport.Id,                      response[1].Id);
        Assert.Equal("OverflowingBin",                  response[1].ReportType);
        Assert.Null(response[1].PhotoUrl);
        Assert.Equal("New",                             response[1].Status);
        Assert.Null(response[1].AcknowledgedAt);
        Assert.Null(response[1].ResolvedAt);
        Assert.Null(response[1].ResolutionNote);
    }

    [Fact]
    public async Task Handle_ReportTypeIsNull_ResponseFallsBackToOther()
    {
        var userId = Guid.NewGuid();
        var report = NewReport(userId);

        typeof(CitizenReport)
            .GetProperty(nameof(CitizenReport.ReportType))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(report, new object?[] { null });

        _repository
            .ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { report });

        var response = await _handler.Handle(
            new ListMyReportsQuery(userId),
            CancellationToken.None);

        Assert.Single(response);
        Assert.Equal("Other", response[0].ReportType);
    }

    [Fact]
    public async Task Handle_NoReports_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();

        _repository
            .ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CitizenReport>());

        var response = await _handler.Handle(
            new ListMyReportsQuery(userId),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task Handle_QueriesRepositoryWithExactUserId()
    {
        var userId = Guid.NewGuid();

        _repository
            .ListByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CitizenReport>());

        await _handler.Handle(
            new ListMyReportsQuery(userId),
            CancellationToken.None);

        await _repository
            .Received(1)
            .ListByUserAsync(userId, Arg.Any<CancellationToken>());
    }
}