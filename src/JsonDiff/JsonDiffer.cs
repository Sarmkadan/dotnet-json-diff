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

    /// <summary>
    /// Determines whether two JSON strings are deeply equal (semantically equivalent).
    /// Returns <c>true</c> if the documents are identical or differ only in ways that are considered equal
    /// according to the provided options (e.g., numeric tolerance, property case sensitivity).
    /// This is a short-circuiting operation that returns at the first difference found.
    /// </summary>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    public static bool DeepEquals(string left, string right, DiffOptions? options = null)
    {
        using var l = JsonDocument.Parse(left);
        using var r = JsonDocument.Parse(right);
        return DeepEquals(l.RootElement, r.RootElement, options);
    }

    /// <summary>
    /// Determines whether two already-parsed <see cref="JsonElement"/> values are deeply equal.
    /// Returns <c>true</c> if the elements are identical or differ only in ways that are considered equal
    /// according to the provided options (e.g., numeric tolerance, property case sensitivity).
    /// This is a short-circuiting operation that returns at the first difference found.
    /// </summary>
    public static bool DeepEquals(JsonElement left, JsonElement right, DiffOptions? options = null)
    {
        options ??= DiffOptions.Default;
        return WalkAndCompare("/", left, right, options, 0);
    }

    private static bool WalkAndCompare(string path, JsonElement left, JsonElement right, DiffOptions opt, int depth)
    {
        // Different JSON kinds at the same path -> not equal
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        // Check if we've exceeded max depth
        if (opt.MaxDepth.HasValue && depth >= opt.MaxDepth.Value)
        {
            // Compare subtrees with raw text equality - if different, they're not equal
            return string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                return CompareObjects(path, left, right, opt, depth);
            case JsonValueKind.Array:
                return CompareArrays(path, left, right, opt, depth);
            default:
                return ScalarEquals(left, right, opt);
        }
    }

    private static bool CompareObjects(string path, JsonElement left, JsonElement right, DiffOptions opt, int depth)
    {
        var comparer = opt.IgnorePropertyCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var rightProps = new Dictionary<string, JsonElement>(comparer);
        foreach (var p in right.EnumerateObject())
        {
            rightProps[p.Name] = p.Value;
        }

        var seen = new HashSet<string>(comparer);

        foreach (var p in left.EnumerateObject())
        {
            seen.Add(p.Name);
            var childPath = Join(path, p.Name);
            if (rightProps.TryGetValue(p.Name, out var rv))
            {
                if (!WalkAndCompare(childPath, p.Value, rv, opt, depth + 1))
                {
                    return false;
                }
            }
            else
            {
                // Property exists in left but not in right
                return false;
            }
        }

        foreach (var p in right.EnumerateObject())
        {
            if (!seen.Contains(p.Name))
            {
                // Property exists in right but not in left
                return false;
            }
        }

        return true;
    }

    private static bool CompareArrays(string path, JsonElement left, JsonElement right, DiffOptions opt, int depth)
    {
        var l = left.EnumerateArray().ToArray();
        var r = right.EnumerateArray().ToArray();

        if (l.Length != r.Length)
        {
            return false;
        }

        for (var i = 0; i < l.Length; i++)
        {
            var childPath = Join(path, i.ToString());
            if (!WalkAndCompare(childPath, l[i], r[i], opt, depth + 1))
            {
                return false;
            }
        }

        return true;
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

    // Handle array shift detection when enabled and arrays have different lengths
    if (opt.DetectArrayShifts && l.Length != r.Length)
    {
        HandleArrayShifts(path, l, r, opt, sink, depth);
        return;
    }

    var common = Math.Min(l.Length, r.Length);

    for (var i = 0; i < common; i++)
        Walk(Join(path, i.ToString()), l[i], r[i], opt, sink, depth + 1);

    for (var i = common; i < l.Length; i++)
        sink.Add(new JsonChange(ChangeKind.Removed, Join(path, i.ToString()), l[i].Clone(), null));

    for (var i = common; i < r.Length; i++)
        sink.Add(new JsonChange(ChangeKind.Added, Join(path, i.ToString()), null, r[i].Clone()));
}

