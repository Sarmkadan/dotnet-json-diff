using System;

namespace JsonDiff;

/// <summary>
/// Provides extension methods for <see cref="JsonDiffer"/> to support additional input types.
/// Use <see cref="JsonDifferExtensions"/> instead for all JSON diff operations.
/// </summary>
[Obsolete("This class has been renamed to 'JsonDifferExtensions' for clarity. Use JsonDifferExtensions.Diff() and JsonDifferExtensions.DeepEquals() methods instead.")]
public static class JsonDifferJsonExtensions
{
    // This class is obsolete and kept only for backward compatibility.
    // All functionality has been moved to JsonDifferExtensions.
}