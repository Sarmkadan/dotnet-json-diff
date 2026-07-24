using System.IO;
using System.Text.Json;

namespace JsonDiff;

/// <summary>
/// Formats a list of <see cref="JsonChange"/> objects into a JSON Patch document (RFC 6902).
/// </summary>
public static class JsonPatchFormatter
{
    /// <summary>
    /// Renders the changes as a JSON Patch string.
    /// </summary>
    /// <param name="changes">The list of changes to format.</param>
    /// <returns>A JSON string representing the patch operations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="changes"/> is <c>null</c>.</exception>
    public static string ToJsonPatch(IReadOnlyList<JsonChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

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
}
