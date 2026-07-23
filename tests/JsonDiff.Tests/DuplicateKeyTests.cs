using System.Linq;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Tests the last-wins policy for duplicate object keys.
/// JSON permits repeated keys within an object; the differ resolves them to the
/// value of the last occurrence, matching System.Text.Json's dictionary deserialization.
/// </summary>
public class DuplicateKeyTests
{
    /// <summary>
    /// Duplicate key on the left side: only the last occurrence participates.
    /// </summary>
    [Fact]
    public void DuplicateKeyOnLeft_LastValueWins()
    {
        var changes = JsonDiffer.Diff("{\"a\":1,\"a\":2}", "{\"a\":2}");
        Assert.Empty(changes);
    }

    /// <summary>
    /// Duplicate key on the right side: only the last occurrence participates.
    /// </summary>
    [Fact]
    public void DuplicateKeyOnRight_LastValueWins()
    {
        var changes = JsonDiffer.Diff("{\"a\":2}", "{\"a\":1,\"a\":2}");
        Assert.Empty(changes);
    }

    /// <summary>
    /// Duplicate keys on both sides: last occurrences are compared against each other.
    /// </summary>
    [Fact]
    public void DuplicateKeysOnBothSides_LastValuesAreCompared()
    {
        var changes = JsonDiffer.Diff("{\"a\":1,\"a\":2}", "{\"a\":9,\"a\":2}");
        Assert.Empty(changes);
    }

    /// <summary>
    /// When the last occurrences differ, exactly one change is reported for the key.
    /// </summary>
    [Fact]
    public void DuplicateKey_DifferingLastValues_ReportSingleChange()
    {
        var changes = JsonDiffer.Diff("{\"a\":1,\"a\":2}", "{\"a\":2,\"a\":3}");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
        Assert.Equal(2, c.Left!.Value.GetInt32());
        Assert.Equal(3, c.Right!.Value.GetInt32());
    }

    /// <summary>
    /// A duplicated key missing from the other side is reported once, not per occurrence.
    /// </summary>
    [Fact]
    public void DuplicateKey_RemovedProperty_ReportedOnce()
    {
        var changes = JsonDiffer.Diff("{\"a\":1,\"a\":2}", "{}");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/a", c.Path);
        Assert.Equal(2, c.Left!.Value.GetInt32());
    }

    /// <summary>
    /// DeepEquals applies the same last-wins policy as Diff.
    /// </summary>
    [Fact]
    public void DeepEquals_DuplicateKeys_LastValueWins()
    {
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1,\"a\":2}", "{\"a\":2}"));
        Assert.False(JsonDiffer.DeepEquals("{\"a\":2,\"a\":1}", "{\"a\":2}"));
    }

    /// <summary>
    /// Duplicate keys inside nested objects are also resolved last-wins.
    /// </summary>
    [Fact]
    public void DuplicateKeys_InNestedObject_LastValueWins()
    {
        var changes = JsonDiffer.Diff("{\"o\":{\"x\":1,\"x\":2}}", "{\"o\":{\"x\":2}}");
        Assert.Empty(changes);
    }
}
