using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JsonDiff.Tests;

/// <summary>
/// Extension methods for <see cref="DiffTests"/> that provide additional utility functionality
/// for working with JSON diff results and test assertions.
/// </summary>
public static class DiffTestsExtensions
{
    /// <summary>
    /// Gets the first change at the specified path, or throws if not found.
    /// </summary>
    /// <param name="changes">The collection of changes to search.</param>
    /// <param name="path">The JSON path to find (e.g., "/user/name").</param>
    /// <returns>The change at the specified path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">No change found at the specified path.</exception>
    public static JsonChange ChangeAtPath(this IEnumerable<JsonChange> changes, string path)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentException.ThrowIfNullOrEmpty(path);

        return changes.FirstOrDefault(c => string.Equals(c.Path, path, StringComparison.Ordinal)) is var change
            && change.Path is not null
                ? change
                : throw new InvalidOperationException($"No change found at path '{path}'");
    }

    /// <summary>
    /// Asserts that exactly one change exists and returns it.
    /// </summary>
    /// <param name="changes">The collection of changes.</param>
    /// <returns>The single change.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Zero or more than one change exists.</exception>
    public static JsonChange SingleChange(this IEnumerable<JsonChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        return changes.ToList() switch
        {
            [] => throw new InvalidOperationException("Expected exactly one change but found none."),
            [var single] => single,
            var list => throw new InvalidOperationException($"Expected exactly one change but found {list.Count}.")
        };
    }

    /// <summary>
    /// Asserts that all changes have the specified <see cref="ChangeKind"/>.
    /// </summary>
    /// <param name="changes">The collection of changes.</param>
    /// <param name="expectedKind">The expected change kind.</param>
    /// <exception cref="ArgumentNullException"><paramref name="changes"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expectedKind"/> is not a valid value.</exception>
    public static void AllHaveKind(this IEnumerable<JsonChange> changes, ChangeKind expectedKind)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var list = changes.ToList();
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Expected changes but found none.");
        }

        if (list.Any(change => change.Kind != expectedKind))
        {
            throw new InvalidOperationException(
                $"Expected all changes to have kind '{expectedKind}' but found mismatches.");
        }
    }

    /// <summary>
    /// Gets the value at the specified path from the original JSON document.
    /// </summary>
    /// <param name="json">The JSON document to parse.</param>
    /// <param name="path">The JSON path to extract (e.g., "/user/name").</param>
    /// <returns>The value at the specified path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null or empty.</exception>
    /// <exception cref="FormatException">The JSON is malformed or the path is invalid.</exception>
    public static object GetValueAtPath(this string json, string path)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Parse the JSON document
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Navigate to the path
        var segments = path.Split('/');
        var current = root;

        foreach (var segment in segments.Skip(1))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            // Handle escaped characters (~1 for /)
            var unescaped = segment.Replace("~1", "/");

            if (current.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (!current.TryGetProperty(unescaped, out var property))
                {
                    throw new InvalidOperationException($"Property '{unescaped}' not found at path '{path}'");
                }

                current = property;
            }
            else if (current.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                if (!int.TryParse(unescaped, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index < 0)
                {
                    throw new InvalidOperationException($"Invalid array index '{unescaped}' at path '{path}'");
                }

                if (index >= current.GetArrayLength())
                {
                    throw new InvalidOperationException($"Array index {index} out of range at path '{path}'");
                }

                current = current[index];
            }
            else
            {
                throw new InvalidOperationException($"Cannot navigate into non-object/array value at path '{path}'");
            }
        }

        return current.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => current.GetString(),
            System.Text.Json.JsonValueKind.Number => current.GetDecimal(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            _ => current.ToString()
        };
    }
}