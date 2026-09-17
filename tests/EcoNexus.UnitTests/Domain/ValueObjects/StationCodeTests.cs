using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.ValueObjects;

public sealed class StationCodeTests
{
    [Theory]
    [InlineData("ST-1001")]
    [InlineData("ST-001")]
    [InlineData("AB-123456")]
    public void Create_WithValidCode_Succeeds(string value)
    {
        var code = StationCode.Create(value);

        Assert.Equal(value, code.Value);
    }

    [Theory]
    [InlineData("st-1001")]
    [InlineData(" St-1001 ")]
    public void Create_NormalizesToUpperCaseAndTrims(string value)
    {
        var code = StationCode.Create(value);

        Assert.Equal("ST-1001", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ST1001")]
    [InlineData("ST-1")]
    [InlineData("S-1001")]
    [InlineData("ST-1234567")]
    [InlineData("ST-12A")]
    public void Create_WithInvalidFormat_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => StationCode.Create(value));
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        Assert.Equal(StationCode.Create("ST-1001"), StationCode.Create("st-1001"));
    }
}
