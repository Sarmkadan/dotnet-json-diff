using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JsonDiff;
using Xunit;
using static JsonDiff.Tests.JsonPatchFormatterTestsConstants;

namespace JsonDiff.Tests
{
    public class JsonPatchFormatterTests : IJsonPatchFormatterTests, IEquatable<JsonPatchFormatterTests>
    {
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

        // IEquatable implementation
        public bool Equals(JsonPatchFormatterTests? other)
        {
            // The class has no instance state; all instances are considered equal if the other is not null.
            return other is not null;
        }

        public override bool Equals(object? obj) => Equals(obj as JsonPatchFormatterTests);

        public override int GetHashCode()
        {
            // No instance fields to hash; use a constant combined hash.
            return HashCode.Combine(0);
        }

        public static bool operator ==(JsonPatchFormatterTests? left, JsonPatchFormatterTests? right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(JsonPatchFormatterTests? left, JsonPatchFormatterTests? right) => !(left == right);
    }
}
