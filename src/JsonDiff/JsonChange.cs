using System.Text.Json;

namespace JsonDiff;

/// <summary>
/// A single structural difference between two JSON documents, anchored to a path.
/// </summary>
/// <param name="Kind">Whether the path was added, removed or changed.</param>
/// <param name="Path">
/// JSON-Pointer-ish path to the node, e.g. <c>/user/roles/0</c>. The root is <c>/</c>.
/// </param>
/// <param name="Left">The value on the left document, or <c>null</c> for <see cref="ChangeKind.Added"/>.</param>
/// <param name="Right">The value on the right document, or <c>null</c> for <see cref="ChangeKind.Removed"/>.</param>
/// <param name="IsTruncated">
/// Indicates whether this change is part of a truncated diff result due to hitting a limit
/// (e.g., maximum depth or maximum number of changes).
/// </param>
public readonly record struct JsonChange(
    ChangeKind Kind,
    string Path,
    JsonElement? Left,
    JsonElement? Right,
    bool IsTruncated = false)
{
    /// <summary>Renders the change in a compact <c>+/-/~ path: value</c> form.</summary>
    public override string ToString() => Kind switch
    {
        ChangeKind.Added => $"+ {Path}: {Render(Right)}",
        ChangeKind.Removed => $"- {Path}: {Render(Left)}",
        ChangeKind.Moved => $"→ {Path}: {Render(Right)}",
        _ => $"~ {Path}: {Render(Left)} -> {Render(Right)}"
    };

    private static string Render(JsonElement? element)
        => element is { } e ? e.GetRawText() : "null";
}
