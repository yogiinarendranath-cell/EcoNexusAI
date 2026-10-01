using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Recycling.CreateFacility;
using EcoNexus.Contracts.Recycling;
using EcoNexus.Domain.Entities;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Recycling;

public sealed class CreateFacilityHandlerTests
{
    private readonly IRecyclingFacilityRepository _repository =
        Substitute.For<IRecyclingFacilityRepository>();

    private readonly CreateFacilityHandler _handler;

    public CreateFacilityHandlerTests()
    {
        _handler = new CreateFacilityHandler(_repository);
    }

    private static CreateFacilityRequest ValidRequest(
        string name = "Alpha Recycling",
        double lat = 12.9716,
        double lng = 77.5946,
        double capacity = 10_000)
        => new(name, lat, lng, capacity);

    private static CreateFacilityCommand Command(CreateFacilityRequest req) => new(req);

    [Fact]
    public async Task Handle_ValidRequest_CreatesFacilityAndReturnsResponse()
    {
        _repository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _handler.Handle(
            Command(ValidRequest(name: "Alpha Recycling")),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Alpha Recycling", response.Name);

        await _repository
            .Received(1)
            .AddAsync(
                Arg.Is<RecyclingFacility>(f => f.Name == "Alpha Recycling"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NameWithWhitespace_TrimmedBeforeExistenceCheckAndPersist()
    {
        _repository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _handler.Handle(
            Command(ValidRequest(name: "  Alpha Recycling  ")),
            CancellationToken.None);

        await _repository
            .Received(1)
            .NameExistsAsync("Alpha Recycling", Arg.Any<CancellationToken>());

        Assert.Equal("Alpha Recycling", response.Name);

        await _repository
            .Received(1)
            .AddAsync(
                Arg.Is<RecyclingFacility>(f => f.Name == "Alpha Recycling"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NameAlreadyExists_ThrowsInvalidOperationException()
    {
        _repository
            .NameExistsAsync("Alpha Recycling", Arg.Any<CancellationToken>())
            .Returns(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                Command(ValidRequest(name: "Alpha Recycling")),
                CancellationToken.None));

        Assert.Contains("Alpha Recycling", ex.Message);

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<RecyclingFacility>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LatitudeOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        _repository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.Handle(Command(ValidRequest(lat: 91.0)), CancellationToken.None));

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<RecyclingFacility>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LongitudeOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        _repository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.Handle(Command(ValidRequest(lng: 181.0)), CancellationToken.None));

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<RecyclingFacility>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        _repository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.Handle(Command(ValidRequest(capacity: -1)), CancellationToken.None));

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<RecyclingFacility>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ZeroCapacity_ThrowsArgumentException()
    {
        _repository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(Command(ValidRequest(capacity: 0)), CancellationToken.None));

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<RecyclingFacility>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhitespaceOnlyName_ThrowsArgumentException()
    {
        _repository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(Command(ValidRequest(name: "   ")), CancellationToken.None));

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<RecyclingFacility>(), Arg.Any<CancellationToken>());
    }
}