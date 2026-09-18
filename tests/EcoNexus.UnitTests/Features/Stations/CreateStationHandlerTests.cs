using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Stations.CreateStation;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class CreateStationHandlerTests
{
    private readonly IWasteStationRepository _repository = Substitute.For<IWasteStationRepository>();
    private readonly CreateStationHandler _handler;

    public CreateStationHandlerTests()
    {
        _handler = new CreateStationHandler(_repository);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesStationAndReturnsResponse()
    {
        // Arrange
        _repository
            .CodeExistsAsync(Arg.Any<StationCode>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateStationRequest(
            Code: "ST-1001",
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

        // Act
        var response = await _handler.Handle(
            new CreateStationCommand(request),
            CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("ST-1001", response.Code);

        await _repository
            .Received(1)
            .AddAsync(Arg.Is<WasteStation>(s => s.Code.Value == "ST-1001"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        _repository
            .CodeExistsAsync(Arg.Any<StationCode>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new CreateStationRequest(
            Code: "ST-1001",
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new CreateStationCommand(request), CancellationToken.None));

        Assert.Contains("ST-1001", ex.Message);

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<WasteStation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidCategory_ThrowsArgumentException()
    {
        // Arrange
        _repository
            .CodeExistsAsync(Arg.Any<StationCode>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateStationRequest(
            Code: "ST-1002",
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "NotARealCategory");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _handler.Handle(new CreateStationCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidStationCodeFormat_ThrowsArgumentException()
    {
        // Arrange
        _repository
            .CodeExistsAsync(Arg.Any<StationCode>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateStationRequest(
            Code: "invalid-code",
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _handler.Handle(new CreateStationCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OutOfRangeLatitude_ThrowsArgumentException()
    {
        // Arrange
        _repository
            .CodeExistsAsync(Arg.Any<StationCode>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateStationRequest(
            Code: "ST-1003",
            Latitude: 999,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            _handler.Handle(new CreateStationCommand(request), CancellationToken.None));
    }
}
