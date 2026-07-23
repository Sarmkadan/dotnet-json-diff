using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace JsonDiff;

/// <summary>
/// Provides extension and helper overloads for <see cref="JsonDiffer"/> so every common
/// input form (string, UTF-8 bytes, <see cref="Stream"/>, <see cref="JsonNode"/>,
/// <see cref="JsonElement"/>) can be diffed without manual parsing.
/// </summary>
public static class JsonDifferExtensions
{
    /// <summary>
    /// Diffs two JSON strings.
    /// </summary>
    /// <param name="left">The left JSON string to compare.</param>
    /// <param name="right">The right JSON string to compare.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns>A list of changes between the two JSON documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    public static IReadOnlyList<JsonChange> Diff(this string left, string right, DiffOptions? options = null)
        => JsonDiffer.Diff(left, right, options);

    /// <summary>
    /// Diffs two JSON documents represented as UTF-8 bytes, e.g. raw HTTP response bodies,
    /// without an intermediate string allocation.
    /// </summary>
    /// <param name="leftUtf8">The left JSON as UTF-8 bytes.</param>
    /// <param name="rightUtf8">The right JSON as UTF-8 bytes.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns>A list of changes between the two JSON documents.</returns>
    /// <exception cref="JsonException">Either input is empty or not valid JSON.</exception>
    public static IReadOnlyList<JsonChange> Diff(ReadOnlySpan<byte> leftUtf8, ReadOnlySpan<byte> rightUtf8, DiffOptions? options = null)
    {
        var lReader = new Utf8JsonReader(leftUtf8);
        var rReader = new Utf8JsonReader(rightUtf8);
        using var l = JsonDocument.ParseValue(ref lReader);
        using var r = JsonDocument.ParseValue(ref rReader);
        return JsonDiffer.Diff(l.RootElement, r.RootElement, options);
    }

    /// <summary>
    /// Diffs two JSON documents read from streams.
    /// </summary>
    /// <param name="leftStream">Stream containing the left JSON.</param>
    /// <param name="rightStream">Stream containing the right JSON.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns>A list of changes between the two JSON documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leftStream"/> or <paramref name="rightStream"/> is null.</exception>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    /// <exception cref="IOException">Error reading from stream.</exception>
    public static IReadOnlyList<JsonChange> Diff(Stream leftStream, Stream rightStream, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(leftStream);
        ArgumentNullException.ThrowIfNull(rightStream);

        using var l = JsonDocument.Parse(leftStream);
        using var r = JsonDocument.Parse(rightStream);
        return JsonDiffer.Diff(l.RootElement, r.RootElement, options);
    }

    /// <summary>
    /// Asynchronously diffs two JSON documents read from streams, e.g. HTTP response bodies,
    /// without blocking on I/O or requiring an intermediate string allocation.
    /// </summary>
    /// <param name="leftStream">Stream containing the left JSON.</param>
    /// <param name="rightStream">Stream containing the right JSON.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous read/parse operations.</param>
    /// <returns>A task producing a list of changes between the two JSON documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leftStream"/> or <paramref name="rightStream"/> is null.</exception>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    /// <exception cref="IOException">Error reading from stream.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static async Task<IReadOnlyList<JsonChange>> DiffAsync(Stream leftStream, Stream rightStream, DiffOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leftStream);
        ArgumentNullException.ThrowIfNull(rightStream);

        using var l = await JsonDocument.ParseAsync(leftStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        using var r = await JsonDocument.ParseAsync(rightStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return JsonDiffer.Diff(l.RootElement, r.RootElement, options);
    }

    /// <summary>
    /// Diffs two JSON documents represented as <see cref="JsonNode"/> objects.
    /// </summary>
    /// <param name="left">The left JSON node to compare.</param>
    /// <param name="right">The right JSON node to compare.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns>A list of changes between the two JSON documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
    public static IReadOnlyList<JsonChange> Diff(JsonNode? left, JsonNode? right, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        using var lDoc = JsonDocument.Parse(left.ToJsonString());
        using var rDoc = JsonDocument.Parse(right.ToJsonString());
        return JsonDiffer.Diff(lDoc.RootElement, rDoc.RootElement, options);
    }

    /// <summary>
    /// Diffs a JSON string against a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="leftJson">The left JSON string.</param>
    /// <param name="right">The right <see cref="JsonElement"/>.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns>A list of changes between the two JSON documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leftJson"/> is null.</exception>
    /// <exception cref="JsonException"><paramref name="leftJson"/> is not valid JSON.</exception>
    public static IReadOnlyList<JsonChange> Diff(this string leftJson, JsonElement right, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(leftJson);

        using var l = JsonDocument.Parse(leftJson);
        return JsonDiffer.Diff(l.RootElement, right, options);
    }

    /// <summary>
    /// Diffs a <see cref="JsonElement"/> against a JSON string.
    /// </summary>
    /// <param name="left">The left <see cref="JsonElement"/>.</param>
    /// <param name="rightJson">The right JSON string.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns>A list of changes between the two JSON documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rightJson"/> is null.</exception>
    /// <exception cref="JsonException"><paramref name="rightJson"/> is not valid JSON.</exception>
    public static IReadOnlyList<JsonChange> Diff(JsonElement left, string rightJson, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rightJson);

        using var r = JsonDocument.Parse(rightJson);
        return JsonDiffer.Diff(left, r.RootElement, options);
    }

