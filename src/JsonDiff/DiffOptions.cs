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
/// Controls how JSON numbers are compared for equality.
/// </summary>
public enum NumberComparison
{
    /// <summary>
    /// Numbers are equal only when their raw JSON text matches exactly.
    /// Under this mode <c>1</c> and <c>1.0</c> are reported as different values.
    /// </summary>
    Exact,

    /// <summary>
    /// Numbers are compared by numeric value: <c>1</c>, <c>1.0</c> and <c>1e0</c> are equal,
    /// as are <c>100</c> and <c>1e2</c>, and <c>0</c> and <c>-0</c>.
    /// Values are compared as <see cref="decimal"/> when both parse (preserving precision
    /// that <see cref="double"/> would round away); values outside the decimal range
    /// (e.g. <c>1e400</c>) fall back to <see cref="double"/> and finally to raw-text comparison.
    /// </summary>
    Semantic
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
    /// Strategy used to compare JSON numbers.
    /// <see cref="JsonDiff.NumberComparison.Semantic"/> (the default) treats numerically-equal
    /// values with different lexical forms (<c>1</c> vs <c>1.0</c>, <c>100</c> vs <c>1e2</c>,
    /// <c>0</c> vs <c>-0</c>) as equal, using <see cref="decimal"/> precision where possible.
    /// <see cref="JsonDiff.NumberComparison.Exact"/> requires identical raw JSON text.
    /// Applies only when <see cref="NumericTolerance"/> is <c>true</c>; when it is <c>false</c>,
    /// numbers are always compared by raw text.
    /// </summary>
    public NumberComparison NumberComparison { get; init; } = NumberComparison.Semantic;

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

    /// <summary>
    /// Maximum number of changes to report before truncating the result.
    /// When <c>null</c>, no limit is applied (unlimited changes).
    /// Defaults to <c>null</c>.
    /// </summary>
    /// <remarks>
    /// This protects against pathological documents that differ in every leaf node,
    /// which could otherwise consume excessive memory and CPU.
    /// When the limit is exceeded, a <see cref="JsonDiffLimitExceededException"/> is thrown.
    /// </remarks>
    public int? MaxChanges { get; init; }

    internal static readonly DiffOptions Default = new();
}