using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonDiff.Tests;

/// <summary>
/// Provides validation helpers for the <see cref="DiffTests"/> test class.
/// </summary>
public static class DiffTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="DiffTests"/> instance.
    /// </summary>
    /// <param name="value">The <see cref="DiffTests"/> instance to validate.</param>
    /// <returns>
    /// A read-only list of human-readable problem descriptions. An empty list indicates that the instance is valid.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public static IReadOnlyList<string> Validate(this DiffTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // DiffTests contains only test methods and no stateful members.
        // Therefore, there are no runtime validation rules to apply.
        // Returning an empty list indicates that the instance is valid.
        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether the specified <see cref="DiffTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The <see cref="DiffTests"/> instance to check.</param>
    /// <returns><c>true</c> if the instance is valid or <c>null</c>; otherwise, <c>false</c>.</returns>
    public static bool IsValid(this DiffTests? value)
        => value is null || Validate(value).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="DiffTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The <see cref="DiffTests"/> instance to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the instance is not valid.
    /// </exception>
    public static void EnsureValid(this DiffTests? value)
    {
        if (value is null)
            return;

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            var message = string.Join("; ", problems);
            throw new ArgumentException(message, nameof(value));
        }
    }
}