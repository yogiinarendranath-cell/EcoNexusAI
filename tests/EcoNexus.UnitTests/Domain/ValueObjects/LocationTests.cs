using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.ValueObjects;

public sealed class LocationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(12.9716, 77.5946)]   // Bangalore
    public void Create_WithValidCoordinates_Succeeds(double lat, double lng)
    {
        var location = Location.Create(lat, lng);

        Assert.Equal(lat, location.Latitude);
        Assert.Equal(lng, location.Longitude);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void Create_WithOutOfRangeCoordinates_Throws(double lat, double lng)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Location.Create(lat, lng));
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(0, double.NegativeInfinity)]
    public void Create_WithNonFiniteCoordinates_Throws(double lat, double lng)
    {
        Assert.Throws<ArgumentException>(() => Location.Create(lat, lng));
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        var a = Location.Create(12.97, 77.59);
        var b = Location.Create(12.97, 77.59);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentCoordinates_NotEqual()
    {
        var a = Location.Create(12.97, 77.59);
        var b = Location.Create(12.98, 77.59);

        Assert.NotEqual(a, b);
    }
}
