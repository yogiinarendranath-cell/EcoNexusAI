using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.CollectionVehicles.CreateVehicle;
using EcoNexus.Contracts.CollectionVehicles;
using EcoNexus.Domain.Entities;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.CollectionVehicles;

public sealed class CreateVehicleHandlerTests
{
    private readonly ICollectionVehicleRepository _repository =
        Substitute.For<ICollectionVehicleRepository>();

    private readonly CreateVehicleHandler _handler;

    public CreateVehicleHandlerTests()
    {
        _handler = new CreateVehicleHandler(_repository);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesVehicleAndReturnsResponse()
    {
        _repository
            .RegistrationNumberExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateCollectionVehicleRequest(
            RegistrationNumber: "MH-12-AB-1234",
            CapacityKilograms: 5000);

        var response = await _handler.Handle(
            new CreateVehicleCommand(request),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("MH-12-AB-1234", response.RegistrationNumber);
        Assert.Equal(5000, response.CapacityKilograms);
        Assert.True(response.IsActive);

        await _repository
            .Received(1)
            .AddAsync(
                Arg.Is<CollectionVehicle>(v => v.RegistrationNumber == "MH-12-AB-1234"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationWithWhitespaceAndMixedCase_NormalisesBeforeExistenceCheckAndPersist()
    {
        _repository
            .RegistrationNumberExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateCollectionVehicleRequest(
            RegistrationNumber: "  mh-12-ab-1234  ",
            CapacityKilograms: 5000);

        var response = await _handler.Handle(
            new CreateVehicleCommand(request),
            CancellationToken.None);

        await _repository
            .Received(1)
            .RegistrationNumberExistsAsync(
                "MH-12-AB-1234",
                Arg.Any<CancellationToken>());

        await _repository
            .Received(1)
            .AddAsync(
                Arg.Is<CollectionVehicle>(v => v.RegistrationNumber == "MH-12-AB-1234"),
                Arg.Any<CancellationToken>());

        Assert.Equal("MH-12-AB-1234", response.RegistrationNumber);
    }

    [Fact]
    public async Task Handle_DuplicateRegistrationNumber_ThrowsConflictException()
    {
        _repository
            .RegistrationNumberExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new CreateCollectionVehicleRequest(
            RegistrationNumber: "MH-12-AB-1234",
            CapacityKilograms: 5000);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new CreateVehicleCommand(request), CancellationToken.None));

        Assert.Contains("MH-12-AB-1234", ex.Message);

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<CollectionVehicle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ZeroCapacity_ThrowsArgumentException()
    {
        _repository
            .RegistrationNumberExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateCollectionVehicleRequest(
            RegistrationNumber: "MH-12-AB-1234",
            CapacityKilograms: 0);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(new CreateVehicleCommand(request), CancellationToken.None));

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<CollectionVehicle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeCapacity_ThrowsArgumentException()
    {
        _repository
            .RegistrationNumberExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateCollectionVehicleRequest(
            RegistrationNumber: "MH-12-AB-1234",
            CapacityKilograms: -100);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(new CreateVehicleCommand(request), CancellationToken.None));

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<CollectionVehicle>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhitespaceRegistrationNumber_ThrowsArgumentException()
    {
        _repository
            .RegistrationNumberExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateCollectionVehicleRequest(
            RegistrationNumber: "   ",
            CapacityKilograms: 5000);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(new CreateVehicleCommand(request), CancellationToken.None));

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<CollectionVehicle>(), Arg.Any<CancellationToken>());
    }
}