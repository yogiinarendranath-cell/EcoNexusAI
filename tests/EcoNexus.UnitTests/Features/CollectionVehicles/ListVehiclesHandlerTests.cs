using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.CollectionVehicles.ListVehicles;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.CollectionVehicles;

public sealed class ListVehiclesHandlerTests
{
    private readonly ICollectionVehicleRepository _repository =
        Substitute.For<ICollectionVehicleRepository>();

    private readonly ListVehiclesHandler _handler;

    public ListVehiclesHandlerTests()
    {
        _handler = new ListVehiclesHandler(_repository);
    }

    private static CollectionVehicle NewVehicle(string registration, double capacityKg)
        => CollectionVehicle.Create(
            registration,
            Weight.FromKilograms(capacityKg));

    [Fact]
    public async Task Handle_MultipleVehicles_ReturnsAllMappedResponses()
    {
        var v1 = NewVehicle("MH-12-AB-1200", 3500);
        var v2 = NewVehicle("MH-12-AB-1234", 5000);

        _repository
            .ListAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { v1, v2 });

        var response = await _handler.Handle(
            new ListVehiclesQuery(),
            CancellationToken.None);

        Assert.Equal(2, response.Count);

        Assert.Equal(v1.Id,                   response[0].Id);
        Assert.Equal("MH-12-AB-1200",         response[0].RegistrationNumber);
        Assert.Equal(3500,                    response[0].CapacityKilograms);
        Assert.True(response[0].IsActive);

        Assert.Equal(v2.Id,                   response[1].Id);
        Assert.Equal("MH-12-AB-1234",         response[1].RegistrationNumber);
        Assert.Equal(5000,                    response[1].CapacityKilograms);
        Assert.True(response[1].IsActive);
    }

    [Fact]
    public async Task Handle_NoVehicles_ReturnsEmptyList()
    {
        _repository
            .ListAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CollectionVehicle>());

        var response = await _handler.Handle(
            new ListVehiclesQuery(),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task Handle_CallsRepositoryExactlyOnce()
    {
        _repository
            .ListAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CollectionVehicle>());

        await _handler.Handle(new ListVehiclesQuery(), CancellationToken.None);

        await _repository
            .Received(1)
            .ListAsync(Arg.Any<CancellationToken>());
    }
}