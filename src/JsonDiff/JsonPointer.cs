using System.Text;

namespace JsonDiff;

/// <summary>
/// An immutable RFC 6901 JSON Pointer, modeled as a list of unescaped segments.
/// This is the single source of truth for pointer escaping used by both
/// <see cref="JsonDiffer"/> (when anchoring changes to paths) and
/// <see cref="JsonPatchFormatter"/> (when emitting JSON Patch documents),
/// guaranteeing that keys containing <c>/</c>, <c>~</c> or the empty string
/// are always escaped consistently as <c>~1</c>/<c>~0</c>.
/// </summary>
public readonly struct JsonPointer : IEquatable<JsonPointer>
{
    private readonly string[]? _segments;

    private JsonPointer(string[] segments) => _segments = segments;

    /// <summary>The pointer to the whole document (no segments, rendered as an empty string).</summary>
    public static JsonPointer Root => default;

    /// <summary>The unescaped reference tokens of this pointer, in order from the root.</summary>
    public IReadOnlyList<string> Segments => _segments ?? [];

    /// <summary>
    /// Returns a new pointer with <paramref name="segment"/> appended as the last reference token.
    /// </summary>
    /// <param name="segment">The raw (unescaped) segment to append. May be empty; empty-string keys are valid in JSON.</param>
    /// <returns>A new <see cref="JsonPointer"/> one level deeper than this one.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="segment"/> is <c>null</c>.</exception>
    public JsonPointer Append(string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);

        var current = _segments ?? [];
        var next = new string[current.Length + 1];
        current.CopyTo(next, 0);
        next[current.Length] = segment;
        return new JsonPointer(next);
    }

    /// <summary>
    /// Escapes a single reference token per RFC 6901: <c>~</c> becomes <c>~0</c>, then <c>/</c> becomes <c>~1</c>.
    /// </summary>
    /// <param name="segment">The raw segment to escape.</param>
    /// <returns>The escaped reference token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="segment"/> is <c>null</c>.</exception>
    public static string Escape(string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        return segment.Replace("~", "~0").Replace("/", "~1");
    }

    /// <summary>
    /// Unescapes a single reference token per RFC 6901: <c>~1</c> becomes <c>/</c>, then <c>~0</c> becomes <c>~</c>.
    /// </summary>
    /// <param name="token">The escaped reference token.</param>
    /// <returns>The raw segment value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="token"/> contains an invalid escape sequence (a <c>~</c> not followed by <c>0</c> or <c>1</c>).</exception>
    public static string Unescape(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (!token.Contains('~'))
        {
            return token;
        }

        var sb = new StringBuilder(token.Length);
        for (var i = 0; i < token.Length; i++)
        {
            var c = token[i];
            if (c != '~')
            {
                sb.Append(c);
                continue;
            }

            if (i + 1 >= token.Length)
            {
                throw new FormatException($"Invalid RFC 6901 escape at end of token '{token}'.");
            }

            sb.Append(token[++i] switch
            {
                '0' => '~',
                '1' => '/',
                var bad => throw new FormatException($"Invalid RFC 6901 escape '~{bad}' in token '{token}'.")
            });
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses an RFC 6901 pointer string into a <see cref="JsonPointer"/>.
    /// The empty string denotes the root pointer.
    /// </summary>
    /// <param name="pointer">The pointer string, e.g. <c>/a~1b/0</c>.</param>
    /// <returns>The parsed pointer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pointer"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="pointer"/> is non-empty but does not start with <c>/</c>, or contains an invalid escape sequence.</exception>
    public static JsonPointer Parse(string pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);

        if (pointer.Length == 0)
        {
            return Root;
        }

        if (pointer[0] != '/')
        {
            throw new FormatException($"A non-empty JSON Pointer must start with '/': '{pointer}'.");
        }

        var tokens = pointer[1..].Split('/');
        var segments = new string[tokens.Length];
        for (var i = 0; i < tokens.Length; i++)
        {
            segments[i] = Unescape(tokens[i]);
        }

        return new JsonPointer(segments);
    }

    /// <summary>
    /// Renders this pointer as an RFC 6901 string suitable for JSON Patch <c>path</c> values.
    /// The root pointer renders as an empty string.
    /// </summary>
    /// <returns>The escaped pointer string.</returns>
    public string ToPointerString()
    {
        var segments = _segments;
        if (segments is null || segments.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var segment in segments)
        {
            sb.Append('/').Append(Escape(segment));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders this pointer in a human-readable, unambiguous form for diagnostics:
    /// <c>$</c> for the root, dot notation for identifier-like segments and
    /// bracket notation (with quoted keys) for everything else, e.g. <c>$.obj['a/b'][0]</c>.
    /// </summary>
    /// <returns>The display string.</returns>
    public string ToDisplayString()
    {
        var segments = _segments;
        if (segments is null || segments.Length == 0)
        {
            return "$";
        }

        var sb = new StringBuilder("$");
        foreach (var segment in segments)
        {
            if (segment.Length > 0 && ulong.TryParse(segment, out _))
            {
                sb.Append('[').Append(segment).Append(']');
            }
            else if (IsIdentifierLike(segment))
            {
                sb.Append('.').Append(segment);
            }
            else
            {
                sb.Append("['").Append(segment.Replace("\\", "\\\\").Replace("'", "\\'")).Append("']");
            }
        }

        return sb.ToString();
    }

    /// <summary>Returns the RFC 6901 pointer string; equivalent to <see cref="ToPointerString"/>.</summary>
    public override string ToString() => ToPointerString();

    /// <inheritdoc />
    public bool Equals(JsonPointer other)
    {
        var a = _segments ?? [];
        var b = other._segments ?? [];
        if (a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is JsonPointer other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var segment in _segments ?? [])
        {
            hash.Add(segment, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    /// <summary>Value equality over segments.</summary>
    public static bool operator ==(JsonPointer left, JsonPointer right) => left.Equals(right);

    /// <summary>Value inequality over segments.</summary>
    public static bool operator !=(JsonPointer left, JsonPointer right) => !left.Equals(right);

    private static bool IsIdentifierLike(string segment)
    {
        if (segment.Length == 0 || char.IsAsciiDigit(segment[0]))
        {
            return false;
        }

        foreach (var c in segment)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }
}