private static void HandleArrayShifts(string path, JsonElement[] l, JsonElement[] r, DiffOptions opt, List<JsonChange> sink, int depth)
{
    var shorter = l.Length < r.Length ? l : r;
    var longer = l.Length < r.Length ? r : l;
    var isAdded = l.Length < r.Length; // true if right is longer (added elements), false if left is longer (removed elements)

    // Check if shorter array is a prefix of longer array
    bool isPrefix = true;
    for (int i = 0; i < shorter.Length; i++)
    {
        if (!ElementsEqual(shorter[i], longer[i], opt))
        {
            isPrefix = false;
            break;
        }
    }

    if (isPrefix)
    {
        // All elements in shorter array match prefix of longer array
        // Report only the extra elements at the end
        var startIndex = shorter.Length;
        for (var i = startIndex; i < longer.Length; i++)
        {
            var elementPath = Join(path, i.ToString());
            if (isAdded)
                sink.Add(new JsonChange(ChangeKind.Added, elementPath, null, longer[i].Clone()));
            else
                sink.Add(new JsonChange(ChangeKind.Removed, elementPath, longer[i].Clone(), null));
        }
        return;
    }

    // Check if shorter array is a suffix of longer array
    bool isSuffix = true;
    var offset = longer.Length - shorter.Length;
    for (int i = 0; i < shorter.Length; i++)
    {
        if (!ElementsEqual(shorter[i], longer[offset + i], opt))
        {
            isSuffix = false;
            break;
        }
    }

    if (isSuffix)
    {
        // All elements in shorter array match suffix of longer array
        // Report only the extra elements at the beginning
        for (var i = 0; i < offset; i++)
        {
            var elementPath = Join(path, i.ToString());
            if (isAdded)
                sink.Add(new JsonChange(ChangeKind.Added, elementPath, null, longer[i].Clone()));
            else
                sink.Add(new JsonChange(ChangeKind.Removed, elementPath, longer[i].Clone(), null));
        }
        return;
    }

    // Not a simple shift - fall back to index-by-index comparison
    var common = Math.Min(l.Length, r.Length);
    for (var i = 0; i < common; i++)
        Walk(Join(path, i.ToString()), l[i], r[i], opt, sink, depth + 1);

    for (var i = common; i < l.Length; i++)
        sink.Add(new JsonChange(ChangeKind.Removed, Join(path, i.ToString()), l[i].Clone(), null));

    for (var i = common; i < r.Length; i++)
        sink.Add(new JsonChange(ChangeKind.Added, Join(path, i.ToString()), null, r[i].Clone()));
}

private static bool ElementsEqual(JsonElement a, JsonElement b, DiffOptions opt)
{
    if (a.ValueKind != b.ValueKind)
        return false;
    
    switch (a.ValueKind)
    {
        case JsonValueKind.Object:
            // For shift detection, use raw text equality to compare entire objects/arrays
            return string.Equals(a.GetRawText(), b.GetRawText(), StringComparison.Ordinal);
        case JsonValueKind.Array:
            // For shift detection, use raw text equality to compare entire arrays
            return string.Equals(a.GetRawText(), b.GetRawText(), StringComparison.Ordinal);
        case JsonValueKind.String:
            return string.Equals(a.GetString(), b.GetString(), StringComparison.Ordinal);
        case JsonValueKind.Number:
            if (opt.NumericTolerance
                && a.TryGetDouble(out var ad) 
                && b.TryGetDouble(out var bd))
                return ad.Equals(bd);
            return string.Equals(a.GetRawText(), b.GetRawText(), StringComparison.Ordinal);
        case JsonValueKind.True:
        case JsonValueKind.False:
            return true; // Same ValueKind already guaranteed
        case JsonValueKind.Null:
            return true; // Same ValueKind already guaranteed
        default:
            return string.Equals(a.GetRawText(), b.GetRawText(), StringComparison.Ordinal);
    }
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