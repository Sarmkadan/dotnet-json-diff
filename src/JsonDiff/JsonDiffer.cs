using System.Text.Json;

namespace JsonDiff;

/// <summary>
/// Computes a semantic diff between two JSON documents. Object property order is
/// irrelevant; only structural and value differences are reported.
/// <para>
/// Duplicate object keys (which JSON permits and <see cref="JsonElement.EnumerateObject"/>
/// surfaces verbatim) are resolved with a last-wins policy, matching the behavior of
/// <see cref="JsonSerializer"/> when deserializing into a dictionary: for each key,
/// only the value of its last occurrence in the object participates in the comparison.
/// This applies to both sides of the diff, so <c>{"a":1,"a":2}</c> and <c>{"a":2}</c>
/// are considered equal.
/// </para>
/// </summary>
public static class JsonDiffer
{
    /// <summary>
    /// Diffs two raw JSON strings.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    public static IReadOnlyList<JsonChange> Diff(string left, string right, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

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
        Walk(JsonPointer.Root, left, right, options, changes, 0);
        return changes;
    }

    /// <summary>
    /// Determines whether two JSON strings are deeply equal (semantically equivalent).
    /// Returns <c>true</c> if the documents are identical or differ only in ways that are considered equal
    /// according to the provided options (e.g., numeric tolerance, property case sensitivity).
    /// This is a short-circuiting operation that returns at the first difference found.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is null.</exception>
    /// <exception cref="JsonException">Either input is not valid JSON.</exception>
    public static bool DeepEquals(string left, string right, DiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

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
        return WalkAndCompare(JsonPointer.Root, left, right, options, 0);
    }

    private static bool WalkAndCompare(JsonPointer path, JsonElement left, JsonElement right, DiffOptions opt, int depth)
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

    private static bool CompareObjects(JsonPointer path, JsonElement left, JsonElement right, DiffOptions opt, int depth)
    {
        var comparer = opt.IgnorePropertyCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        // Last-wins on duplicate keys, matching JsonSerializer's dictionary behavior.
        var leftProps = CollectProperties(left, comparer);
        var rightProps = CollectProperties(right, comparer);

        foreach (var (name, lv) in leftProps)
        {
            var childPath = path.Append(name);
            if (rightProps.TryGetValue(name, out var rv))
            {
                if (!WalkAndCompare(childPath, lv, rv, opt, depth + 1))
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

        foreach (var name in rightProps.Keys)
        {
            if (!leftProps.ContainsKey(name))
            {
                // Property exists in right but not in left
                return false;
            }
        }

        return true;
    }

    private static bool CompareArrays(JsonPointer path, JsonElement left, JsonElement right, DiffOptions opt, int depth)
    {
        var l = left.EnumerateArray().ToArray();
        var r = right.EnumerateArray().ToArray();

        if (l.Length != r.Length)
        {
            return false;
        }

        for (var i = 0; i < l.Length; i++)
        {
            var childPath = path.Append(i.ToString());
            if (!WalkAndCompare(childPath, l[i], r[i], opt, depth + 1))
            {
                return false;
            }
        }

        return true;
    }

    private static void Walk(JsonPointer path, JsonElement left, JsonElement right, DiffOptions opt, List<JsonChange> sink, int depth)
    {
        // Different JSON kinds at the same path -> a changed value, no descent.
        if (left.ValueKind != right.ValueKind)
        {
            sink.Add(new JsonChange(ChangeKind.Changed, path.ToPointerString(), left.Clone(), right.Clone()));
            return;
        }

        // Check if we've exceeded max depth
        if (opt.MaxDepth.HasValue && depth >= opt.MaxDepth.Value)
        {
            // Compare subtrees with raw text equality
            if (!string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal))
            {
                sink.Add(new JsonChange(ChangeKind.Changed, path.ToPointerString(), left.Clone(), right.Clone()));
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
                sink.Add(new JsonChange(ChangeKind.Changed, path.ToPointerString(), left.Clone(), right.Clone()));
                break;
        }
    }

    private static void WalkObject(JsonPointer path, JsonElement left, JsonElement right, DiffOptions opt, List<JsonChange> sink, int depth)
    {
        var comparer = opt.IgnorePropertyCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        // Last-wins on duplicate keys, matching JsonSerializer's dictionary behavior.
        var leftProps = CollectProperties(left, comparer);
        var rightProps = CollectProperties(right, comparer);

        foreach (var (name, lv) in leftProps)
        {
            var childPath = path.Append(name);
            if (rightProps.TryGetValue(name, out var rv))
                Walk(childPath, lv, rv, opt, sink, depth + 1);
            else
                sink.Add(new JsonChange(ChangeKind.Removed, childPath.ToPointerString(), lv.Clone(), null));
        }

        foreach (var (name, rv) in rightProps)
        {
            if (leftProps.ContainsKey(name))
                continue;
            sink.Add(new JsonChange(ChangeKind.Added, path.Append(name).ToPointerString(), null, rv.Clone()));
        }
    }

    /// <summary>
    /// Flattens an object's properties into a dictionary using a last-wins policy for
    /// duplicate keys: when the same name occurs more than once, the value of the last
    /// occurrence replaces earlier ones, mirroring <see cref="JsonSerializer"/>.
    /// Insertion order of first occurrences is preserved.
    /// </summary>
    private static Dictionary<string, JsonElement> CollectProperties(JsonElement obj, StringComparer comparer)
    {
        var props = new Dictionary<string, JsonElement>(comparer);
        foreach (var p in obj.EnumerateObject())
            props[p.Name] = p.Value;
        return props;
    }

private static void WalkArray(JsonPointer path, JsonElement left, JsonElement right, DiffOptions opt, List<JsonChange> sink, int depth)
{
    var l = left.EnumerateArray().ToArray();
    var r = right.EnumerateArray().ToArray();

    // Handle array shift detection when enabled and arrays have different lengths
    if (opt.DetectArrayShifts && l.Length != r.Length)
    {
        HandleArrayShifts(path, l, r, opt, sink, depth);
        return;
    }

    switch (opt.ArrayComparison)
    {
        case ArrayComparison.Ordered:
            WalkArrayOrdered(path, l, r, opt, sink, depth);
            break;
        case ArrayComparison.Unordered:
            WalkArrayUnordered(path, l, r, opt, sink, depth);
            break;
        case ArrayComparison.KeyedBy:
            WalkArrayKeyed(path, l, r, opt, sink, depth);
            break;
    }
}

private static void WalkArrayOrdered(JsonPointer path, JsonElement[] l, JsonElement[] r, DiffOptions opt, List<JsonChange> sink, int depth)
{
    var common = Math.Min(l.Length, r.Length);

    for (var i = 0; i < common; i++)
        Walk(path.Append(i.ToString()), l[i], r[i], opt, sink, depth + 1);

    for (var i = common; i < l.Length; i++)
        sink.Add(new JsonChange(ChangeKind.Removed, path.Append(i.ToString()).ToPointerString(), l[i].Clone(), null));

    for (var i = common; i < r.Length; i++)
        sink.Add(new JsonChange(ChangeKind.Added, path.Append(i.ToString()).ToPointerString(), null, r[i].Clone()));
}

private static void WalkArrayUnordered(JsonPointer path, JsonElement[] l, JsonElement[] r, DiffOptions opt, List<JsonChange> sink, int depth)
{
    // For unordered comparison, we need to match elements by deep equality
    // We'll use a multiset approach: match elements from left to right, report unmatched as removed/added

    var leftElements = l.ToList();
    var rightElements = r.ToList();

    // First pass: find matching elements (deep equality)
    var matchedLeftIndices = new bool[leftElements.Count];
    var matchedRightIndices = new bool[rightElements.Count];

    for (int i = 0; i < leftElements.Count; i++)
    {
        for (int j = 0; j < rightElements.Count; j++)
        {
            if (!matchedRightIndices[j] && WalkAndCompare(path, leftElements[i], rightElements[j], opt, depth + 1))
            {
                matchedLeftIndices[i] = true;
                matchedRightIndices[j] = true;
                break;
            }
        }
    }

    // Report removed elements (in left but not matched in right)
    for (int i = 0; i < leftElements.Count; i++)
    {
        if (!matchedLeftIndices[i])
        {
            sink.Add(new JsonChange(ChangeKind.Removed, path.Append(i.ToString()).ToPointerString(), leftElements[i].Clone(), null));
        }
    }

    // Report added elements (in right but not matched in left)
    for (int j = 0; j < rightElements.Count; j++)
    {
        if (!matchedRightIndices[j])
        {
            sink.Add(new JsonChange(ChangeKind.Added, path.Append((l.Length + j).ToString()).ToPointerString(), null, rightElements[j].Clone()));
        }
    }
}

private static void WalkArrayKeyed(JsonPointer path, JsonElement[] l, JsonElement[] r, DiffOptions opt, List<JsonChange> sink, int depth)
{
    if (string.IsNullOrEmpty(opt.ArrayKeySelector))
    {
        // Fall back to ordered comparison if no key selector is provided
        WalkArrayOrdered(path, l, r, opt, sink, depth);
        return;
    }

    // Extract key from each element using the JSON-Pointer path
    var leftDict = new Dictionary<string, JsonElement>();
    var rightDict = new Dictionary<string, JsonElement>();

    foreach (var element in l)
    {
        if (TryGetKey(element, opt.ArrayKeySelector, out var key, out var value))
        {
            leftDict[key] = value;
        }
        else
        {
            // If we can't extract a key, treat the whole element as the value
            // This will be reported as removed
            sink.Add(new JsonChange(ChangeKind.Removed, path.ToPointerString(), element.Clone(), null));
        }
    }

    foreach (var element in r)
    {
        if (TryGetKey(element, opt.ArrayKeySelector, out var key, out var value))
        {
            rightDict[key] = value;
        }
        else
        {
            // If we can't extract a key, treat as added
            sink.Add(new JsonChange(ChangeKind.Added, path.ToPointerString(), null, element.Clone()));
        }
    }

    // Compare elements with matching keys
    foreach (var kvp in leftDict)
    {
        if (rightDict.TryGetValue(kvp.Key, out var rightElement))
        {
            var elementPath = path.Append(Array.IndexOf(l, kvp.Value).ToString());
            Walk(elementPath, kvp.Value, rightElement, opt, sink, depth + 1);
            rightDict.Remove(kvp.Key); // Mark as matched
        }
        else
        {
            // Key exists in left but not in right - report as removed
            var elementPath = path.Append(Array.IndexOf(l, kvp.Value).ToString());
            sink.Add(new JsonChange(ChangeKind.Removed, elementPath.ToPointerString(), kvp.Value.Clone(), null));
        }
    }

    // Report remaining elements in right as added
    foreach (var kvp in rightDict)
    {
        var elementPath = path.Append((l.Length + Array.IndexOf(r, kvp.Value)).ToString());
        sink.Add(new JsonChange(ChangeKind.Added, elementPath.ToPointerString(), null, kvp.Value.Clone()));
    }
}

private static bool TryGetKey(JsonElement element, string keySelector, out string key, out JsonElement value)
{
    key = null;
    value = element;

    if (element.ValueKind != JsonValueKind.Object)
    {
        return false;
    }

    // Parse JSON-Pointer path (simplified - only supports simple property names)
    var pathSegments = keySelector.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

    JsonElement current = element;
    foreach (var segment in pathSegments)
    {
        if (current.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!current.TryGetProperty(segment, out var prop))
        {
            return false;
        }

        current = prop;
    }

    // The final value is the key
    if (current.ValueKind == JsonValueKind.String)
    {
        key = current.GetString();
        return true;
    }

    if (current.ValueKind == JsonValueKind.Number)
    {
        key = current.GetRawText();
        return true;
    }

    return false;
}

private static void HandleArrayShifts(JsonPointer path, JsonElement[] l, JsonElement[] r, DiffOptions opt, List<JsonChange> sink, int depth)
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
            var elementPath = path.Append(i.ToString());
            if (isAdded)
                sink.Add(new JsonChange(ChangeKind.Added, elementPath.ToPointerString(), null, longer[i].Clone()));
            else
                sink.Add(new JsonChange(ChangeKind.Removed, elementPath.ToPointerString(), longer[i].Clone(), null));
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
            var elementPath = path.Append(i.ToString());
            if (isAdded)
                sink.Add(new JsonChange(ChangeKind.Added, elementPath.ToPointerString(), null, longer[i].Clone()));
            else
                sink.Add(new JsonChange(ChangeKind.Removed, elementPath.ToPointerString(), longer[i].Clone(), null));
        }
        return;
    }

    // Not a simple shift - fall back to index-by-index comparison
    var common = Math.Min(l.Length, r.Length);
    for (var i = 0; i < common; i++)
        Walk(path.Append(i.ToString()), l[i], r[i], opt, sink, depth + 1);

    for (var i = common; i < l.Length; i++)
        sink.Add(new JsonChange(ChangeKind.Removed, path.Append(i.ToString()).ToPointerString(), l[i].Clone(), null));

    for (var i = common; i < r.Length; i++)
        sink.Add(new JsonChange(ChangeKind.Added, path.Append(i.ToString()).ToPointerString(), null, r[i].Clone()));
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
            return NumbersEqual(a, b, opt);
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
                return NumbersEqual(left, right, opt);
            default:
                return string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Compares two JSON number elements according to the configured number-comparison strategy.
    /// In semantic mode, values are compared as <see cref="decimal"/> when both parse (so
    /// <c>1</c> == <c>1.0</c>, <c>100</c> == <c>1e2</c>, <c>0</c> == <c>-0</c>, and 28+ digit
    /// decimals keep full precision instead of being rounded through <see cref="double"/>).
    /// Values outside the decimal range fall back to <see cref="double"/>, and finally to
    /// ordinal raw-text comparison (e.g. <c>1e400</c> vs <c>1e400</c>).
    /// </summary>
    private static bool NumbersEqual(JsonElement left, JsonElement right, DiffOptions opt)
    {
        var leftRaw = left.GetRawText();
        var rightRaw = right.GetRawText();

        if (!opt.NumericTolerance || opt.NumberComparison == NumberComparison.Exact)
            return string.Equals(leftRaw, rightRaw, StringComparison.Ordinal);

        if (decimal.TryParse(leftRaw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ld)
            && decimal.TryParse(rightRaw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rd))
            return ld == rd;

        if (left.TryGetDouble(out var lf) && right.TryGetDouble(out var rf))
            return lf.Equals(rf);

        return string.Equals(leftRaw, rightRaw, StringComparison.Ordinal);
    }

}