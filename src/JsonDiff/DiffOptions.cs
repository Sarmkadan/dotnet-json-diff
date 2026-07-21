namespace JsonDiff;

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

    internal static readonly DiffOptions Default = new();
}