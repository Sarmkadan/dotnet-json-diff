using System.Linq;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Test suite for JSON diffing functionality.
/// Validates that the JsonDiffer correctly identifies and reports differences between JSON documents.
/// </summary>
public class DiffTests
{
    /// <summary>
    /// Tests that identical JSON documents produce no differences.
    /// </summary>
    [Fact]
    public void IdenticalDocuments_ProduceNoChanges()
    {
        var changes = JsonDiffer.Diff("{\"a\":1,\"b\":2}", "{\"a\":1,\"b\":2}");
        Assert.Empty(changes);
    }

    /// <summary>
    /// Tests that property order differences are ignored during comparison.
    /// </summary>
    [Fact]
    public void KeyOrder_IsIgnored()
    {
        var changes = JsonDiffer.Diff("{\"a\":1,\"b\":2}", "{\"b\":2,\"a\":1}");
        Assert.Empty(changes);
    }

    /// <summary>
    /// Tests that adding a property is correctly detected and reported.
    /// </summary>
    [Fact]
    public void AddedProperty_IsReported()
    {
        var changes = JsonDiffer.Diff("{\"a\":1}", "{\"a\":1,\"b\":2}");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/b", c.Path);
    }

    /// <summary>
    /// Tests that removing a property is correctly detected and reported.
    /// </summary>
    [Fact]
    public void RemovedProperty_IsReported()
    {
        var changes = JsonDiffer.Diff("{\"a\":1,\"b\":2}", "{\"a\":1}");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/b", c.Path);
    }

    /// <summary>
    /// Tests that changing a scalar value is correctly detected and reported.
    /// </summary>
    [Fact]
    public void ChangedScalar_IsReported()
    {
        var changes = JsonDiffer.Diff("{\"a\":1}", "{\"a\":2}");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
    }

    /// <summary>
    /// Tests that changing the kind of a value (e.g., from object to scalar) is reported at the parent level.
    /// </summary>
    [Fact]
    public void KindChange_IsReportedWithoutDescent()
    {
        var changes = JsonDiffer.Diff("{\"a\":{\"x\":1}}", "{\"a\":5}");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
    }

    /// <summary>
    /// Tests that nested property paths use slash notation for separation.
    /// </summary>
    [Fact]
    public void NestedPaths_UseSlashSeparator()
    {
        var changes = JsonDiffer.Diff("{\"user\":{\"name\":\"a\"}}", "{\"user\":{\"name\":\"b\"}}");
        var c = Assert.Single(changes);
        Assert.Equal("/user/name", c.Path);
    }

    /// <summary>
    /// Tests that array elements are compared by their index positions.
    /// </summary>
    [Fact]
    public void Arrays_DiffByIndex()
    {
        var changes = JsonDiffer.Diff("[1,2,3]", "[1,9,3]");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/1", c.Path);
    }

    /// <summary>
    /// Tests that removing elements from the end of an array is detected.
    /// </summary>
    [Fact]
    public void ShorterArray_ReportsRemovedTail()
    {
        var changes = JsonDiffer.Diff("[1,2,3]", "[1,2]");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/2", c.Path);
    }

    /// <summary>
    /// Tests that adding elements to the end of an array is detected.
    /// </summary>
    [Fact]
    public void LongerArray_ReportsAddedTail()
    {
        var changes = JsonDiffer.Diff("[1]", "[1,2,3]");
        Assert.Equal(2, changes.Count);
        Assert.All(changes, c => Assert.Equal(ChangeKind.Added, c.Kind));
    }

    /// <summary>
    /// Tests that numeric tolerance treats equivalent numbers (1 vs 1.0) as equal.
    /// </summary>
    [Fact]
    public void NumericTolerance_TreatsEquivalentNumbersAsEqual()
    {
        var changes = JsonDiffer.Diff("{\"a\":1}", "{\"a\":1.0}");
        Assert.Empty(changes);
    }

    /// <summary>
    /// Tests that disabling numeric tolerance reports raw text differences between numbers.
    /// </summary>
    [Fact]
    public void NumericTolerance_Off_ReportsRawTextDifference()
    {
        var opts = new DiffOptions { NumericTolerance = false };
        var changes = JsonDiffer.Diff("{\"a\":1}", "{\"a\":1.0}", opts);
        Assert.Single(changes);
    }

    /// <summary>
    /// Tests that property name case differences can be ignored when configured.
    /// </summary>
    [Fact]
    public void IgnorePropertyCase_MatchesRegardlessOfCase()
    {
        var opts = new DiffOptions { IgnorePropertyCase = true };
        var changes = JsonDiffer.Diff("{\"Name\":\"a\"}", "{\"name\":\"a\"}", opts);
        Assert.Empty(changes);
    }

