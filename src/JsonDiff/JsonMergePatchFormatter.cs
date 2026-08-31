using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace JsonDiff;

/// <summary>
/// Formats JSON differences as an RFC 7386 JSON Merge Patch document.
/// JSON Merge Patch replaces arrays as whole values and cannot represent edits to
/// individual array elements.
/// </summary>
public static class JsonMergePatchFormatter
{
    /// <summary>
    /// Renders the supplied changes as an RFC 7386 JSON Merge Patch document.
    /// Added and changed values are written from <see cref="JsonChange.Right"/>,
    /// while removed values are represented by JSON <see langword="null"/>.
    /// </summary>
    /// <param name="changes">The changes to format.</param>
    /// <returns>A JSON string containing the merge-patch document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="changes"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when a path contains an array index. RFC 7386 can replace an entire
    /// array, but it cannot express an edit to an individual array element.
    /// </exception>
    public static string ToJsonMergePatch(IReadOnlyList<JsonChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var root = new PatchNode();
        foreach (var change in changes)
        {
            var segments = ParsePath(change.Path);
            var node = root;

            foreach (var segment in segments)
            {
                if (IsArrayIndex(segment))
                {
                    throw new NotSupportedException(
                        $"JSON Merge Patch cannot express an edit to array element '{change.Path}'.");
                }

                node.HasValue = false;
                node = node.GetOrAdd(segment);
            }

            node.Children.Clear();
            node.HasValue = true;
            node.Value = change.Kind switch
            {
                ChangeKind.Added or ChangeKind.Changed => change.Right,
                ChangeKind.Removed => null,
                _ => throw new NotSupportedException($"Change kind '{change.Kind}' is not supported by JSON Merge Patch.")
            };
        }

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        WriteNode(writer, root);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static IReadOnlyList<string> ParsePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Length == 0)
        {
            return Array.Empty<string>();
        }

        if (path[0] != '/')
        {
            throw new FormatException($"A non-empty JSON Pointer must start with '/': '{path}'.");
        }

        var tokens = path[1..].Split('/');
        for (var i = 0; i < tokens.Length; i++)
        {
            tokens[i] = JsonPointer.Unescape(tokens[i]);
        }

        return tokens;
    }

    private static bool IsArrayIndex(string segment)
    {
        if (segment == "-")
        {
            return true;
        }

        if (segment == "0")
        {
            return true;
        }

        if (segment.Length == 0 || segment[0] < '1' || segment[0] > '9')
        {
            return false;
        }

        for (var i = 1; i < segment.Length; i++)
        {
            if (segment[i] < '0' || segment[i] > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteNode(Utf8JsonWriter writer, PatchNode node)
    {
        if (node.HasValue)
        {
            if (node.Value.HasValue)
            {
                node.Value.Value.WriteTo(writer);
            }
            else
            {
                writer.WriteNullValue();
            }

            return;
        }

        writer.WriteStartObject();
        foreach (var child in node.Children)
        {
            writer.WritePropertyName(child.Key);
            WriteNode(writer, child.Value);
        }

        writer.WriteEndObject();
    }

    private sealed class PatchNode
    {
        public Dictionary<string, PatchNode> Children { get; } = new(StringComparer.Ordinal);

        public bool HasValue { get; set; }

        public JsonElement? Value { get; set; }

        public PatchNode GetOrAdd(string propertyName)
        {
            if (!Children.TryGetValue(propertyName, out var child))
            {
                child = new PatchNode();
                Children.Add(propertyName, child);
            }

            return child;
        }
    }
}
