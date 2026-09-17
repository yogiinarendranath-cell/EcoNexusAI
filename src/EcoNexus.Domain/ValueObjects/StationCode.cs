using System.Text.RegularExpressions;
using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.ValueObjects;

/// <summary>
/// A human-readable identifier for a waste station, e.g. "ST-1001".
/// Format: two letters, hyphen, three to six digits. Immutable once created.
/// </summary>
public sealed partial class StationCode : ValueObject
{
    [GeneratedRegex(@"^[A-Z]{2}-\d{3,6}$", RegexOptions.Compiled)]
    private static partial Regex Pattern();

    public string Value { get; }

    private StationCode(string value)
    {
        Value = value;
    }

    public static StationCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Station code must not be empty.", nameof(value));
        }

        var trimmed = value.Trim().ToUpperInvariant();

        if (!Pattern().IsMatch(trimmed))
        {
            throw new ArgumentException(
                "Station code must match format 'XX-999' where XX are two letters and 999 are 3-6 digits.",
                nameof(value));
        }

        return new StationCode(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
