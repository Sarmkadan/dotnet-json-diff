namespace JsonDiff
{
    /// <summary>
    /// Interface exposing the public instance members of <see cref="DiffOptions"/>.
    /// </summary>
    public interface IDiffOptions
    {
        /// <summary>
        /// When <c>true</c>, numbers that are numerically equal but written differently
        /// (e.g. <c>1</c> vs <c>1.0</c> vs <c>1e0</c>) are treated as equal.
        /// Defaults to <c>true</c>.
        /// </summary>
        bool NumericTolerance { get; init; }

        /// <summary>
        /// When <c>true</c>, object property names are compared case-insensitively.
        /// Defaults to <c>false</c>.
        /// </summary>
        bool IgnorePropertyCase { get; init; }

        /// <summary>
        /// Maximum depth to traverse when diffing nested objects/arrays.
        /// When <c>null</c>, no limit is applied (unlimited depth).
        /// Defaults to <c>null</c>.
        /// </summary>
        int? MaxDepth { get; init; }
    }
}
