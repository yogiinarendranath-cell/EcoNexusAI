using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.ValueObjects;

/// <summary>
/// Unit tests for <see cref="ToolCall"/> — the immutable value object
/// representing an assistant's decision to invoke a tool.
/// </summary>
public sealed class ToolCallTests
{
    [Fact]
    public void Constructor_ValidInputs_StoresFields()
    {
        var parameters = new Dictionary<string, object?> { ["limit"] = 10 };

        var call = new ToolCall("GetStations", parameters);

        Assert.Equal("GetStations", call.ToolName);
        Assert.Equal(parameters, call.Parameters);
    }

    [Fact]
    public void Constructor_EmptyToolName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ToolCall("", new Dictionary<string, object?>()));
    }

    [Fact]
    public void Constructor_WhitespaceToolName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ToolCall("   ", new Dictionary<string, object?>()));
    }

    [Fact]
    public void Constructor_NullParameters_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ToolCall("GetStations", null!));
    }

    [Fact]
    public void NoParameters_CreatesEmptyDictionary()
    {
        var call = ToolCall.NoParameters("GetStations");

        Assert.Equal("GetStations", call.ToolName);
        Assert.Empty(call.Parameters);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var parameters = new Dictionary<string, object?> { ["x"] = 1 };
        var a = new ToolCall("Tool", parameters);
        var b = new ToolCall("Tool", parameters);

        Assert.Equal(a.ToolName, b.ToolName);
        Assert.Equal(a.Parameters, b.Parameters);
    }
}

