using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.ValueObjects;

/// <summary>
/// A mass value expressed in kilograms. Canonical unit is kg; all conversions
/// go through factory methods to keep arithmetic unambiguous.
/// </summary>
public sealed class Weight : ValueObject
{
    public double Kilograms { get; }

    private Weight(double kilograms)
    {
        Kilograms = Math.Round(kilograms, 3);
    }

    public static Weight FromKilograms(double kilograms)
    {
        if (double.IsNaN(kilograms) || double.IsInfinity(kilograms))
        {
            throw new ArgumentException("Weight must be a finite number.", nameof(kilograms));
        }

        if (kilograms < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kilograms), "Weight cannot be negative.");
        }

        return new Weight(kilograms);
    }

    public static Weight FromGrams(double grams) => FromKilograms(grams / 1000.0);

    public static Weight FromTonnes(double tonnes) => FromKilograms(tonnes * 1000.0);

    public static Weight Zero() => new(0);

    public Weight Add(Weight other) => new(Kilograms + other.Kilograms);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kilograms;
    }

    public override string ToString() => $"{Kilograms} kg";
}
