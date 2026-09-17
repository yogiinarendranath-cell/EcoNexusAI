using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.ValueObjects;

public sealed class FillLevelTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(90)]
    [InlineData(100)]
    public void FromPercent_WithValidValue_Succeeds(double value)
    {
        var fill = FillLevel.FromPercent(value);

        Assert.Equal(value, fill.Percent);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    [InlineData(101)]
    public void FromPercent_WithOutOfRangeValue_Throws(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FillLevel.FromPercent(value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FromPercent_WithNonFiniteValue_Throws(double value)
    {
        Assert.Throws<ArgumentException>(() => FillLevel.FromPercent(value));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(89.9, false)]
    [InlineData(90, true)]
    [InlineData(95, true)]
    [InlineData(100, true)]
    public void IsCritical_ReflectsThreshold(double percent, bool expected)
    {
        var fill = FillLevel.FromPercent(percent);

        Assert.Equal(expected, fill.IsCritical);
    }

    [Fact]
    public void Empty_ReturnsZero()
    {
        Assert.Equal(0, FillLevel.Empty().Percent);
    }

    [Fact]
    public void Full_ReturnsHundred()
    {
        Assert.Equal(100, FillLevel.Full().Percent);
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        var a = FillLevel.FromPercent(42.5);
        var b = FillLevel.FromPercent(42.5);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void RoundsToOneDecimalPlace()
    {
        var fill = FillLevel.FromPercent(42.567);

        Assert.Equal(42.6, fill.Percent);
    }
}
