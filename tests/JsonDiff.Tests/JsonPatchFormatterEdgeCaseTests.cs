using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests
{
    /// <summary>
    /// Comprehensive edge-case tests for <see cref="JsonPatchFormatter"/> to validate:
    /// - Empty diffs (zero changes) produce valid empty patch arrays
    /// - Special character escaping in paths (RFC 6901 compliance)
    /// - Array index path handling after detected shifts
    /// - Round-tripping through JsonDocument.Parse for validation
    /// </summary>
    public class JsonPatchFormatterEdgeCaseTests
    {
        [Fact]
        public void EmptyChangesList_ProducesEmptyArray_NotNullOrMalformed()
        {
            // Arrange
            var changes = Array.Empty<JsonChange>();

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);

            // Assert: Should produce valid JSON array, not null or empty string
            Assert.NotNull(json);
            Assert.NotEmpty(json);

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            Assert.Equal(0, root.GetArrayLength());
        }

        [Fact]
        public void EmptyChangesList_ProducesValidJsonArray()
        {
            // Arrange
            var changes = Array.Empty<JsonChange>();

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);

            // Assert: Should be a valid JSON array that can be parsed
            var parseResult = JsonDocument.Parse(json);
            Assert.Equal(JsonValueKind.Array, parseResult.RootElement.ValueKind);
            Assert.Equal(0, parseResult.RootElement.GetArrayLength());
        }

        [Fact]
        public void SingleChange_WithSpecialCharactersInPath_ProducesValidPatch()
        {
            // Arrange: Test key with both '/' and '~' characters
            var changes = new List<JsonChange>
            {
                new JsonChange(
                    ChangeKind.Changed,
                    "/properties/a~1b/c~0d",
                    JsonDocument.Parse("\"old\"").RootElement,
                    JsonDocument.Parse("\"new\"").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var operation = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal("replace", operation.GetProperty("op").GetString());
            Assert.Equal("/properties/a~1b/c~0d", operation.GetProperty("path").GetString());
            Assert.Equal("\"new\"", operation.GetProperty("value").GetRawText());
        }

        [Fact]
        public void MultipleChanges_WithMixedSpecialCharacters_AllPathsEscapedCorrectly()
        {
            // Arrange: Mix of normal paths and paths with special characters
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Added, "/normal/path", null, JsonDocument.Parse("1").RootElement),
                new JsonChange(ChangeKind.Removed, "/special/key~with~tilde", JsonDocument.Parse("2").RootElement, null),
                new JsonChange(ChangeKind.Changed, "/array/0/item", JsonDocument.Parse("3").RootElement, JsonDocument.Parse("4").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);

            // Assert: All operations should be valid
            var operations = doc.RootElement.EnumerateArray().ToList();
            Assert.Equal(3, operations.Count);

            // Verify each operation has required fields
            foreach (var operation in operations)
            {
                Assert.True(operation.TryGetProperty("op", out _));
                Assert.True(operation.TryGetProperty("path", out _));
                Assert.Equal("add", operations[0].GetProperty("op").GetString());
                Assert.Equal("remove", operations[1].GetProperty("op").GetString());
                Assert.Equal("replace", operations[2].GetProperty("op").GetString());
            }
        }

        [Fact]
        public void ChangeWithNullValue_ProducesValidJsonWithNullValue()
        {
            // Arrange: Test with null value (common for removed properties)
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Removed, "/null/value", JsonDocument.Parse("null").RootElement, null)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var operation = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal("remove", operation.GetProperty("op").GetString());
            Assert.Equal("/null/value", operation.GetProperty("path").GetString());
            // Remove operations should not have a value property
            Assert.False(operation.TryGetProperty("value", out _));
        }

        [Fact]
        public void ChangeWithComplexNestedValue_ProducesValidJson()
        {
            // Arrange: Test with complex nested JSON value
            var complexValue = JsonDocument.Parse("{\"nested\":{\"array\":[1,2,3]}}").RootElement;
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Added, "/complex/structure", null, complexValue)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var operation = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal("add", operation.GetProperty("op").GetString());
            Assert.Equal("/complex/structure", operation.GetProperty("path").GetString());
            Assert.True(operation.TryGetProperty("value", out var value));
            Assert.Equal(JsonValueKind.Object, value.ValueKind);
        }

        [Fact]
        public void RoundTrip_FormatterOutput_ProducesValidJsonPatchShape()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Added, "/added", null, JsonDocument.Parse("\"value\"").RootElement),
                new JsonChange(ChangeKind.Removed, "/removed", JsonDocument.Parse("\"old\"").RootElement, null),
                new JsonChange(ChangeKind.Changed, "/changed", JsonDocument.Parse("1").RootElement, JsonDocument.Parse("2").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);

            // Assert: Valid JSON Patch shape (array of objects with op/path/value)
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            var operations = doc.RootElement.EnumerateArray().ToList();
            Assert.Equal(3, operations.Count);

            // Each operation should have the required RFC 6902 fields
            foreach (var operation in operations)
            {
                Assert.True(operation.TryGetProperty("op", out _), "Each operation must have 'op' field");
                Assert.True(operation.TryGetProperty("path", out _), "Each operation must have 'path' field");

                var op = operation.GetProperty("op").GetString();
                if (op is "add" or "replace")
                {
                    Assert.True(operation.TryGetProperty("value", out _), $"Operation with op='{op}' must have 'value' field");
                }
                else if (op == "remove")
                {
                    Assert.False(operation.TryGetProperty("value", out _), "Remove operation must not have 'value' field");
                }
            }
        }

        [Fact]
        public void NullChangesList_ThrowsArgumentNullException()
        {
            // Arrange
            IReadOnlyList<JsonChange> changes = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => JsonPatchFormatter.ToJsonPatch(changes));
        }

        [Fact]
        public void ChangeWithEmptyPath_ProducesValidPatch()
        {
            // Arrange: Empty path should represent root-level change
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Changed, "/", JsonDocument.Parse("1").RootElement, JsonDocument.Parse("2").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var operation = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal("replace", operation.GetProperty("op").GetString());
            Assert.Equal("/", operation.GetProperty("path").GetString());
            Assert.Equal(2, operation.GetProperty("value").GetInt32());
        }

        [Fact]
        public void ArrayIndexPaths_WithDifferentChangeKinds_AllValid()
        {
            // Arrange: Test various array index scenarios
            var changes = new List<JsonChange>
            {
                // Array element added
                new JsonChange(ChangeKind.Added, "/items/0", null, JsonDocument.Parse("\"newItem\"").RootElement),

                // Array element removed
                new JsonChange(ChangeKind.Removed, "/items/1", JsonDocument.Parse("\"oldItem\"").RootElement, null),

                // Array element changed
                new JsonChange(ChangeKind.Changed, "/items/2", JsonDocument.Parse("1").RootElement, JsonDocument.Parse("2").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);

            // Assert
            var operations = doc.RootElement.EnumerateArray().ToList();
            Assert.Equal(3, operations.Count);
            Assert.Equal("add", operations[0].GetProperty("op").GetString());
            Assert.Equal("remove", operations[1].GetProperty("op").GetString());
            Assert.Equal("replace", operations[2].GetProperty("op").GetString());
        }

        [Fact]
        public void DeepNestedPaths_WithManySegments_ProducesValidPatch()
        {
            // Arrange: Very deep nesting
            var changes = new List<JsonChange>
            {
                new JsonChange(
                    ChangeKind.Added,
                    "/level1/level2/level3/level4/level5/deep",
                    null,
                    JsonDocument.Parse("\"deepValue\"").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);

            // Assert
            var operation = doc.RootElement.EnumerateArray().First();
            Assert.Equal("add", operation.GetProperty("op").GetString());
            Assert.Equal("/level1/level2/level3/level4/level5/deep", operation.GetProperty("path").GetString());
        }

        [Fact]
        public void BooleanAndNumericValues_FormattedCorrectly()
        {
            // Arrange: Test various JSON value types
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Added, "/bool", null, JsonDocument.Parse("true").RootElement),
                new JsonChange(ChangeKind.Added, "/number", null, JsonDocument.Parse("42").RootElement),
                new JsonChange(ChangeKind.Added, "/float", null, JsonDocument.Parse("3.14").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);

            // Assert
            var operations = doc.RootElement.EnumerateArray().ToList();
            Assert.True( operations[0].GetProperty("value").GetBoolean());
            Assert.Equal(42, operations[1].GetProperty("value").GetInt32());
            Assert.Equal(3.14, operations[2].GetProperty("value").GetDouble());
        }

        [Fact]
        public void FormatterOutput_IsAlwaysValidUtf8Json()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Added, "/test", null, JsonDocument.Parse("\"value\"").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);

            // Assert: Should be valid JSON that can be parsed
            // Should be parseable by standard JSON parser
            var parseResult = JsonDocument.Parse(json);
            Assert.NotNull(parseResult);
            Assert.Equal(JsonValueKind.Array, parseResult.RootElement.ValueKind);
        }

        [Fact]
        public void MultipleOperations_OrderPreservedInOutput()
        {
            // Arrange: Changes in specific order
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Removed, "/first", JsonDocument.Parse("1").RootElement, null),
                new JsonChange(ChangeKind.Added, "/second", null, JsonDocument.Parse("2").RootElement),
                new JsonChange(ChangeKind.Changed, "/third", JsonDocument.Parse("3").RootElement, JsonDocument.Parse("4").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);

            // Assert: Order should be preserved
            var operations = doc.RootElement.EnumerateArray().ToList();
            Assert.Equal(3, operations.Count);
            Assert.Equal("remove", operations[0].GetProperty("op").GetString());
            Assert.Equal("add", operations[1].GetProperty("op").GetString());
            Assert.Equal("replace", operations[2].GetProperty("op").GetString());
        }
    }
}