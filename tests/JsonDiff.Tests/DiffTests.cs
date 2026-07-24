using System.Linq;
using System.Text;
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
    /// Tests that changing a property value is correctly detected.
    /// </summary>
    [Fact]
    public void ChangedValue_IsReported()
    {
        var changes = JsonDiffer.Diff("{\"a\":1}", "{\"a\":2}");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
        Assert.Equal("1", c.Left?.GetRawText());
        Assert.Equal("2", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that nested object differences are correctly detected.
    /// </summary>
    [Fact]
    public void NestedObjectDifference_IsReported()
    {
        var changes = JsonDiffer.Diff(
            "{\"user\":{\"name\":\"Alice\",\"age\":30}}",
            "{\"user\":{\"name\":\"Bob\",\"age\":30}}");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/user/name", c.Path);
    }

    /// <summary>
    /// Tests that array element additions are correctly detected.
    /// </summary>
    [Fact]
    public void ArrayElementAddition_IsReported()
    {
        var changes = JsonDiffer.Diff("[1,2,3]", "[1,2,3,4]");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/3", c.Path);
    }

    /// <summary>
    /// Tests that array element removals are correctly detected.
    /// </summary>
    [Fact]
    public void ArrayElementRemoval_IsReported()
    {
        var changes = JsonDiffer.Diff("[1,2,3]", "[1,2]");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/2", c.Path);
    }

    /// <summary>
    /// Tests that array element changes are correctly detected.
    /// </summary>
    [Fact]
    public void ArrayElementChange_IsReported()
    {
        var changes = JsonDiffer.Diff("[1,2,3]", "[1,9,3]");
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/1", c.Path);
    }

    /// <summary>
    /// Tests that MaxDepth limits traversal and reports subtree differences as a single change.
    /// </summary>
    [Fact]
    public void MaxDepth_WithDepthLimit_ReportsSubtreeAsSingleChange()
    {
        // Create nested objects where only the innermost property differs
        var left = "{\"level1\":{\"level2\":{\"level3\":{\"value\":1}}}";
        var right = "{\"level1\":{\"level2\":{\"level3\":{\"value\":2}}}";

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

        // Should report only the added first element
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/0", c.Path);
        Assert.Equal("1", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that changing an element in the middle of an array is reported correctly
    /// when DetectArrayShifts is enabled.
    /// </summary>
    [Fact]
    public void DetectArrayShifts_ChangedMiddleElement_ReportsSingleChangedChange()
    {
        var opts = new DiffOptions { DetectArrayShifts = true };
        var changes = JsonDiffer.Diff("[1,2,3]", "[1,9,3]", opts);

        // Should report the changed element in the middle
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/1", c.Path);
    }

    /// <summary>
    /// Tests that MaxNodeCount limits traversal and throws JsonDiffLimitExceededException when exceeded.
    /// </summary>
    [Fact]
    public void MaxNodeCount_WithLimit_ThrowsWhenExceeded()
    {
        // Create a wide object with many properties that will exceed the default MaxNodeCount of 100,000
        // Each property adds 1 node, so 100,001 properties will exceed the limit
        var sb = new StringBuilder();
        sb.Append("{");
        for (int i = 0; i < 100_002; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"\"prop{i}\":{i}");
        }
        sb.Append("}");

        var left = sb.ToString();
        var right = left; // Same structure

        // Should throw JsonDiffLimitExceededException (100,002 > 100,000 limit)
        var ex = Assert.Throws<JsonDiffLimitExceededException>(() => JsonDiffer.Diff(left, right));
        Assert.Contains("exceeds maximum node count limit", ex.Message);
        Assert.Equal("/", ex.Path);
    }

    /// <summary>
    /// Tests that MaxNodeCount allows documents under the limit to process normally.
    /// </summary>
    [Fact]
    public void MaxNodeCount_UnderLimit_ProcessesSuccessfully()
    {
        // Create a document with 50,000 properties (under the default limit of 100,000)
        var sb = new StringBuilder();
        sb.Append("{");
        for (int i = 0; i < 50_000; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"\"prop{i}\":{i}");
        }
        sb.Append("}");

        var left = sb.ToString();
        var right = left; // Same structure

        // Should process without throwing
        var changes = JsonDiffer.Diff(left, right);
        Assert.Empty(changes);
    }

    /// <summary>
    /// Tests that MaxNodeCount can be customized to allow larger documents.
    /// </summary>
    [Fact]
    public void MaxNodeCount_CustomLimit_AllowsLargerDocuments()
    {
        // Create a document with 150,000 properties (over default limit but under custom limit)
        var sb = new StringBuilder();
        sb.Append("{");
        for (int i = 0; i < 150_000; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"\"prop{i}\":{i}");
        }
        sb.Append("}");

        var left = sb.ToString();
        var right = left; // Same structure

        var opts = new DiffOptions { MaxNodeCount = 200_000 };

        // Should process without throwing
        var changes = JsonDiffer.Diff(left, right, opts);
        Assert.Empty(changes);
    }

    /// <summary>
    /// Tests that MaxNodeCount=0 throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void MaxNodeCount_Zero_ThrowsArgumentOutOfRangeException()
    {
        var opts = new DiffOptions { MaxNodeCount = 0 };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => JsonDiffer.Diff("{}", "{}", opts));
        Assert.Contains("MaxNodeCount must be a positive integer", ex.Message);
    }

    /// <summary>
    /// Tests that MaxNodeCount=null allows unlimited traversal.
    /// </summary>
    [Fact]
    public void MaxNodeCount_Null_AllowsUnlimitedTraversal()
    {
        // Create a document with 150,000 properties (over default limit)
        var sb = new StringBuilder();
        sb.Append("{");
        for (int i = 0; i < 150_000; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"\"prop{i}\":{i}");
        }
        sb.Append("}");

        var left = sb.ToString();
        var right = left; // Same structure

        var opts = new DiffOptions { MaxNodeCount = null };

        // Should process without throwing
        var changes = JsonDiffer.Diff(left, right, opts);
        Assert.Empty(changes);
    }
}