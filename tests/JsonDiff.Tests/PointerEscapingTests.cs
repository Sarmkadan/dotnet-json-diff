using System.Linq;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests
{
    public class PointerEscapingTests
    {
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
    }
}
