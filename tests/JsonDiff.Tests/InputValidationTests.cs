using System;
using System.Text.Json;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Tests input validation for JsonDifferExtensions string-based entry points.
/// Ensures proper exception types are thrown for invalid inputs and that
/// duplicate key behavior is properly documented.
/// </summary>
public class InputValidationTests
{
    #region Diff(string, string) validation

    [Fact]
    public void Diff_StringString_NullLeft_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ((string)null!).Diff("{\"a\":1}"));
        Assert.Equal("left", ex.ParamName);
    }

    [Fact]
    public void Diff_StringString_NullRight_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => "{\"a\":1}".Diff(null!));
        Assert.Equal("right", ex.ParamName);
    }

    [Fact]
    public void Diff_StringString_EmptyLeft_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => string.Empty.Diff("{\"a\":1}"));
        Assert.Equal("left", ex.ParamName);
    }

    [Fact]
    public void Diff_StringString_WhitespaceLeft_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => "   \n\t  ".Diff("{\"a\":1}"));
        Assert.Equal("left", ex.ParamName);
    }

    [Fact]
    public void Diff_StringString_EmptyRight_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => "{\"a\":1}".Diff(string.Empty));
        Assert.Equal("right", ex.ParamName);
    }

    [Fact]
    public void Diff_StringString_WhitespaceRight_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => "{\"a\":1}".Diff("   \n\t  "));
        Assert.Equal("right", ex.ParamName);
    }

    [Fact]
    public void Diff_StringString_InvalidJsonLeft_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => "not json".Diff("{\"a\":1}"));
    }

    [Fact]
    public void Diff_StringString_InvalidJsonRight_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => "{\"a\":1}".Diff("not json"));
    }

    [Fact]
    public void Diff_StringString_UnterminatedObject_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => "{\"a\":1".Diff("{\"a\":2}"));
    }

    [Fact]
    public void Diff_StringString_DuplicateKeys_LastWins()
    {
        // Verify that duplicate key behavior is documented and consistent
        // This should not throw, but should use last-wins policy
        var changes = "{\"a\":1,\"a\":2}".Diff("{\"a\":2}");
        Assert.Empty(changes);
    }

    [Fact]
    public void Diff_StringString_DuplicateKeysWithDifferentValues_ReportsChange()
    {
        // When last values differ, should report a single change
        var changes = "{\"a\":1,\"a\":2}".Diff("{\"a\":2,\"a\":3}");
        var change = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, change.Kind);
        Assert.Equal("/a", change.Path);
    }

    #endregion

    #region DeepEquals(string, string) validation

    [Fact]
    public void DeepEquals_StringString_NullLeft_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ((string)null!).DeepEquals("{\"a\":1}"));
        Assert.Equal("left", ex.ParamName);
    }

    [Fact]
    public void DeepEquals_StringString_NullRight_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => "{\"a\":1}".DeepEquals(null!));
        Assert.Equal("right", ex.ParamName);
    }

    [Fact]
    public void DeepEquals_StringString_EmptyLeft_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => string.Empty.DeepEquals("{\"a\":1}"));
        Assert.Equal("left", ex.ParamName);
    }

    [Fact]
    public void DeepEquals_StringString_WhitespaceLeft_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => "   \n\t  ".DeepEquals("{\"a\":1}"));
        Assert.Equal("left", ex.ParamName);
    }

    [Fact]
    public void DeepEquals_StringString_EmptyRight_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => "{\"a\":1}".DeepEquals(string.Empty));
        Assert.Equal("right", ex.ParamName);
    }

    [Fact]
    public void DeepEquals_StringString_WhitespaceRight_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => "{\"a\":1}".DeepEquals("   \n\t  "));
        Assert.Equal("right", ex.ParamName);
    }

    [Fact]
    public void DeepEquals_StringString_InvalidJsonLeft_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => "not json".DeepEquals("{\"a\":1}"));
    }

    [Fact]
    public void DeepEquals_StringString_InvalidJsonRight_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => "{\"a\":1}".DeepEquals("not json"));
    }

    [Fact]
    public void DeepEquals_StringString_DuplicateKeys_LastWins()
    {
        // Verify duplicate key behavior for DeepEquals
        Assert.True("{\"a\":1,\"a\":2}".DeepEquals("{\"a\":2}"));
        Assert.False("{\"a\":2,\"a\":1}".DeepEquals("{\"a\":2}"));
    }

    #endregion
}
