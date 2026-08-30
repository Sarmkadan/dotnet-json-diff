using System.Text.Json;

namespace JsonDiff;

/// <summary>
/// Extension methods for <see cref="IReadOnlyList{JsonChange}"/> to simplify working with collections of changes.
/// </summary>
public static class JsonChangeExtensions
{
    /// <summary>
    /// Determines whether the collection contains any changes.
    /// </summary>
    /// <param name="changes">The collection of changes to check.</param>
    /// <returns><c>true</c> if there are any changes; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">changes is null.</exception>
    public static bool HasChanges(this IReadOnlyList<JsonChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        return changes.Count > 0;
    }

    /// <summary>
    /// Filters the collection to only include changes of the specified kind.
    /// </summary>
    /// <param name="changes">The collection of changes to filter.</param>
    /// <param name="kind">The change kind to filter by.</param>
    /// <returns>An enumerable containing only changes of the specified kind.</returns>
    /// <exception cref="ArgumentNullException">changes is null.</exception>
    public static IEnumerable<JsonChange> OfKind(this IReadOnlyList<JsonChange> changes, ChangeKind kind)
    {
        ArgumentNullException.ThrowIfNull(changes);
        foreach (var change in changes)
        {
            if (change.Kind == kind)
            {
                yield return change;
            }
        }
    }

    /// <summary>
    /// Filters the collection to only include changes whose path starts with the specified pointer prefix.
    /// The prefix match is done on path segments, not raw string prefix.
    /// For example, "/a" will match "/a/b" but not "/ab".
    /// </summary>
    /// <param name="changes">The collection of changes to filter.</param>
    /// <param name="pointerPrefix">The JSON Pointer prefix to match (e.g., "/user/roles").</param>
    /// <returns>An enumerable containing only changes under the specified path prefix.</returns>
    /// <exception cref="ArgumentNullException">changes is null.</exception>
    public static IEnumerable<JsonChange> UnderPath(this IReadOnlyList<JsonChange> changes, string pointerPrefix)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if (string.IsNullOrEmpty(pointerPrefix))
        {
            yield break;
        }

        foreach (var change in changes)
        {
            // Handle root path
            if (pointerPrefix == "/")
            {
                yield return change;
                continue;
            }

            // Check for exact match first
            if (change.Path == pointerPrefix)
            {
                yield return change;
                continue;
            }

            // Normalize pointerPrefix to always end with '/' for prefix matching
            var normalizedPrefix = pointerPrefix.EndsWith('/')
                ? pointerPrefix
                : pointerPrefix + '/';

            // Check if the path starts with the normalized prefix (prefix match)
            if (change.Path.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            {
                yield return change;
            }
        }
    }

    /// <summary>
    /// Generates a summary string representation of all changes in the collection.
    /// Each change is rendered on a separate line in the format: "changed {path}: {left} -> {right}".
    /// </summary>
    /// <param name="changes">The collection of changes to summarize.</param>
    /// <returns>A multi-line string with one change per line.</returns>
    /// <exception cref="ArgumentNullException">changes is null.</exception>
    public static string ToSummaryString(this IReadOnlyList<JsonChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if (changes.Count == 0)
        {
            return string.Empty;
        }

        using var writer = new StringWriter();
        foreach (var change in changes)
        {
            writer.WriteLine(change.ToString());
        }

        return writer.ToString().TrimEnd();
    }
}
