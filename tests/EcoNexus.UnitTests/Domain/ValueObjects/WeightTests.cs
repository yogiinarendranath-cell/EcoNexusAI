using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.ValueObjects;

public sealed class WeightTests
{
    [Fact]
    public void FromKilograms_WithValidValue_Succeeds()
    {
        var w = Weight.FromKilograms(12.5);

        Assert.Equal(12.5, w.Kilograms);
    }

    [Fact]
    public void FromKilograms_WithNegativeValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Weight.FromKilograms(-1));
    }

    [Fact]
    public void FromGrams_ConvertsCorrectly()
    {
        var w = Weight.FromGrams(1500);

        Assert.Equal(1.5, w.Kilograms);
    }

    [Fact]
    public void FromTonnes_ConvertsCorrectly()
    {
        var w = Weight.FromTonnes(2);

        Assert.Equal(2000, w.Kilograms);
    }

    [Fact]
    public void Zero_ReturnsZeroKilograms()
    {
        Assert.Equal(0, Weight.Zero().Kilograms);
    }

    [Fact]
    public void Add_SumsWeights()
    {
        var a = Weight.FromKilograms(10);
        var b = Weight.FromKilograms(5.5);

        var result = a.Add(b);

        Assert.Equal(15.5, result.Kilograms);
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        Assert.Equal(Weight.FromKilograms(7.5), Weight.FromKilograms(7.5));
    }
}
