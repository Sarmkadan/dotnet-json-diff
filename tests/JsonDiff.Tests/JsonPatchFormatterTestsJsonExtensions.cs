using System;
using System.Text.Json;

namespace JsonDiff.Tests
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="JsonPatchFormatterTests"/>.
    /// </summary>
    public static class JsonPatchFormatterTestsJsonExtensions
    {
        /// <summary>
        /// Private static JSON serializer options configured for camelCase property naming.
        /// </summary>
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the <see cref="JsonPatchFormatterTests"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this JsonPatchFormatterTests value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_options)
                {
                    WriteIndented = true
                }
                : _options;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="JsonPatchFormatterTests"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized <see cref="JsonPatchFormatterTests"/> instance, or null if the JSON is empty.</returns>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static JsonPatchFormatterTests? FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<JsonPatchFormatterTests>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="JsonPatchFormatterTests"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized value if successful.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        public static bool TryFromJson(string json, out JsonPatchFormatterTests? value)
        {
            value = null;

            if (string.IsNullOrEmpty(json))
            {
                return true;
            }

            try
            {
                value = JsonSerializer.Deserialize<JsonPatchFormatterTests>(json, _options);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}