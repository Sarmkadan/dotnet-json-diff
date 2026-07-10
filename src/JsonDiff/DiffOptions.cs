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

    internal static readonly DiffOptions Default = new();
}