    /// <summary>
    /// Tests that path segments containing forward slashes are properly escaped.
    /// </summary>
    [Fact]
    public void PathSegmentsWithSlash_AreEscaped()
    {
        var changes = JsonDiffer.Diff("{\"a/b\":1}", "{\"a/b\":2}");
        var c = Assert.Single(changes);
        Assert.Equal("/a~1b", c.Path);
    }

    /// <summary>
    /// Tests that the ToString method formats change information correctly.
    /// </summary>
    [Fact]
    public void ToString_FormatsChange()
    {
        var changes = JsonDiffer.Diff("{\"a\":1}", "{\"a\":2}");
        Assert.Equal("~ /a: 1 -> 2", changes.Single().ToString());
    }

    /// <summary>
    /// Tests that MaxDepth limits traversal and reports subtree differences as a single change.
    /// </summary>
    [Fact]
    public void MaxDepth_WithDepthLimit_ReportsSubtreeAsSingleChange()
    {
        // Create nested objects where only the innermost property differs
        var left = "{\"level1\":{\"level2\":{\"level3\":{\"value\":1}}}}";
        var right = "{\"level1\":{\"level2\":{\"level3\":{\"value\":2}}}}";

        // With MaxDepth=1, we should only see the root level1 change
        var opts = new DiffOptions { MaxDepth = 1 };
        var changes = JsonDiffer.Diff(left, right, opts);
        Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, changes[0].Kind);
        Assert.Equal("/level1", changes[0].Path);
    }

    /// <summary>
    /// Tests that MaxDepth=1 with nested object difference reports the parent as changed.
    /// </summary>
    [Fact]
    public void MaxDepth_WithLimit1_NestedObjectDifference()
    {
        var left = "{\"user\":{\"name\":\"Alice\",\"age\":30}}";
        var right = "{\"user\":{\"name\":\"Bob\",\"age\":30}}";

        var opts = new DiffOptions { MaxDepth = 1 };
        var changes = JsonDiffer.Diff(left, right, opts);

        // Should report the "user" object as changed without descending to name/age
        Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, changes[0].Kind);
        Assert.Equal("/user", changes[0].Path);
    }

    /// <summary>
    /// Tests that MaxDepth=2 allows traversal to depth 2.
    /// </summary>
    [Fact]
    public void MaxDepth_WithLimit2_AllowsDeeperTraversal()
    {
        var left = "{\"a\":{\"b\":{\"c\":1}}}";
        var right = "{\"a\":{\"b\":{\"c\":2}}}";

        var opts = new DiffOptions { MaxDepth = 2 };
        var changes = JsonDiffer.Diff(left, right, opts);

        // Should report "b" as changed since we can traverse to depth 2
        Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, changes[0].Kind);
        Assert.Equal("/a/b", changes[0].Path);
    }

    /// <summary>
    /// Tests that MaxDepth=null allows unlimited traversal (default behavior).
    /// </summary>
    [Fact]
    public void MaxDepth_Null_AllowsUnlimitedTraversal()
    {
        var left = "{\"a\":{\"b\":{\"c\":1}}}";
        var right = "{\"a\":{\"b\":{\"c\":2}}}";

        var opts = new DiffOptions { MaxDepth = null };
        var changes = JsonDiffer.Diff(left, right, opts);

        // Should report "c" as changed since we can traverse to any depth
        Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, changes[0].Kind);
        Assert.Equal("/a/b/c", changes[0].Path);
    }

    /// <summary>
    /// Tests that MaxDepth works with arrays.
    /// </summary>
    [Fact]
    public void MaxDepth_WithArrays_StopsAtDepthLimit()
    {
        var left = "{\"items\":[{\"id\":1,\"value\":\"a\"},{\"id\":2,\"value\":\"b\"}]}";
        var right = "{\"items\":[{\"id\":1,\"value\":\"x\"},{\"id\":2,\"value\":\"b\"}]}";

        var opts = new DiffOptions { MaxDepth = 2 };
        var changes = JsonDiffer.Diff(left, right, opts);

        // Should report "items/0" as changed since we can traverse to depth 2
        Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, changes[0].Kind);
        Assert.Equal("/items/0", changes[0].Path);
    }

    /// <summary>
    /// Tests that removing the first element of an array is reported as a single Removed change
    /// when DetectArrayShifts is enabled.
    /// </summary>
    [Fact]
    public void DetectArrayShifts_RemovedFirstElement_ReportsSingleRemovedChange()
    {
        var opts = new DiffOptions { DetectArrayShifts = true };
        var changes = JsonDiffer.Diff("[1,2,3]", "[2,3]", opts);

        // Should report only the removed first element, not changes at all indices
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/0", c.Path);
        Assert.Equal("1", c.Left?.GetRawText());
    }

    /// <summary>
    /// Tests that adding an element at the beginning of an array is reported as a single Added change
    /// when DetectArrayShifts is enabled.
    /// </summary>
    [Fact]
    public void DetectArrayShifts_AddedFirstElement_ReportsSingleAddedChange()
    {
        var opts = new DiffOptions { DetectArrayShifts = true };
        var changes = JsonDiffer.Diff("[2,3]", "[1,2,3]", opts);

        // Should report only the added first element, not changes at all indices
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/0", c.Path);
        Assert.Equal("1", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that removing the last element of an array is reported as a single Removed change
    /// when DetectArrayShifts is enabled.
    /// </summary>
    [Fact]
    public void DetectArrayShifts_RemovedLastElement_ReportsSingleRemovedChange()
    {
        var opts = new DiffOptions { DetectArrayShifts = true };
        var changes = JsonDiffer.Diff("[1,2,3]", "[1,2]", opts);

        // Should report only the removed last element
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/2", c.Path);
        Assert.Equal("3", c.Left?.GetRawText());
    }

    /// <summary>
    /// Tests that adding an element at the end of an array is reported as a single Added change
    /// when DetectArrayShifts is enabled.
    /// </summary>
    [Fact]
    public void DetectArrayShifts_AddedLastElement_ReportsSingleAddedChange()
    {
        var opts = new DiffOptions { DetectArrayShifts = true };
        var changes = JsonDiffer.Diff("[1,2]", "[1,2,3]", opts);

        // Should report only the added last element
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/2", c.Path);
        Assert.Equal("3", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that DetectArrayShifts is disabled by default (existing behavior preserved).
    /// </summary>
    [Fact]
    public void DetectArrayShifts_DefaultDisabled_PreservesExistingBehavior()
    {
        // Without DetectArrayShifts enabled, should report changes at all indices
        var changes = JsonDiffer.Diff("[1,2,3]", "[2,3]");

        // Should report multiple changes (the bug this feature fixes)
        // Old behavior: compares index-by-index, so [1,2,3] vs [2,3] gives:
        // - /0: 1 vs 2 -> Changed
        // - /1: 2 vs 3 -> Changed
        // - /2: 3 vs null -> Removed
        Assert.Equal(3, changes.Count);
        Assert.Contains(changes, c => c.Kind == ChangeKind.Changed && c.Path == "/0");
        Assert.Contains(changes, c => c.Kind == ChangeKind.Changed && c.Path == "/1");
        Assert.Contains(changes, c => c.Kind == ChangeKind.Removed && c.Path == "/2");
    }

    /// <summary>
    /// Tests that DetectArrayShifts handles nested arrays correctly.
    /// </summary>
    [Fact]
    public void DetectArrayShifts_NestedArrays_ReportsCorrectChanges()
    {
        var opts = new DiffOptions { DetectArrayShifts = true };
        var changes = JsonDiffer.Diff(
            "[[1],[2],[3],[4]]",
            "[[2],[3],[4]]",
            opts);

        // Should report only the removed first nested array
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/0", c.Path);
    }

    /// <summary>
    /// Tests that DetectArrayShifts handles arrays of objects correctly.
    /// </summary>
    [Fact]
    public void DetectArrayShifts_ArraysOfObjects_ReportsCorrectChanges()
    {
        var opts = new DiffOptions { DetectArrayShifts = true };
        var changes = JsonDiffer.Diff(
            "[{\"id\":1},{\"id\":2},{\"id\":3}]",
            "[{\"id\":2},{\"id\":3}]",
            opts);

        // Should report only the removed first object
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/0", c.Path);
    }

    /// <summary>
    /// Tests that DetectArrayShifts falls back to index-by-index when arrays don't match as prefix/suffix.
    /// </summary>
    [Fact]
    public void DetectArrayShifts_NonMatchingArrays_FallsBackToIndexByIndex()
    {
        var opts = new DiffOptions { DetectArrayShifts = true };
        var changes = JsonDiffer.Diff("[1,2,3]", "[1,9,3]", opts);

        // Should report the changed element in the middle
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/1", c.Path);
    }
}