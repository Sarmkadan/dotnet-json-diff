using System.Collections.Generic;
using System.Text.Json;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests
{
    public class ScalarEdgeCaseTests
    {
        [Fact]
        public void NumericTolerance_True_1_vs_1dot0_NoChanges()
        {
            var left = "1";
            var right = "1.0";
            var options = new DiffOptions { NumericTolerance = true };

            var changes = JsonDiffer.Diff(left, right, options);

            Assert.Empty(changes);
        }

        [Fact]
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
