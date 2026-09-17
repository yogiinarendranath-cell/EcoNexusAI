using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A collection vehicle (truck/van) in the fleet.
/// </summary>
public sealed class CollectionVehicle : AggregateRoot
{
    public string RegistrationNumber { get; private set; }
    public Weight Capacity { get; private set; }
    public bool IsActive { get; private set; }

    // Required by EF Core
    private CollectionVehicle()
    {
        RegistrationNumber = null!;
        Capacity = null!;
    }

    private CollectionVehicle(string registrationNumber, Weight capacity)
    {
        RegistrationNumber = registrationNumber;
        Capacity = capacity;
        IsActive = true;
    }

    public static CollectionVehicle Create(string registrationNumber, Weight capacity)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            throw new ArgumentException("Registration number must not be empty.", nameof(registrationNumber));
        }

        if (capacity.Kilograms <= 0)
        {
            throw new ArgumentException("Vehicle capacity must be greater than zero.", nameof(capacity));
        }

        return new CollectionVehicle(registrationNumber.Trim().ToUpperInvariant(), capacity);
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
