using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests
{
    public class JsonPatchFormatterTests
    {
        [Fact]
        public void AddOperation_RendersCorrectly()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Added, "/foo/bar", null, JsonDocument.Parse("\"baz\"").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var op = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal("add", op.GetProperty("op").GetString());
            Assert.Equal("/foo/bar", op.GetProperty("path").GetString());
            Assert.Equal("baz", op.GetProperty("value").GetString());
        }

        [Fact]
        public void RemoveOperation_RendersCorrectly()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Removed, "/foo/bar", JsonDocument.Parse("\"baz\"").RootElement, null)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var op = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal("remove", op.GetProperty("op").GetString());
            Assert.Equal("/foo/bar", op.GetProperty("path").GetString());
            Assert.False(op.TryGetProperty("value", out _));
        }

        [Fact]
        public void ReplaceOperation_RendersCorrectly()
        {
            // Arrange
            var changes = new List<JsonChange>
            {
                new JsonChange(ChangeKind.Changed, "/foo/bar", JsonDocument.Parse("1").RootElement, JsonDocument.Parse("2").RootElement)
            };

            // Act
            var json = JsonPatchFormatter.ToJsonPatch(changes);
            var doc = JsonDocument.Parse(json);
            var op = doc.RootElement.EnumerateArray().First();

            // Assert
            Assert.Equal("replace", op.GetProperty("op").GetString());
            Assert.Equal("/foo/bar", op.GetProperty("path").GetString());
            Assert.Equal(2, op.GetProperty("value").GetInt32());
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
            Assert.Equal("remove", first.GetProperty("op").GetString());
            
            var second = doc.RootElement[1];
            Assert.Equal("add", second.GetProperty("op").GetString());
        }
    }
}
