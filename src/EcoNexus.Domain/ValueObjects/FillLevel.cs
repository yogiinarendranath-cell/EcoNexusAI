using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.ValueObjects;

/// <summary>
/// The fill level of a waste station, expressed as a percentage in [0, 100].
/// Values are stored to one decimal place to avoid floating-point noise.
/// Implements <see cref="IComparable{T}"/> so EF Core can translate
/// OrderBy / comparisons on the value-converted column.
/// </summary>
public sealed class FillLevel : ValueObject, IComparable<FillLevel>, IComparable
{
    /// <summary>Threshold above which a station is considered critical.</summary>
    public const double CriticalThresholdPercent = 90.0;

    public double Percent { get; }

    /// <summary>True if this fill level is at or above the critical threshold.</summary>
    public bool IsCritical => Percent >= CriticalThresholdPercent;

    private FillLevel(double percent)
    {
        Percent = Math.Round(percent, 1);
    }

    public static FillLevel FromPercent(double percent)
    {
        if (double.IsNaN(percent) || double.IsInfinity(percent))
        {
            throw new ArgumentException("Fill level must be a finite number.", nameof(percent));
        }

        if (percent < 0 || percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Fill level must be between 0 and 100.");
        }

        return new FillLevel(percent);
    }

    public static FillLevel Empty() => new(0);

    public static FillLevel Full() => new(100);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Percent;
    }

    public int CompareTo(FillLevel? other)
        => other is null ? 1 : Percent.CompareTo(other.Percent);

    int IComparable.CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is not FillLevel other)
        {
            throw new ArgumentException($"Object must be of type {nameof(FillLevel)}.", nameof(obj));
        }
        return CompareTo(other);
    }

    public static bool operator <(FillLevel left, FillLevel right)
        => left is not null && right is not null && left.Percent < right.Percent;

    public static bool operator >(FillLevel left, FillLevel right)
        => left is not null && right is not null && left.Percent > right.Percent;

    public static bool operator <=(FillLevel left, FillLevel right)
        => left is not null && right is not null && left.Percent <= right.Percent;

    public static bool operator >=(FillLevel left, FillLevel right)
        => left is not null && right is not null && left.Percent >= right.Percent;

    public override bool Equals(object? obj)
        => obj is FillLevel other && Percent.Equals(other.Percent);

    public override int GetHashCode()
        => Percent.GetHashCode();

    public static bool operator ==(FillLevel left, FillLevel right)
        => Equals(left, right);

    public static bool operator !=(FillLevel left, FillLevel right)
        => !Equals(left, right);

    public override string ToString() => $"{Percent}%";
}

