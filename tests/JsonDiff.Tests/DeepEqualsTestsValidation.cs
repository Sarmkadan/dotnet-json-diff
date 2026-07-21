using System;
using System.Collections.Generic;
using System.Globalization;

namespace JsonDiff.Tests;

/// <summary>
/// Provides validation helpers for <see cref="DeepEqualsTests"/> instances.
/// </summary>
public static class DeepEqualsTestsValidation
{
    /// <summary>
    /// Validates that a <see cref="DeepEqualsTests"/> instance contains valid values.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this DeepEqualsTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate that all test methods are non-null and have meaningful names
        // This is a structural validation since we can't inspect the test methods at runtime
        // without reflection

        if (value.GetType() != typeof(DeepEqualsTests))
        {
            problems.Add("Instance is not of expected type DeepEqualsTests.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="DeepEqualsTests"/> instance contains valid values.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(this DeepEqualsTests value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that a <see cref="DeepEqualsTests"/> instance contains valid values.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance contains validation problems.</exception>
    public static void EnsureValid(this DeepEqualsTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"DeepEqualsTests instance is invalid. Problems: {string.Join(" ", problems)}");
        }
    }
}