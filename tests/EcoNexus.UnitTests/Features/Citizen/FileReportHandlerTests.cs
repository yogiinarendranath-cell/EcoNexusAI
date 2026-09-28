using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Citizen.FileReport;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class FileReportHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly ICitizenReportRepository _reports = Substitute.For<ICitizenReportRepository>();
    private readonly IWasteStationRepository _stations = Substitute.For<IWasteStationRepository>();
    private readonly FileReportHandler _handler;

    public FileReportHandlerTests()
    {
        _handler = new FileReportHandler(_reports, _stations);
    }

    // -------------------- Fixtures --------------------

    private static WasteStation NewStation(string code = "ST-3001")
        => WasteStation.Create(
            StationCode.Create(code),
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(500),
            WasteCategory.Plastic);

    private void StationExists(Guid stationId)
    {
        var station = NewStation();
        _stations.GetByIdAsync(stationId, Arg.Any<CancellationToken>())
                 .Returns(station);
    }

    private static FileReportRequest ValidRequest(Guid stationId,
        CitizenReportType type = CitizenReportType.OverflowingBin,
        string description = "Bin is overflowing onto the sidewalk",
        string? photoUrl = "https://example.com/photo.jpg")
        => new(stationId, type.ToString(), description, photoUrl);

    // -------------------- Station guard --------------------

    [Fact]
    public async Task Handle_StationNotFound_ThrowsNotFoundException()
    {
        var missingStationId = Guid.NewGuid();
        _stations.GetByIdAsync(missingStationId, Arg.Any<CancellationToken>())
                 .Returns((WasteStation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new FileReportCommand(UserId, ValidRequest(missingStationId)),
                CancellationToken.None));

        await _reports.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // -------------------- Happy path --------------------

    [Fact]
    public async Task Handle_ValidRequest_CreatesReportAndReturnsResponse()
    {
        var stationId = Guid.NewGuid();
        StationExists(stationId);

        var response = await _handler.Handle(
            new FileReportCommand(UserId, ValidRequest(stationId)),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(stationId, response.StationId);
        Assert.Equal("OverflowingBin", response.ReportType);
        Assert.Equal("New", response.Status);
        Assert.NotEqual(default, response.FiledAt);

        await _reports.Received(1).AddAsync(
            Arg.Is<CitizenReport>(r => r.StationId == stationId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsReport()
    {
        var stationId = Guid.NewGuid();
        StationExists(stationId);

        await _handler.Handle(
            new FileReportCommand(UserId, ValidRequest(stationId)),
            CancellationToken.None);

        await _reports.Received(1).AddAsync(
            Arg.Any<CitizenReport>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------- Report type parsing --------------------

    [Fact]
    public async Task Handle_CaseInsensitiveReportType_ParsesCorrectly()
    {
        var stationId = Guid.NewGuid();
        StationExists(stationId);

        // Lowercase input should still parse (ignoreCase: true in the handler).
        var request = new FileReportRequest(
            stationId,
            "illegaldumping",  // lowercase
            "Bags of trash dumped on the corner",
            null);

        var response = await _handler.Handle(
            new FileReportCommand(UserId, request),
            CancellationToken.None);

        Assert.Equal("IllegalDumping", response.ReportType);
    }

    [Fact]
    public async Task Handle_UnknownReportType_ThrowsArgumentException()
    {
        var stationId = Guid.NewGuid();
        StationExists(stationId);

        var request = new FileReportRequest(
            stationId,
            "NotARealReportType",
            "Some description",
            null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(new FileReportCommand(UserId, request), CancellationToken.None));

        await _reports.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // -------------------- Optional fields --------------------

    [Fact]
    public async Task Handle_NullPhotoUrl_IsAccepted()
    {
        var stationId = Guid.NewGuid();
        StationExists(stationId);

        var request = new FileReportRequest(
            stationId,
            "Other",
            "Something odd at this station",
            null);

        var response = await _handler.Handle(
            new FileReportCommand(UserId, request),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Other", response.ReportType);
        await _reports.Received(1).AddAsync(
            Arg.Any<CitizenReport>(),
            Arg.Any<CancellationToken>());
    }
}