namespace JsonDiff;

/// <summary>
/// Controls how arrays are compared during diff operations.
/// </summary>
public enum ArrayComparison
{
    /// <summary>
    /// Arrays are compared by their index positions (default behavior).
    /// This maintains backward compatibility with existing code.
    /// </summary>
    Ordered,

    /// <summary>
    /// Arrays are compared as multisets (unordered collections).
    /// Elements are matched by deep equality, and only genuine additions/removals are reported.
    /// Order differences are ignored.
    /// </summary>
    Unordered,

    /// <summary>
    /// Arrays are compared by matching elements using a key selector path.
    /// Elements with matching keys are compared by their content, and only differences
    /// in matched elements are reported. This treats moved elements as changes rather than
    /// Remove+Add pairs.
    /// </summary>
    KeyedBy
}

/// <summary>
/// Tunables for a diff run.
/// </summary>
public sealed class DiffOptions
{
    /// <summary>
    /// When <c>true</c>, numbers that are numerically equal but written differently
    /// (e.g. <c>1</c> vs <c>1.0</c> vs <c>1e0</c>) are treated as equal.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool NumericTolerance { get; init; } = true;

    /// <summary>
    /// When <c>true</c>, object property names are compared case-insensitively.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IgnorePropertyCase { get; init; }

    /// <summary>
    /// Maximum depth to traverse when diffing nested objects/arrays.
    /// When <c>null</c>, no limit is applied (unlimited depth).
    /// Defaults to <c>null</c>.
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// When <c>true</c>, arrays that differ in length are checked for shift patterns.
    /// If the shorter array matches a prefix or suffix of the longer array, only the extra
    /// elements are reported as Added/Removed at the correct indices.
    /// Defaults to <c>false</c> to preserve existing behavior.
    /// </summary>
    public bool DetectArrayShifts { get; init; }

    /// <summary>
    /// Controls how arrays are compared during diff operations.
    /// Defaults to <see cref="ArrayComparison.Ordered"/> for backward compatibility.
    /// </summary>
    public ArrayComparison ArrayComparison { get; init; } = ArrayComparison.Ordered;

    /// <summary>
    /// JSON-Pointer path to a property to use as a key when <see cref="ArrayComparison.KeyedBy"/> is enabled.
    /// For example, "/id" would match array elements by their "id" property.
    /// Defaults to <c>null</c>.
    /// </summary>
    public string? ArrayKeySelector { get; init; }

    /// <summary>
    /// Maximum array size (number of elements) for which array shift detection will be attempted.
    /// When arrays are larger than this size and <see cref="DetectArrayShifts"/> is enabled,
    /// the algorithm falls back to positional diffing for performance.
    /// Defaults to <c>1000</c>.
    /// </summary>
    /// <remarks>
    /// Array shift detection uses O(n²) deep comparison in the worst case.
    /// For arrays larger than this threshold, the performance impact becomes prohibitive.
    /// Set to <c>null</c> to disable the size check and always attempt shift detection.
    /// </remarks>
    public int? MaxArrayShiftDetectionSize { get; init; } = 1000;

    internal static readonly DiffOptions Default = new();
}