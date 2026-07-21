using System.Text.Json;

namespace JsonDiff;

/// <summary>
/// Provides JSON serialization extensions for <see cref="JsonDiffer"/> type metadata.
/// </summary>
public static class JsonDifferJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="JsonDiffer"/> type identifier to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="JsonDiffer"/> type reference (must not be null).</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string containing the type identifier "JsonDiffer".</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this object value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize("JsonDiffer", options);
    }

    /// <summary>
    /// Deserializes a type identifier from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The type identifier "JsonDiffer" if successful, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">Thrown when the JSON does not contain a valid type identifier.</exception>
    public static string? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetString();
    }

    /// <summary>
    /// Attempts to deserialize a type identifier from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized type identifier if successful, otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson(string json, out string? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            value = doc.RootElement.GetString();
            return value == "JsonDiffer";
        }
        catch (JsonException)
        {
            return false;
        }
    }
}