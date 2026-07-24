using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Extension methods for <see cref="DeepEqualsTests"/> that provide additional test utilities
/// for working with DeepEquals functionality.
/// </summary>
public static class DeepEqualsTestsExtensions
{
    /// <summary>
    /// Creates a JSON string with the specified scalar value.
    /// </summary>
    /// <param name="value">The scalar value to wrap in JSON.</param>
    /// <returns>A JSON string representation of the scalar value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    private static string WrapScalar(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value switch
        {
            string s => $"\"{s}\"",
            bool b => b.ToString().ToLowerInvariant(),
            null => "null",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    /// Tests that two JSON strings are deeply equal.
    /// </summary>
    /// <param name="left">The left JSON string.</param>
    /// <param name="right">The right JSON string.</param>
    /// <returns>True if the JSON strings are deeply equal; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either parameter is null.</exception>
    public static bool AreDeeplyEqual(this DeepEqualsTests _, string left, string right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return JsonDiffer.DeepEquals(left, right);
    }

    /// <summary>
    /// Tests that two JSON strings are deeply equal with the specified options.
    /// </summary>
    /// <param name="left">The left JSON string.</param>
    /// <param name="right">The right JSON string.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>True if the JSON strings are deeply equal; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public static bool AreDeeplyEqual(this DeepEqualsTests _, string left, string right, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(options);

        return JsonDiffer.DeepEquals(left, right, options);
    }

    /// <summary>
    /// Tests that two <see cref="JsonElement"/> instances are deeply equal.
    /// </summary>
    /// <param name="left">The left JSON element.</param>
    /// <param name="right">The right JSON element.</param>
    /// <returns>True if the JSON elements are deeply equal; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either parameter is null.</exception>
    public static bool AreDeeplyEqual(this DeepEqualsTests _, JsonElement left, JsonElement right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return JsonDiffer.DeepEquals(left, right);
    }

    /// <summary>
    /// Tests that two <see cref="JsonElement"/> instances are deeply equal with the specified options.
    /// </summary>
    /// <param name="left">The left JSON element.</param>
    /// <param name="right">The right JSON element.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>True if the JSON elements are deeply equal; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public static bool AreDeeplyEqual(this DeepEqualsTests _, JsonElement left, JsonElement right, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(options);

        return JsonDiffer.DeepEquals(left, right, options);
    }

    /// <summary>
    /// Creates a collection of test cases for verifying that various scalar values are equal.
    /// </summary>
    /// <returns>An enumerable of test cases where scalar values should be equal.</returns>
    public static IEnumerable<object[]> GetScalarEqualityTestCases()
    {
        yield return new object[] { "1", "1.0", true };
        yield return new object[] { "1", "1e0", true };
        yield return new object[] { "0", "-0", true };
        yield return new object[] { "true", "true", true };
        yield return new object[] { "false", "false", true };
        yield return new object[] { "null", "null", true };
        yield return new object[] { "\"hello\"", "\"hello\"", true };
    }

    /// <summary>
    /// Creates a collection of test cases for verifying that various scalar values are not equal.
    /// </summary>
    /// <returns>An enumerable of test cases where scalar values should not be equal.</returns>
    public static IEnumerable<object[]> GetScalarInequalityTestCases()
    {
        yield return new object[] { "1", "2", false };
        yield return new object[] { "true", "false", false };
        yield return new object[] { "\"hello\"", "\"world\"", false };
        yield return new object[] { "null", "\"null\"", false };
    }

    /// <summary>
    /// Creates a collection of test cases for verifying property case sensitivity.
    /// </summary>
    /// <returns>An enumerable of test cases for property case sensitivity.</returns>
    public static IEnumerable<object[]> GetPropertyCaseTestCases()
    {
        yield return new object[] { "{\"Name\":\"Alice\"}", "{\"name\":\"Alice\"}", false };
        yield return new object[] { "{\"NAME\":\"Alice\"}", "{\"name\":\"Alice\"}", false };
        yield return new object[] { "{\"Name\":\"Alice\"}", "{\"Name\":\"Alice\"}", true };
    }

    /// <summary>
    /// Creates a collection of test cases for verifying array equality.
    /// </summary>
    /// <returns>An enumerable of test cases for array equality.</returns>
    public static IEnumerable<object[]> GetArrayTestCases()
    {
        yield return new object[] { "[1,2,3]", "[1,2,3]", true };
        yield return new object[] { "[1,2,3]", "[3,2,1]", false };
        yield return new object[] { "[]", "[]", true };
        yield return new object[] { "[1]", "[1,2]", false };
    }

    /// <summary>
    /// Creates a collection of test cases for verifying numeric tolerance behavior.
    /// </summary>
    /// <param name="withTolerance">Whether to use numeric tolerance.</param>
    /// <returns>An enumerable of test cases for numeric tolerance.</returns>
    public static IEnumerable<object[]> GetNumericToleranceTestCases(bool withTolerance)
    {
        var tolerance = withTolerance ? "tolerance" : "no tolerance";
        var opts = withTolerance ? new DiffOptions { NumericTolerance = true } : new DiffOptions { NumericTolerance = false };

        yield return new object[] { $"{{ \"value\": 1 }}", $"{{ \"value\": 1.0 }}", withTolerance, opts };
        yield return new object[] { $"{{ \"value\": 100 }}", $"{{ \"value\": 1e2 }}", withTolerance, opts };
        yield return new object[] { $"{{ \"value\": 0 }}", $"{{ \"value\": -0 }}", withTolerance, opts };
    }

    /// <summary>
    /// Creates a collection of test cases for verifying max depth behavior.
    /// </summary>
    /// <returns>An enumerable of test cases for max depth.</returns>
    public static IEnumerable<object[]> GetMaxDepthTestCases()
    {
        yield return new object[] { "{\"a\":{\"b\":{\"c\":1}}}", "{\"a\":{\"b\":{\"c\":2}}}", 2, false };
        yield return new object[] { "{\"a\":{\"b\":{\"c\":1}}}", "{\"a\":{\"b\":{\"c\":1}}}", 3, true };
        yield return new object[] { "{\"user\":{\"name\":\"Alice\",\"age\":30}}", "{\"user\":{\"name\":\"Bob\",\"age\":30}}", 1, false };
    }
}