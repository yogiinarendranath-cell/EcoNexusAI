using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.ValueObjects;

/// <summary>
/// Unit tests for <see cref="ToolDescriptor"/> — the immutable description
/// of a tool offered to the assistant LLM.
/// </summary>
public sealed class ToolDescriptorTests
{
    private const string ValidSchema = @"{""type"":""object""}";

    [Fact]
    public void Constructor_ValidInputs_StoresFields()
    {
        var descriptor = new ToolDescriptor(
            "GetStations",
            "Returns active stations",
            ValidSchema);

        Assert.Equal("GetStations", descriptor.Name);
        Assert.Equal("Returns active stations", descriptor.Description);
        Assert.Equal(ValidSchema, descriptor.ParametersJsonSchema);
    }

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ToolDescriptor("", "desc", ValidSchema));
    }

    [Fact]
    public void Constructor_NameOver100Chars_Throws()
    {
        var tooLong = new string('N', 101);
        Assert.Throws<ArgumentException>(() =>
            new ToolDescriptor(tooLong, "desc", ValidSchema));
    }

    [Fact]
    public void Constructor_EmptyDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ToolDescriptor("Tool", "", ValidSchema));
    }

    [Fact]
    public void Constructor_DescriptionOver500Chars_Throws()
    {
        var tooLong = new string('D', 501);
        Assert.Throws<ArgumentException>(() =>
            new ToolDescriptor("Tool", tooLong, ValidSchema));
    }

    [Fact]
    public void Constructor_EmptySchema_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ToolDescriptor("Tool", "desc", ""));
    }
}

