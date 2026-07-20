using System.Text.Json;

namespace JsonDiff;

/// <summary>
/// Computes a semantic diff between two JSON documents. Object property order is
/// irrelevant; only structural and value differences are reported.
/// </summary>
public static class JsonDiffer
{
    /// <summary>
    /// Diffs two raw JSON strings.
    /// </summary>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    public static IReadOnlyList<JsonChange> Diff(string left, string right, DiffOptions? options = null)
    {
        using var l = JsonDocument.Parse(left);
        using var r = JsonDocument.Parse(right);
        return Diff(l.RootElement, r.RootElement, options);
    }

    /// <summary>
    /// Diffs two already-parsed <see cref="JsonElement"/> values.
    /// </summary>
    public static IReadOnlyList<JsonChange> Diff(JsonElement left, JsonElement right, DiffOptions? options = null)
    {
        options ??= DiffOptions.Default;
        var changes = new List<JsonChange>();
        Walk("/", left, right, options, changes, 0);
        return changes;
    }

    private static void Walk(string path, JsonElement left, JsonElement right, DiffOptions opt, List<JsonChange> sink, int depth)
    {
        // Different JSON kinds at the same path -> a changed value, no descent.
        if (left.ValueKind != right.ValueKind)
        {
            sink.Add(new JsonChange(ChangeKind.Changed, path, left.Clone(), right.Clone()));
            return;
        }

        // Check if we've exceeded max depth
        if (opt.MaxDepth.HasValue && depth >= opt.MaxDepth.Value)
        {
            // Compare subtrees with raw text equality
            if (!string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal))
            {
                sink.Add(new JsonChange(ChangeKind.Changed, path, left.Clone(), right.Clone()));
            }
            return;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                WalkObject(path, left, right, opt, sink, depth);
                break;
            case JsonValueKind.Array:
                WalkArray(path, left, right, opt, sink, depth);
                break;
            default:
                if (!ScalarEquals(left, right, opt))
                sink.Add(new JsonChange(ChangeKind.Changed, path, left.Clone(), right.Clone()));
                break;
        }
    }

    private static void WalkObject(string path, JsonElement left, JsonElement right, DiffOptions opt, List<JsonChange> sink, int depth)
    {
        var comparer = opt.IgnorePropertyCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var rightProps = new Dictionary<string, JsonElement>(comparer);
        foreach (var p in right.EnumerateObject())
            rightProps[p.Name] = p.Value;

        var seen = new HashSet<string>(comparer);

        foreach (var p in left.EnumerateObject())
        {
            seen.Add(p.Name);
            var childPath = Join(path, p.Name);
            if (rightProps.TryGetValue(p.Name, out var rv))
                Walk(childPath, p.Value, rv, opt, sink, depth + 1);
            else
                sink.Add(new JsonChange(ChangeKind.Removed, childPath, p.Value.Clone(), null));
        }

        foreach (var p in right.EnumerateObject())
        {
            if (seen.Contains(p.Name))
                continue;
            sink.Add(new JsonChange(ChangeKind.Added, Join(path, p.Name), null, p.Value.Clone()));
        }
    }

    private static void WalkArray(string path, JsonElement left, JsonElement right, DiffOptions opt, List<JsonChange> sink, int depth)
    {
        var l = left.EnumerateArray().ToArray();
        var r = right.EnumerateArray().ToArray();
        var common = Math.Min(l.Length, r.Length);

        for (var i = 0; i < common; i++)
            Walk(Join(path, i.ToString()), l[i], r[i], opt, sink, depth + 1);

        for (var i = common; i < l.Length; i++)
            sink.Add(new JsonChange(ChangeKind.Removed, Join(path, i.ToString()), l[i].Clone(), null));

        for (var i = common; i < r.Length; i++)
            sink.Add(new JsonChange(ChangeKind.Added, Join(path, i.ToString()), null, r[i].Clone()));
    }

    private static bool ScalarEquals(JsonElement left, JsonElement right, DiffOptions opt)
    {
        switch (left.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
                return true; // same ValueKind already guaranteed by caller
            case JsonValueKind.String:
                return string.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal);
            case JsonValueKind.Number:
                if (opt.NumericTolerance
                    && left.TryGetDouble(out var ld)
                    && right.TryGetDouble(out var rd))
                    return ld.Equals(rd);
                return string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);
            default:
                return string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);
        }
    }

    private static string Join(string parent, string segment)
    {
        // Escape per RFC 6901 so segments with '/' or '~' stay unambiguous.
        var escaped = segment.Replace("~", "~0").Replace("/", "~1");
        return parent == "/" ? "/" + escaped : parent + "/" + escaped;
    }
}