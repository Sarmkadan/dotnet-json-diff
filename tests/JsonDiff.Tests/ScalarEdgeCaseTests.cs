using System.Collections.Generic;
using System.Text.Json;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests
{
    /// <summary>
    /// Tests for edge cases involving scalar values in JSON comparison.
    /// This class verifies that the JSON differ correctly handles various scalar value scenarios
    /// including numeric tolerance, case sensitivity, boolean values, null vs missing properties,
    /// and type mismatches between empty objects and arrays.
    /// </summary>
    public class ScalarEdgeCaseTests
    {
        [Fact]
        /// <summary>
        /// Tests that numeric tolerance treats 1 and 1.0 as equal when numeric tolerance is enabled.
        /// Verifies that integer and floating-point representations of the same numeric value
        /// are considered identical when the NumericTolerance option is set to true.
        /// </summary>
        public void NumericTolerance_True_1_vs_1dot0_NoChanges()
        {
            var left = "1";
            var right = "1.0";
            var options = new DiffOptions { NumericTolerance = true };

            var changes = JsonDiffer.Diff(left, right, options);

            Assert.Empty(changes);
        }

        [Fact]
        /// <summary>
        /// Tests that numeric tolerance treats 1 and 1.0 as different when numeric tolerance is disabled.
        /// Verifies that integer and floating-point representations of the same numeric value
        /// are considered different when the NumericTolerance option is set to false.
        /// </summary>
        public void NumericTolerance_False_1_vs_1dot0_Changed()
        {
            var left = "1";
            var right = "1.0";
            var options = new DiffOptions { NumericTolerance = false };

            var changes = JsonDiffer.Diff(left, right, options);

            Assert.Single(changes);
            var change = changes.SingleChange();
            Assert.Equal(ChangeKind.Changed, change.Kind);
            Assert.Equal("", change.Path);
        }

        [Fact]
        /// <summary>
        /// Tests that numeric tolerance handles large numbers beyond double precision correctly.
        /// Verifies that when two large integers differ by 1 but round to the same double value,
        /// they are considered equal when NumericTolerance is enabled.
        /// </summary>
        public void NumericTolerance_True_LargeNumbers_NoChanges()
        {
            // Numbers beyond double precision
            var left = "9007199254740993";
            var right = "9007199254740992";
            var options = new DiffOptions { NumericTolerance = true };

            var changes = JsonDiffer.Diff(left, right, options);

            // With numeric tolerance enabled, the two values are considered equal
            // because they round to the same double value.
            Assert.Empty(changes);
        }

        [Fact]
        /// <summary>
        /// Tests that numeric tolerance handles large numbers correctly when disabled.
        /// Verifies that when two large integers differ by 1 and NumericTolerance is disabled,
        /// they are considered different and a change is detected.
        /// </summary>
        public void NumericTolerance_False_LargeNumbers_Changed()
        {
            var left = "9007199254740993";
            var right = "9007199254740992";
            var options = new DiffOptions { NumericTolerance = false };

            var changes = JsonDiffer.Diff(left, right, options);

            Assert.Single(changes);
            var change = changes.SingleChange();
            Assert.Equal(ChangeKind.Changed, change.Kind);
            Assert.Equal("", change.Path);
        }

        [Fact]
        /// <summary>
        /// Tests that string comparison is case-sensitive by default.
        /// Verifies that strings with different casing are considered different values
        /// when performing JSON comparison without explicit case-insensitive options.
        /// </summary>
        public void StringCaseSensitivity_Changed()
        {
            var left = "{\"a\":\"Hello\"}";
            var right = "{\"a\":\"hello\"}";

            var changes = JsonDiffer.Diff(left, right);

            Assert.Single(changes);
            var change = changes.SingleChange();
            Assert.Equal(ChangeKind.Changed, change.Kind);
            Assert.Equal("a", change.Path);
        }

        [Fact]
        /// <summary>
        /// Tests that boolean values true and false are considered different.
        /// Verifies that the JSON differ correctly identifies changes between
        /// boolean true and boolean false values.
        /// </summary>
        public void BooleanTrueVsFalse_Changed()
        {
            var left = "{\"a\":true}";
            var right = "{\"a\":false}";

            var changes = JsonDiffer.Diff(left, right);

            Assert.Single(changes);
            var change = changes.SingleChange();
            Assert.Equal(ChangeKind.Changed, change.Kind);
            Assert.Equal("a", change.Path);
        }

        [Fact]
        /// <summary>
        /// Tests that a null property is treated as a removed property when compared to a missing property.
        /// Verifies that when a property exists with a null value in the left JSON and is missing in the right JSON,
        /// it is detected as a removal rather than no change.
        /// </summary>
        public void NullVsMissingProperty_Removed()
        {
            var left = "{\"a\":null}";
            var right = "{}";

            var changes = JsonDiffer.Diff(left, right);

            Assert.Single(changes);
            var change = changes.SingleChange();
            Assert.Equal(ChangeKind.Removed, change.Kind);
            Assert.Equal("a", change.Path);
        }

        [Fact]
        /// <summary>
        /// Tests that a missing property is treated as an added property when compared to a null property.
        /// Verifies that when a property is missing in the left JSON and exists with a null value in the right JSON,
        /// it is detected as an addition rather than no change.
        /// </summary>
        public void MissingPropertyVsNull_Added()
        {
            var left = "{}";
            var right = "{\"a\":null}";

            var changes = JsonDiffer.Diff(left, right);

            Assert.Single(changes);
            var change = changes.SingleChange();
            Assert.Equal(ChangeKind.Added, change.Kind);
            Assert.Equal("a", change.Path);
        }

        [Fact]
        /// <summary>
        /// Tests that empty objects and empty arrays are considered different types.
        /// Verifies that {} and [] are detected as a type change when performing JSON comparison,
        /// as they represent different JSON data structures (object vs array).
        /// </summary>
        public void EmptyObjectVsEmptyArray_Changed()
        {
            var left = "{}";
            var right = "[]";

            var changes = JsonDiffer.Diff(left, right);

            Assert.Single(changes);
            var change = changes.SingleChange();
            Assert.Equal(ChangeKind.Changed, change.Kind);
            Assert.Equal("", change.Path);
        }
    }
}
