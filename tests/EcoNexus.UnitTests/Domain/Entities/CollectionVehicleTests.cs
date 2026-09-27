using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Entities;

/// <summary>
/// Unit tests for <see cref="CollectionVehicle"/> — the fleet vehicle
/// aggregate root.
/// </summary>
public sealed class CollectionVehicleTests
{
    [Fact]
    public void Create_ValidInputs_SetsInitialState()
    {
        var vehicle = CollectionVehicle.Create("MH-12-AB-1234", Weight.FromKilograms(5000));

        Assert.Equal("MH-12-AB-1234", vehicle.RegistrationNumber);
        Assert.Equal(5000, vehicle.Capacity.Kilograms);
        Assert.True(vehicle.IsActive);
    }

    [Fact]
    public void Create_TrimsAndUppercasesRegistration()
    {
        var vehicle = CollectionVehicle.Create("  mh-12-ab-1234  ", Weight.FromKilograms(5000));

        Assert.Equal("MH-12-AB-1234", vehicle.RegistrationNumber);
    }

    [Fact]
    public void Create_EmptyRegistration_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CollectionVehicle.Create("", Weight.FromKilograms(5000)));
    }

    [Fact]
    public void Create_WhitespaceRegistration_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CollectionVehicle.Create("   ", Weight.FromKilograms(5000)));
    }

    [Fact]
    public void Create_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CollectionVehicle.Create("MH-12-AB-1234", Weight.FromKilograms(0)));
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var vehicle = CollectionVehicle.Create("MH-12-AB-1234", Weight.FromKilograms(5000));

        vehicle.Deactivate();

        Assert.False(vehicle.IsActive);
    }

    [Fact]
    public void Reactivate_SetsActive()
    {
        var vehicle = CollectionVehicle.Create("MH-12-AB-1234", Weight.FromKilograms(5000));
        vehicle.Deactivate();

        vehicle.Reactivate();

        Assert.True(vehicle.IsActive);
    }

    [Fact]
    public void Deactivate_IsIdempotent()
    {
        var vehicle = CollectionVehicle.Create("MH-12-AB-1234", Weight.FromKilograms(5000));
        vehicle.Deactivate();

        // Calling again should not throw.
        vehicle.Deactivate();

        Assert.False(vehicle.IsActive);
    }
}

