using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Stations.ListStations;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class ListStationsHandlerTests
{
    private readonly IWasteStationRepository _repository = Substitute.For<IWasteStationRepository>();
    private readonly ListStationsHandler _handler;

    public ListStationsHandlerTests()
    {
        _handler = new ListStationsHandler(_repository);
    }

    private static WasteStation MakeStation(string code, double fillPercent = 0, WasteCategory category = WasteCategory.Plastic)
    {
        var station = WasteStation.Create(
            StationCode.Create(code),
            Location.Create(12.97, 77.59),
            Weight.FromKilograms(500),
            category);

        if (fillPercent > 0)
        {
            station.RecordReading(
                FillLevel.FromPercent(fillPercent),
                25.0,
                80,
                DateTimeOffset.UtcNow);
        }

        return station;
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPage()
    {
        _repository
            .ListAsync(Arg.Any<StationListQuery>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<WasteStation>(), 0));

        var result = await _handler.Handle(
            new ListStationsQuery(new StationListQuery()),
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task Handle_ReturnsProjectedItems()
    {
        var stations = new[]
        {
            MakeStation("ST-2001", 45, WasteCategory.Plastic),
            MakeStation("ST-2002", 92, WasteCategory.Paper)
        };

        _repository
            .ListAsync(Arg.Any<StationListQuery>(), Arg.Any<CancellationToken>())
            .Returns((stations, 2));

        var result = await _handler.Handle(
            new ListStationsQuery(new StationListQuery()),
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);

        var first = result.Items[0];
        Assert.Equal("ST-2001", first.Code);
        Assert.Equal("Plastic", first.PrimaryCategory);
        Assert.Equal(45, first.CurrentFillPercent);
        Assert.False(first.IsCritical);

        var second = result.Items[1];
        Assert.Equal("ST-2002", second.Code);
        Assert.Equal("Paper", second.PrimaryCategory);
        Assert.Equal(92, second.CurrentFillPercent);
        Assert.True(second.IsCritical);
    }

    [Fact]
    public async Task Handle_PassesQueryParametersToRepository()
    {
        _repository
            .ListAsync(Arg.Any<StationListQuery>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<WasteStation>(), 0));

        var query = new StationListQuery(
            Page: 3,
            PageSize: 10,
            Status: "Online",
            Category: "Plastic",
            CriticalOnly: true,
            SortBy: "fillLevel",
            SortDesc: true);

        await _handler.Handle(new ListStationsQuery(query), CancellationToken.None);

        await _repository.Received(1).ListAsync(
            Arg.Is<StationListQuery>(q =>
                q.Page == 3 &&
                q.PageSize == 10 &&
                q.Status == "Online" &&
                q.Category == "Plastic" &&
                q.CriticalOnly &&
                q.SortBy == "fillLevel" &&
                q.SortDesc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PopulatesPagingMetadata()
    {
        _repository
            .ListAsync(Arg.Any<StationListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new[] { MakeStation("ST-3001") }, 57));

        var result = await _handler.Handle(
            new ListStationsQuery(new StationListQuery(Page: 2, PageSize: 20)),
            CancellationToken.None);

        Assert.Equal(2, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(57, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }
}
