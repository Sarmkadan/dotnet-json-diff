using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JsonDiff.Tests
{
    /// <summary>
    /// Extension methods for <see cref="ScalarEdgeCaseTests"/> that provide utility functionality
    /// for testing scalar value edge cases in JSON comparison scenarios.
    /// </summary>
    public static class ScalarEdgeCaseTestsExtensions
    {
        /// <summary>
        /// Creates a collection of test cases for numeric tolerance scenarios.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An enumerable of test case tuples containing left JSON, right JSON, and expected change count.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static IEnumerable<(string Left, string Right, int ExpectedChanges)> GetNumericToleranceTestCases(this ScalarEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return new[]
            {
                ("1", "1.0", 0),
                ("1.0", "1", 0),
                ("100", "1e2", 0),
                ("0.0", "-0.0", 0),
                ("9007199254740993", "9007199254740992", 0),
                ("123.456", "123.457", 1),
                ("1.23456789", "1.23456790", 1)
            };
        }

        /// <summary>
        /// Creates a collection of test cases for string comparison scenarios.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An enumerable of test case tuples containing left JSON, right JSON, and expected change count.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static IEnumerable<(string Left, string Right, int ExpectedChanges)> GetStringComparisonTestCases(this ScalarEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return new[]
            {
                ("{\"a\":\"Hello\"}", "{\"a\":\"hello\"}", 1),
                ("{\"a\":\"Test\"}", "{\"a\":\"test\"}", 1),
                ("{\"a\":\"Same\"}", "{\"a\":\"Same\"}", 0),
                ("{\"a\":\"Café\"}", "{\"a\":\"café\"}", 1),
                ("{\"a\":\"\"}", "{\"a\":\" \"}", 1)
            };
        }

        /// <summary>
        /// Creates a collection of test cases for boolean comparison scenarios.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An enumerable of test case tuples containing left JSON, right JSON, and expected change count.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static IEnumerable<(string Left, string Right, int ExpectedChanges)> GetBooleanComparisonTestCases(this ScalarEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return new[]
            {
                ("{\"a\":true}", "{\"a\":false}", 1),
                ("{\"a\":false}", "{\"a\":true}", 1),
                ("{\"a\":true}", "{\"a\":true}", 0),
                ("{\"a\":false}", "{\"a\":false}", 0),
                ("{\"a\":true, \"b\":false}", "{\"a\":false, \"b\":true}", 2)
            };
        }

        /// <summary>
        /// Creates a collection of test cases for null vs missing property scenarios.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An enumerable of test case tuples containing left JSON, right JSON, expected change count, and expected change kind.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static IEnumerable<(string Left, string Right, int ExpectedChanges, JsonDiff.ChangeKind ExpectedKind)> GetNullVsMissingTestCases(this ScalarEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return new[]
            {
                ("{\"a\":null}", "{}", 1, JsonDiff.ChangeKind.Removed),
                ("{}", "{\"a\":null}", 1, JsonDiff.ChangeKind.Added),
                ("{\"a\":null, \"b\":1}", "{\"b\":1}", 1, JsonDiff.ChangeKind.Removed),
                ("{\"b\":1}", "{\"a\":null, \"b\":1}", 1, JsonDiff.ChangeKind.Added),
                ("{\"a\":null}", "{\"a\":null}", 0, default(JsonDiff.ChangeKind))
            };
        }

        /// <summary>
        /// Creates a collection of test cases for type mismatch scenarios.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An enumerable of test case tuples containing left JSON, right JSON, and expected change count.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static IEnumerable<(string Left, string Right, int ExpectedChanges)> GetTypeMismatchTestCases(this ScalarEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return new[]
            {
                ("{}", "[]", 1),
                ("[]", "{}", 1),
                ("{\"a\":1}", "[1]", 1),
                ("[1]", "{\"a\":1}", 1),
                ("true", "1", 1),
                ("\"string\"", "123", 1),
                ("null", "{}", 1)
            };
        }

        /// <summary>
        /// Creates a collection of test cases for numeric edge cases including special values.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An enumerable of test case tuples containing left JSON, right JSON, and expected change count.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static IEnumerable<(string Left, string Right, int ExpectedChanges)> GetNumericEdgeCaseTestCases(this ScalarEdgeCaseTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return new[]
            {
                ("0", "-0", 0),
                ("Infinity", "1e999", 0),
                ("-Infinity", "-1e999", 0),
                ("NaN", "NaN", 0),
                ("1.7976931348623157E+308", "1.7976931348623158E+308", 1),
                ("4.9406564584124654E-324", "0", 1)
            };
        }

        /// <summary>
        /// Gets the expected change kind for a given test case path.
        /// </summary>
        /// <param name="changes">The collection of changes.</param>
        /// <param name="path">The JSON path to check.</param>
        /// <returns>The change kind at the specified path.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="changes"/> is null.</exception>
        public static JsonDiff.ChangeKind GetChangeKind(this IEnumerable<JsonDiff.JsonChange> changes, string path)
        {
            ArgumentNullException.ThrowIfNull(changes);

            var change = changes.FirstOrDefault(c => string.Equals(c.Path, path, StringComparison.Ordinal));
            return change.Kind;
        }

        /// <summary>
        /// Gets the number of changes of a specific kind in the collection.
        /// </summary>
        /// <param name="changes">The collection of changes.</param>
        /// <param name="kind">The change kind to count.</param>
        /// <returns>The count of changes with the specified kind.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="changes"/> is null.</exception>
        public static int CountChangesOfKind(this IEnumerable<JsonDiff.JsonChange> changes, JsonDiff.ChangeKind kind)
        {
            ArgumentNullException.ThrowIfNull(changes);

            return changes.Count(c => c.Kind == kind);
        }

        /// <summary>
        /// Creates test JSON strings with various numeric formats for tolerance testing.
        /// </summary>
        /// <param name="value">The numeric value as a string.</param>
        /// <returns>A tuple containing different numeric format representations.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or empty.</exception>
        public static (string Integer, string Decimal, string Scientific) CreateNumericFormats(this ScalarEdgeCaseTests tests, string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);

            return (
                value,
                $"{value}.0",
                double.Parse(value, CultureInfo.InvariantCulture).ToString("E", CultureInfo.InvariantCulture)
            );
        }
    }
}