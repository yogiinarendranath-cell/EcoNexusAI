using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.ValueObjects;

/// <summary>
/// Geographic coordinates for a station or vehicle.
/// Latitude is constrained to [-90, 90]; longitude to [-180, 180].
/// </summary>
public sealed class Location : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    private Location(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Location Create(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude))
        {
            throw new ArgumentException("Latitude must be a finite number.", nameof(latitude));
        }

        if (double.IsNaN(longitude) || double.IsInfinity(longitude))
        {
            throw new ArgumentException("Longitude must be a finite number.", nameof(longitude));
        }

        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        return new Location(latitude, longitude);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

    public override string ToString() => $"({Latitude}, {Longitude})";
}
