using System.Linq;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

public class DiffTests
{
    [Fact]
    public void IdenticalDocuments_ProduceNoChanges()
    {
        var changes = JsonDiffer.Diff("""{"a":1,"b":2}""", """{"a":1,"b":2}""");
        Assert.Empty(changes);
    }

    [Fact]
    public void KeyOrder_IsIgnored()
    {
        var changes = JsonDiffer.Diff("""{"a":1,"b":2}""", """{"b":2,"a":1}""");
        Assert.Empty(changes);
    }

    [Fact]
    public void AddedProperty_IsReported()
    {
        var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":1,"b":2}""");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/b", c.Path);
    }

    [Fact]
    public void RemovedProperty_IsReported()
    {
        var changes = JsonDiffer.Diff("""{"a":1,"b":2}""", """{"a":1}""");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/b", c.Path);
    }

    [Fact]
    public void ChangedScalar_IsReported()
    {
        var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":2}""");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
    }

    [Fact]
    public void KindChange_IsReportedWithoutDescent()
    {
        var changes = JsonDiffer.Diff("""{"a":{"x":1}}""", """{"a":5}""");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
    }

    [Fact]
    public void NestedPaths_UseSlashSeparator()
    {
        var changes = JsonDiffer.Diff("""{"user":{"name":"a"}}""", """{"user":{"name":"b"}}""");
        var c = Assert.Single(changes);
        Assert.Equal("/user/name", c.Path);
    }

    [Fact]
    public void Arrays_DiffByIndex()
    {
        var changes = JsonDiffer.Diff("""[1,2,3]""", """[1,9,3]""");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/1", c.Path);
    }

    [Fact]
    public void ShorterArray_ReportsRemovedTail()
    {
        var changes = JsonDiffer.Diff("""[1,2,3]""", """[1,2]""");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/2", c.Path);
    }

    [Fact]
    public void LongerArray_ReportsAddedTail()
    {
        var changes = JsonDiffer.Diff("""[1]""", """[1,2,3]""");
        Assert.Equal(2, changes.Count);
        Assert.All(changes, c => Assert.Equal(ChangeKind.Added, c.Kind));
    }

    [Fact]
    public void NumericTolerance_TreatsEquivalentNumbersAsEqual()
    {
        var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":1.0}""");
        Assert.Empty(changes);
    }

    [Fact]
    public void NumericTolerance_Off_ReportsRawTextDifference()
    {
        var opts = new DiffOptions { NumericTolerance = false };
        var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":1.0}""", opts);
        Assert.Single(changes);
    }

    [Fact]
    public void IgnorePropertyCase_MatchesRegardlessOfCase()
    {
        var opts = new DiffOptions { IgnorePropertyCase = true };
        var changes = JsonDiffer.Diff("""{"Name":"a"}""", """{"name":"a"}""", opts);
        Assert.Empty(changes);
    }

    [Fact]
    public void PathSegmentsWithSlash_AreEscaped()
    {
        var changes = JsonDiffer.Diff("""{"a/b":1}""", """{"a/b":2}""");
        var c = Assert.Single(changes);
        Assert.Equal("/a~1b", c.Path);
    }

    [Fact]
    public void ToString_FormatsChange()
    {
        var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":2}""");
        Assert.Equal("~ /a: 1 -> 2", changes.Single().ToString());
    }
}
