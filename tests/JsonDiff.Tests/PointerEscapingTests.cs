using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests
{
    /// <summary>
    /// Tests that JSON pointer escaping is performed correctly by JsonDiffer.
    /// </summary>
    public class PointerEscapingTests : IPointerEscapingTests
    {
        /// <summary>
        /// Verifies that a property name containing a slash is escaped as "~1" in the JSON pointer path.
        /// </summary>
        [Fact]
        public void SlashInPropertyName_IsEscaped()
        {
            // Arrange
            var left = """{ "a/b": 1 }""";
            var right = """{ "a/b": 2 }""";

            // Act
            var changes = JsonDiffer.Diff(left, right);
            var change = changes.Single();

            // Assert
            Assert.Equal("/a~1b", change.Path);
        }

        /// <summary>
        /// Verifies that a property name containing a tilde is escaped as "~0" in the JSON pointer path.
        /// </summary>
        [Fact]
        public void TildeInPropertyName_IsEscaped()
        {
            // Arrange
            var left = """{ "x~y": "foo" }""";
            var right = """{ "x~y": "bar" }""";

            // Act
            var changes = JsonDiffer.Diff(left, right);
            var change = changes.Single();

            // Assert
            Assert.Equal("/x~0y", change.Path);
        }

        /// <summary>
        /// Verifies that nested property names containing both slash and tilde are escaped correctly and combined in the JSON pointer path.
        /// </summary>
        [Fact]
        public void NestedEscaping_CombinesEscapesCorrectly()
        {
            // Arrange
            var left = """{ "obj": { "a/b": { "x~y": 1 } } }""";
            var right = """{ "obj": { "a/b": { "x~y": 2 } } }""";

            // Act
            var changes = JsonDiffer.Diff(left, right);
            var change = changes.Single();

            // Assert
            Assert.Equal("/obj/a~1b/x~0y", change.Path);
        }

        /// <summary>
        /// Verifies that a patch generated from changes involving keys with slashes, tildes, and an empty string can be applied cleanly and reproduces the expected JSON document.
        /// </summary>
        [Fact]
        public void RoundTrip_KeysWithSlashTildeAndEmptyString_PatchAppliesCleanly()
        {
            // Arrange: keys chosen to exercise both escape characters and the empty-string edge case.
            var left = """{ "a/b": 1, "~": 2, "": 3 }""";
            var right = """{ "a/b": 10, "~": 20, "": 30 }""";

            // Act
            var changes = JsonDiffer.Diff(left, right);
            var patchJson = JsonPatchFormatter.ToJsonPatch(changes);

            // Assert: every emitted path resolves back to the correct raw key via RFC 6901 semantics,
            // and applying the patch against the left document reproduces the right document.
            var target = JsonNode.Parse(left)!.AsObject();
            using var patchDoc = JsonDocument.Parse(patchJson);
            foreach (var op in patchDoc.RootElement.EnumerateArray())
            {
                var pointer = JsonPointer.Parse(op.GetProperty("path").GetString()!);
                var key = Assert.Single(pointer.Segments);
                Assert.Equal("replace", op.GetProperty("op").GetString());
                target[key] = JsonNode.Parse(op.GetProperty("value").GetRawText());
            }

            Assert.Equal(
                JsonNode.Parse(right)!.ToJsonString(),
                target.ToJsonString());
        }
    }
}
