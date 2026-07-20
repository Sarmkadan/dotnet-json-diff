using System.Text.Json;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Test suite for DeepEquals functionality.
/// Validates that the JsonDiffer.DeepEquals correctly determines semantic equality between JSON documents.
/// </summary>
public class DeepEqualsTests
{
    /// <summary>
    /// Tests that identical JSON documents are considered equal.
    /// </summary>
    [Fact]
    public void IdenticalDocuments_AreEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1,\"b\":2}", "{\"a\":1,\"b\":2}"));
    }

    /// <summary>
    /// Tests that property order differences are ignored during comparison.
    /// </summary>
    [Fact]
    public void KeyOrder_IsIgnored()
    {
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1,\"b\":2}", "{\"b\":2,\"a\":1}"));
    }

    /// <summary>
    /// Tests that adding a property makes documents not equal.
    /// </summary>
    [Fact]
    public void AddedProperty_MakesNotEqual()
    {
        Assert.False(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1,\"b\":2}"));
    }

    /// <summary>
    /// Tests that removing a property makes documents not equal.
    /// </summary>
    [Fact]
    public void RemovedProperty_MakesNotEqual()
    {
        Assert.False(JsonDiffer.DeepEquals("{\"a\":1,\"b\":2}", "{\"a\":1}"));
    }

    /// <summary>
    /// Tests that changing a scalar value makes documents not equal.
    /// </summary>
    [Fact]
    public void ChangedScalar_MakesNotEqual()
    {
        Assert.False(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":2}"));
    }

    /// <summary>
    /// Tests that changing the kind of a value makes documents not equal.
    /// </summary>
    [Fact]
    public void KindChange_MakesNotEqual()
    {
        Assert.False(JsonDiffer.DeepEquals("{\"a\":{\"x\":1}}", "{\"a\":5}"));
    }

    /// <summary>
    /// Tests that nested property differences are detected.
    /// </summary>
    [Fact]
    public void NestedPropertyDifference_IsDetected()
    {
        Assert.False(JsonDiffer.DeepEquals("{\"user\":{\"name\":\"a\"}}", "{\"user\":{\"name\":\"b\"}}"));
    }

    /// <summary>
    /// Tests that array element differences are detected.
    /// </summary>
    [Fact]
    public void ArrayElementDifference_IsDetected()
    {
        Assert.False(JsonDiffer.DeepEquals("[1,2,3]", "[1,9,3]"));
    }

    /// <summary>
    /// Tests that array length differences are detected.
    /// </summary>
    [Fact]
    public void ArrayLengthDifference_IsDetected()
    {
        Assert.False(JsonDiffer.DeepEquals("[1,2,3]", "[1,2]"));
        Assert.False(JsonDiffer.DeepEquals("[1]", "[1,2,3]"));
    }

    /// <summary>
    /// Tests that numeric tolerance treats equivalent numbers (1 vs 1.0) as equal.
    /// </summary>
    [Fact]
    public void NumericTolerance_TreatsEquivalentNumbersAsEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1.0}"));
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1.00}"));
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1e0}", "{\"a\":1}"));
    }

    /// <summary>
    /// Tests that disabling numeric tolerance reports equivalent numbers as different.
    /// </summary>
    [Fact]
    public void NumericTolerance_Off_ReportsEquivalentNumbersAsDifferent()
    {
        var opts = new DiffOptions { NumericTolerance = false };
        Assert.False(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1.0}", opts));
        Assert.False(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1.00}", opts));
    }

    /// <summary>
    /// Tests that property name case differences can be ignored when configured.
    /// </summary>
    [Fact]
    public void IgnorePropertyCase_MatchesRegardlessOfCase()
    {
        var opts = new DiffOptions { IgnorePropertyCase = true };
        Assert.True(JsonDiffer.DeepEquals("{\"Name\":\"a\"}", "{\"name\":\"a\"}", opts));
        Assert.True(JsonDiffer.DeepEquals("{\"NAME\":\"a\"}", "{\"name\":\"a\"}", opts));
    }

    /// <summary>
    /// Tests that property name case differences are detected when not configured.
    /// </summary>
    [Fact]
    public void PropertyCaseDifference_IsDetected_WhenNotConfigured()
    {
        Assert.False(JsonDiffer.DeepEquals("{\"Name\":\"a\"}", "{\"name\":\"a\"}"));
    }

    /// <summary>
    /// Tests that null values are handled correctly.
    /// </summary>
    [Fact]
    public void NullValues_AreEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("{\"a\":null}", "{\"a\":null}"));
        Assert.True(JsonDiffer.DeepEquals("null", "null"));
    }

    /// <summary>
    /// Tests that boolean values are handled correctly.
    /// </summary>
    [Fact]
    public void BooleanValues_AreEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("{\"a\":true}", "{\"a\":true}"));
        Assert.True(JsonDiffer.DeepEquals("{\"a\":false}", "{\"a\":false}"));
        Assert.False(JsonDiffer.DeepEquals("{\"a\":true}", "{\"a\":false}"));
    }

    /// <summary>
    /// Tests that string values are compared correctly.
    /// </summary>
    [Fact]
    public void StringValues_AreEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("{\"a\":\"hello\"}", "{\"a\":\"hello\"}"));
        Assert.False(JsonDiffer.DeepEquals("{\"a\":\"hello\"}", "{\"a\":\"world\"}"));
    }

    /// <summary>
    /// Tests that DeepEquals with JsonElement overload works correctly.
    /// </summary>
    [Fact]
    public void DeepEquals_JsonElementOverload_WorksCorrectly()
    {
        var left = JsonDocument.Parse("{\"a\":1,\"b\":2}").RootElement;
        var right = JsonDocument.Parse("{\"b\":2,\"a\":1}").RootElement;
        Assert.True(JsonDiffer.DeepEquals(left, right));
    }

    /// <summary>
    /// Tests that DeepEquals with JsonElement overload detects differences.
    /// </summary>
    [Fact]
    public void DeepEquals_JsonElementOverload_DetectsDifferences()
    {
        var left = JsonDocument.Parse("{\"a\":1}").RootElement;
        var right = JsonDocument.Parse("{\"a\":2}").RootElement;
        Assert.False(JsonDiffer.DeepEquals(left, right));
    }

    /// <summary>
    /// Tests that MaxDepth limits traversal - with MaxDepth=2 we can traverse to depth 2.
    /// </summary>
    [Fact]
    public void MaxDepth_Limited_AllowsTraversalToDepth2()
    {
        // Create nested objects where only the innermost property differs
        var left = "{\"level1\":{\"level2\":{\"level3\":{\"value\":1}}}}";
        var right = "{\"level1\":{\"level2\":{\"level3\":{\"value\":2}}}}";

        // With MaxDepth=2, we can traverse to depth 2 (level1/level2), so we detect the difference at level3
        var opts = new DiffOptions { MaxDepth = 2 };
        Assert.False(JsonDiffer.DeepEquals(left, right, opts));
    }

    /// <summary>
    /// Tests that MaxDepth limits traversal - with MaxDepth=1 we can only compare at root level.
    /// </summary>
    [Fact]
    public void MaxDepth_WithLimit1_ComparesAtRootLevel()
    {
        var left = "{\"user\":{\"name\":\"Alice\",\"age\":30}}";
        var right = "{\"user\":{\"name\":\"Bob\",\"age\":30}}";

        // With MaxDepth=1, we can only compare at root level, and user objects are different
        var opts = new DiffOptions { MaxDepth = 1 };
        Assert.False(JsonDiffer.DeepEquals(left, right, opts));
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
        Assert.False(JsonDiffer.DeepEquals(left, right, opts));
    }

    /// <summary>
    /// Tests that empty objects are equal.
    /// </summary>
    [Fact]
    public void EmptyObjects_AreEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("{}", "{}"));
        Assert.True(JsonDiffer.DeepEquals("{}", "{ }")); // Whitespace difference in raw text, but semantically equal
    }

    /// <summary>
    /// Tests that empty arrays are equal.
    /// </summary>
    [Fact]
    public void EmptyArrays_AreEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("[]", "[]"));
    }

    /// <summary>
    /// Tests that arrays with different order are not equal.
    /// </summary>
    [Fact]
    public void ArrayOrderDifference_IsDetected()
    {
        Assert.False(JsonDiffer.DeepEquals("[1,2,3]", "[3,2,1]"));
    }

    /// <summary>
    /// Tests that arrays with same elements in same order are equal.
    /// </summary>
    [Fact]
    public void ArraySameElementsSameOrder_AreEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("[1,2,3]", "[1,2,3]"));
    }

    /// <summary>
    /// Tests that complex nested structures are handled correctly.
    /// </summary>
    [Fact]
    public void ComplexNestedStructures_AreEqual()
    {
        var left = "{\"users\":[{\"name\":\"Alice\",\"age\":30,\"active\":true},{\"name\":\"Bob\",\"age\":25,\"active\":false}]}";
        var right = "{\"users\":[{\"name\":\"Alice\",\"age\":30,\"active\":true},{\"name\":\"Bob\",\"age\":25,\"active\":false}]}";
        Assert.True(JsonDiffer.DeepEquals(left, right));
    }

    /// <summary>
    /// Tests that complex nested structures with differences are not equal.
    /// </summary>
    [Fact]
    public void ComplexNestedStructures_NotEqual_WhenDifferent()
    {
        var left = "{\"users\":[{\"name\":\"Alice\",\"age\":30}]}";
        var right = "{\"users\":[{\"name\":\"Alice\",\"age\":31}]}";
        Assert.False(JsonDiffer.DeepEquals(left, right));
    }

    /// <summary>
    /// Tests that numeric values with different representations are equal with tolerance.
    /// </summary>
    [Fact]
    public void NumericTolerance_VariousFormats_AreEqual()
    {
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1.0}"));
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1e0}"));
        Assert.True(JsonDiffer.DeepEquals("{\"a\":1.5}", "{\"a\":1.50}"));
        Assert.True(JsonDiffer.DeepEquals("{\"a\":0.001}", "{\"a\":0.0010}"));
    }

    /// <summary>
    /// Tests that numeric values with different representations are not equal without tolerance.
    /// </summary>
    [Fact]
    public void NumericTolerance_Off_VariousFormats_NotEqual()
    {
        var opts = new DiffOptions { NumericTolerance = false };
        Assert.False(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1.0}", opts));
        Assert.False(JsonDiffer.DeepEquals("{\"a\":1}", "{\"a\":1e0}", opts));
    }
}