    /// <summary>
    /// Determines whether two JSON strings are deeply equal (semantically equivalent).
    /// </summary>
    /// <param name="left">The left JSON string to compare.</param>
    /// <param name="right">The right JSON string to compare.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns><c>true</c> if the documents are semantically equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    public static bool DeepEquals(this string left, string right, DiffOptions? options = null)
        => JsonDiffer.DeepEquals(left, right, options);

    /// <summary>
    /// Determines whether two JSON documents represented as UTF-8 bytes are deeply equal.
    /// </summary>
    /// <param name="leftUtf8">The left JSON as UTF-8 bytes.</param>
    /// <param name="rightUtf8">The right JSON as UTF-8 bytes.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns><c>true</c> if the documents are semantically equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="JsonException">Either input is empty or not valid JSON.</exception>
    public static bool DeepEquals(ReadOnlySpan<byte> leftUtf8, ReadOnlySpan<byte> rightUtf8, DiffOptions? options = null)
    {
        var lReader = new Utf8JsonReader(leftUtf8);
        var rReader = new Utf8JsonReader(rightUtf8);
        using var l = JsonDocument.ParseValue(ref lReader);
        using var r = JsonDocument.ParseValue(ref rReader);
        return JsonDiffer.DeepEquals(l.RootElement, r.RootElement, options);
    }

    /// <summary>
    /// Determines whether two JSON documents read from streams are deeply equal.
    /// </summary>
    /// <param name="leftStream">Stream containing the left JSON.</param>
    /// <param name="rightStream">Stream containing the right JSON.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns><c>true</c> if the documents are semantically equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leftStream"/> or <paramref name="rightStream"/> is null.</exception>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    /// <exception cref="IOException">Error reading from stream.</exception>
    public static bool DeepEquals(Stream leftStream, Stream rightStream, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(leftStream);
        ArgumentNullException.ThrowIfNull(rightStream);

        using var l = JsonDocument.Parse(leftStream);
        using var r = JsonDocument.Parse(rightStream);
        return JsonDiffer.DeepEquals(l.RootElement, r.RootElement, options);
    }

    /// <summary>
    /// Asynchronously determines whether two JSON documents read from streams, e.g. HTTP response
    /// bodies, are deeply equal, without blocking on I/O or requiring an intermediate string allocation.
    /// </summary>
    /// <param name="leftStream">Stream containing the left JSON.</param>
    /// <param name="rightStream">Stream containing the right JSON.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous read/parse operations.</param>
    /// <returns>A task producing <c>true</c> if the documents are semantically equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leftStream"/> or <paramref name="rightStream"/> is null.</exception>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    /// <exception cref="IOException">Error reading from stream.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public static async Task<bool> DeepEqualsAsync(Stream leftStream, Stream rightStream, DiffOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(leftStream);
        ArgumentNullException.ThrowIfNull(rightStream);

        using var l = await JsonDocument.ParseAsync(leftStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        using var r = await JsonDocument.ParseAsync(rightStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return JsonDiffer.DeepEquals(l.RootElement, r.RootElement, options);
    }

    /// <summary>
    /// Determines whether two <see cref="JsonNode"/> objects are deeply equal.
    /// </summary>
    /// <param name="left">The left JSON node to compare.</param>
    /// <param name="right">The right JSON node to compare.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns><c>true</c> if the nodes are semantically equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
    public static bool DeepEquals(JsonNode? left, JsonNode? right, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        using var lDoc = JsonDocument.Parse(left.ToJsonString());
        using var rDoc = JsonDocument.Parse(right.ToJsonString());
        return JsonDiffer.DeepEquals(lDoc.RootElement, rDoc.RootElement, options);
    }

    /// <summary>
    /// Determines whether a JSON string and a <see cref="JsonElement"/> are deeply equal.
    /// </summary>
    /// <param name="leftJson">The left JSON string.</param>
    /// <param name="right">The right <see cref="JsonElement"/>.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns><c>true</c> if the documents are semantically equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="leftJson"/> is null.</exception>
    /// <exception cref="JsonException"><paramref name="leftJson"/> is not valid JSON.</exception>
    public static bool DeepEquals(this string leftJson, JsonElement right, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(leftJson);

        using var l = JsonDocument.Parse(leftJson);
        return JsonDiffer.DeepEquals(l.RootElement, right, options);
    }

    /// <summary>
    /// Determines whether a <see cref="JsonElement"/> and a JSON string are deeply equal.
    /// </summary>
    /// <param name="left">The left <see cref="JsonElement"/>.</param>
    /// <param name="rightJson">The right JSON string.</param>
    /// <param name="options">Optional diff options. Uses <see cref="DiffOptions.Default"/> if null.</param>
    /// <returns><c>true</c> if the documents are semantically equal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rightJson"/> is null.</exception>
    /// <exception cref="JsonException"><paramref name="rightJson"/> is not valid JSON.</exception>
    public static bool DeepEquals(JsonElement left, string rightJson, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rightJson);

        using var r = JsonDocument.Parse(rightJson);
        return JsonDiffer.DeepEquals(left, r.RootElement, options);
    }
}
