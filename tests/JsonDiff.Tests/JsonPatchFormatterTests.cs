using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JsonDiff;
using Xunit;
using static JsonDiff.Tests.JsonPatchFormatterTestsConstants;

namespace JsonDiff.Tests
{
    /// <summary>
    /// Tests for <see cref="JsonPatchFormatter"/> ensuring that JSON Patch operations are rendered correctly.
    /// </summary>
    public class JsonPatchFormatterTests : IJsonPatchFormatterTests, IEquatable<JsonPatchFormatterTests>
    {
        /// <summary>
        /// Verifies that an added change is rendered as a JSON Patch <c>add</c> operation with the correct path and value.
        /// </summary>
        [Fact]
        public void AddOperation_RendersCorrectly()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Added, FooBarPath, null, JsonDocument.Parse("\"baz\"").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var op = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal(AddOp, op.GetProperty(OpProperty).GetString());
            Assert.Equal(FooBarPath, op.GetProperty(PathProperty).GetString());
            Assert.Equal("baz", op.GetProperty(ValueProperty).GetString());
        }

        /// <summary>
        /// Verifies that a removed change is rendered as a JSON Patch <c>remove</c> operation with the correct path and without a value.
        /// </summary>
        [Fact]
        public void RemoveOperation_RendersCorrectly()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Removed, FooBarPath, JsonDocument.Parse("\"baz\"").RootElement, null)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var op = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal(RemoveOp, op.GetProperty(OpProperty).GetString());
            Assert.Equal(FooBarPath, op.GetProperty(PathProperty).GetString());
            Assert.False(op.TryGetProperty(ValueProperty, out _));
        }

        /// <summary>
        /// Verifies that a changed (replaced) value is rendered as a JSON Patch <c>replace</c> operation with the correct path and new value.
        /// </summary>
        [Fact]
        public void ReplaceOperation_RendersCorrectly()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Changed, FooBarPath, JsonDocument.Parse("1").RootElement, JsonDocument.Parse("2").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var op = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal(ReplaceOp, op.GetProperty(OpProperty).GetString());
            Assert.Equal(FooBarPath, op.GetProperty(PathProperty).GetString());
            Assert.Equal(2, op.GetProperty(ValueProperty).GetInt32());
        }

        /// <summary>
        /// Verifies that multiple changes are rendered as a JSON array containing the appropriate operations in order.
        /// </summary>
        [Fact]
        public void MultipleChanges_RendersArray()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Removed, "/old", JsonDocument.Parse("1").RootElement, null),
                new JsonChange(ChangeKind.Added, "/new", null, JsonDocument.Parse("2").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);

            // Assert
            Assert.Equal(2, doc.RootElement.GetArrayLength());
            
            var first = doc.RootElement[0];
            Assert.Equal(RemoveOp, first.GetProperty(OpProperty).GetString());
            
            var second = doc.RootElement[1];
            Assert.Equal(AddOp, second.GetProperty(OpProperty).GetString());
        }

        /// <summary>
        /// Determines whether the specified <see cref="JsonPatchFormatterTests"/> instance is equal to the current instance.
        /// Since the class has no instance state, any non-null instance is considered equal.
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns>True if <paramref name="other"/> is not null; otherwise false.</returns>
        public bool Equals(JsonPatchFormatterTests? other)
        {
            // The class has no instance state; all instances are considered equal if the other is not null.
            return other is not null;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if <paramref name="obj"/> is a non-null <see cref="JsonPatchFormatterTests"/>; otherwise false.</returns>
        public override bool Equals(object? obj) => Equals(obj as JsonPatchFormatterTests);

        /// <summary>
        /// Returns a hash code for the current instance. Since there is no instance state, a constant hash is returned.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            // No instance fields to hash; use a constant combined hash.
            return HashCode.Combine(0);
        }

        /// <summary>
        /// Determines whether two <see cref="JsonPatchFormatterTests"/> instances are equal.
        /// </summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns>True if both refer to the same instance or both are non-null; otherwise false.</returns>
        public static bool operator ==(JsonPatchFormatterTests? left, JsonPatchFormatterTests? right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="JsonPatchFormatterTests"/> instances are not equal.
        /// </summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns>True if the operands are not equal; otherwise false.</returns>
        public static bool operator !=(JsonPatchFormatterTests? left, JsonPatchFormatterTests? right) => !(left == right);
    }
}
