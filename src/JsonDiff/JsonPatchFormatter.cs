using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JsonDiff;

/// <summary>
/// Formats a list of <see cref="JsonChange"/> objects into a JSON Patch document (RFC 6902).
/// </summary>
public class JsonPatchFormatter : IEquatable<JsonPatchFormatter>
{
    /// <summary>
    /// Renders the changes as a JSON Patch string.
    /// </summary>
    /// <param name="changes">The list of changes to format.</param>
    /// <returns>A JSON string representing the patch operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="changes"/> is <c>null</c>.</exception>
    public static string ToJsonPatch(IReadOnlyList<JsonChange> changes) => ToJsonPatch(changes, indented: false);

    /// <summary>
    /// Renders the changes as a JSON Patch string, optionally using indented formatting.
    /// </summary>
    /// <param name="changes">The list of changes to format.</param>
    /// <param name="indented"><c>true</c> to format the JSON with indentation; otherwise, <c>false</c>.</param>
    /// <returns>A JSON string representing the patch operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="changes"/> is <c>null</c>.</exception>
    public static string ToJsonPatch(IReadOnlyList<JsonChange> changes, bool indented)
    {
        ArgumentNullException.ThrowIfNull(changes);

        using var stream = new MemoryStream();
        using var writer = indented
            ? new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })
            : new Utf8JsonWriter(stream);

        writer.WriteStartArray();

        foreach (var change in changes)
        {
            writer.WriteStartObject();

            // Write "op"
            string op = change.Kind switch
            {
                ChangeKind.Added => "add",
                ChangeKind.Removed => "remove",
                _ => "replace" // ChangeKind.Changed
            };
            writer.WriteString("op", op);

            // Write "path"
            writer.WriteString("path", change.Path);

            // Write "value" if applicable (add and replace require value, remove does not)
            if (change.Kind != ChangeKind.Removed)
            {
                writer.WritePropertyName("value");
                if (change.Right.HasValue)
                {
                    change.Right.Value.WriteTo(writer);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    // Equality members – the class has no instance state, so all instances are considered equal.

    public bool Equals(JsonPatchFormatter? other)
    {
        // Since the class has no instance fields, any non‑null instance is equal to another.
        return other is not null;
    }

    public override bool Equals(object? obj) => obj is JsonPatchFormatter other && Equals(other);

    public override int GetHashCode()
    {
        // Use a constant hash code derived from the type; HashCode.Combine works with a single argument.
        return HashCode.Combine(typeof(JsonPatchFormatter));
    }

    public static bool operator ==(JsonPatchFormatter? left, JsonPatchFormatter? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(JsonPatchFormatter? left, JsonPatchFormatter? right) => !(left == right);
}
