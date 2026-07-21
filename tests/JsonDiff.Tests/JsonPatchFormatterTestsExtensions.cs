using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace JsonDiff.Tests
{
    /// <summary>
    /// Extension methods for <see cref="JsonPatchFormatterTests"/> that provide convenient assertions
    /// and helper methods for testing JSON Patch operations.
    /// </summary>
    public static class JsonPatchFormatterTestsExtensions
    {
        /// <summary>
        /// Creates a JSON Patch operation from the specified change and validates it matches the expected operation type.
        /// </summary>
        /// <param name="changes">The list of changes to format.</param>
        /// <param name="expectedOp">The expected operation type ("add", "remove", "replace", "move", "copy", "test").</param>
        /// <returns>A tuple containing the formatted JSON string and the parsed JsonElement.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
        public static (string Json, JsonElement Document) ToJsonPatchWithDocument(this JsonPatchFormatterTests _, IReadOnlyList<JsonChange> changes, string expectedOp)
        {
            ArgumentNullException.ThrowIfNull(changes);
            ArgumentException.ThrowIfNullOrEmpty(expectedOp);

            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Expected JSON Patch to be an array.");
            }

            var firstOp = root[0];
            var actualOp = firstOp.GetProperty("op").GetString();

            if (!string.Equals(actualOp, expectedOp, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected operation '{expectedOp}' but got '{actualOp}'.");
            }

            return (json, root);
        }

        /// <summary>
        /// Asserts that the JSON Patch contains exactly the specified number of operations.
        /// </summary>
        /// <param name="root">The root JsonElement containing the JSON Patch array.</param>
        /// <param name="expectedCount">The expected number of operations.</param>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> is null.</exception>
        public static void HasOperationCount(this JsonPatchFormatterTests _, JsonElement root, int expectedCount)
        {
            ArgumentNullException.ThrowIfNull(root);

            if (root.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Expected JSON Patch to be an array.");
            }

            if (root.GetArrayLength() != expectedCount)
            {
                throw new InvalidOperationException(
                    $"Expected {expectedCount} operations but found {root.GetArrayLength()}.");
            }
        }

        /// <summary>
        /// Gets the path from a JSON Patch operation.
        /// </summary>
        /// <param name="operation">The JSON Patch operation element.</param>
        /// <returns>The path value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The operation does not have a 'path' property.</exception>
        public static string GetPath(this JsonPatchFormatterTests _, JsonElement operation)
        {
            ArgumentNullException.ThrowIfNull(operation);

            if (!operation.TryGetProperty("path", out var path))
            {
                throw new InvalidOperationException("Operation does not contain a 'path' property.");
            }

            return path.GetString();
        }

        /// <summary>
        /// Gets the value from a JSON Patch operation.
        /// </summary>
        /// <param name="operation">The JSON Patch operation element.</param>
        /// <returns>The value as a JsonElement.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The operation does not have a 'value' property.</exception>
        public static JsonElement GetValue(this JsonPatchFormatterTests _, JsonElement operation)
        {
            ArgumentNullException.ThrowIfNull(operation);

            if (!operation.TryGetProperty("value", out var value))
            {
                throw new InvalidOperationException("Operation does not contain a 'value' property.");
            }

            return value;
        }

        /// <summary>
        /// Creates a JSON Patch from changes and returns the operations as a list for easy inspection.
        /// </summary>
        /// <param name="changes">The list of changes to format.</param>
        /// <returns>A list of JSON Patch operations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
        public static IReadOnlyList<JsonElement> ToOperations(this JsonPatchFormatterTests _, IReadOnlyList<JsonChange> changes)
        {
            ArgumentNullException.ThrowIfNull(changes);

            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Expected JSON Patch to be an array.");
            }

            var operations = new List<JsonElement>(root.GetArrayLength());
            foreach (var element in root.EnumerateArray())
            {
                operations.Add(element);
            }

            return operations.AsReadOnly();
        }

        /// <summary>
        /// Asserts that a JSON Patch operation has the expected value of a specific type.
        /// </summary>
        /// <typeparam name="T">The expected value type (int, string, bool, etc.).</typeparam>
        /// <param name="operation">The JSON Patch operation element.</param>
        /// <param name="expectedValue">The expected value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The operation does not have a 'value' property or type mismatch.</exception>
        public static void HasValue<T>(this JsonPatchFormatterTests _, JsonElement operation, T expectedValue)
        {
            ArgumentNullException.ThrowIfNull(operation);

            var value = GetValue(_, operation);

            if (value.ValueKind != JsonValueKind.Number && value.ValueKind != JsonValueKind.String && value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
            {
                throw new InvalidOperationException("Value is not a simple type (number, string, or boolean).");
            }

            var actualValue = value.GetRawText();
            var expectedText = expectedValue?.ToString() ?? "null";

            if (!string.Equals(actualValue, expectedText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected value '{expectedText}' but got '{actualValue}'.");
            }
        }

        /// <summary>
        /// Creates a JSON Patch from changes and parses the numeric value from the first operation.
        /// </summary>
        /// <param name="changes">The list of changes to format.</param>
        /// <returns>The numeric value from the first operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
        /// <exception cref="InvalidOperationException">No numeric value found or type mismatch.</exception>
        public static int GetFirstOperationNumericValue(this JsonPatchFormatterTests _, IReadOnlyList<JsonChange> changes)
        {
            ArgumentNullException.ThrowIfNull(changes);

            var operations = ToOperations(_, changes);
            if (operations.Count == 0)
            {
                throw new InvalidOperationException("No operations found in JSON Patch.");
            }

            var value = GetValue(_, operations[0]);
            return value.GetInt32();
        }

        /// <summary>
        /// Creates a JSON Patch from changes and parses the string value from the first operation.
        /// </summary>
        /// <param name="changes">The list of changes to format.</param>
        /// <returns>The string value from the first operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
        /// <exception cref="InvalidOperationException">No string value found or type mismatch.</exception>
        public static string GetFirstOperationStringValue(this JsonPatchFormatterTests _, IReadOnlyList<JsonChange> changes)
        {
            ArgumentNullException.ThrowIfNull(changes);

            var operations = ToOperations(_, changes);
            if (operations.Count == 0)
            {
                throw new InvalidOperationException("No operations found in JSON Patch.");
            }

            var value = GetValue(_, operations[0]);
            return value.GetString();
        }
    }
}