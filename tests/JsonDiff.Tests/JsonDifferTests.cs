using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Edge-case coverage for array shift detection (<see cref="DiffOptions.DetectArrayShifts"/>)
/// in <see cref="JsonDiffer"/>: where a single inserted or removed element is reported, how
/// ambiguous duplicates resolve, and how type changes and mixed element types are reported.
/// </summary>
public class JsonDifferTests
{
    private static DiffOptions ShiftOptions => new() { DetectArrayShifts = true };

    /// <summary>
    /// Tests that inserting an element at index 0 is reported as one Added change, not as a
    /// positional change at every shifted index.
    /// </summary>
    [Fact]
    public void InsertAtFront_Primitives_ReportsSingleAdded()
    {
        // Arrange
        const string left = "[2,3,4]";
        const string right = "[1,2,3,4]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/0", c.Path);
        Assert.Equal("1", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that inserting an object at index 0 shifts every existing object, and that this is
    /// reported as one Added change at the new index.
    /// </summary>
    [Fact]
    public void InsertAtFront_Objects_ReportsSingleAdded()
    {
        // Arrange
        const string left = "[{\"id\":2},{\"id\":3}]";
        const string right = "[{\"id\":1},{\"id\":2},{\"id\":3}]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/0", c.Path);
        Assert.Equal("{\"id\":1}", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that inserting two elements at the front is reported as two Added changes at indices
    /// 0 and 1, with no Changed entries for the shifted tail.
    /// </summary>
    [Fact]
    public void InsertTwoAtFront_Primitives_ReportsTwoAddedAndNoChanges()
    {
        // Arrange
        const string left = "[1,2,3]";
        const string right = "[0,0,1,2,3]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        Assert.Equal(2, changes.Count);
        Assert.All(changes, c => Assert.Equal(ChangeKind.Added, c.Kind));
        Assert.Equal(["/0", "/1"], changes.Select(c => c.Path));
    }

    /// <summary>
    /// Tests that the same front insert without shift detection cascades into positional changes,
    /// which is the behavior the flag exists to avoid.
    /// </summary>
    [Fact]
    public void InsertAtFront_WithoutShiftDetection_ReportsPositionalChanges()
    {
        // Arrange
        const string left = "[2,3]";
        const string right = "[1,2,3]";

        // Act
        var changes = JsonDiffer.Diff(left, right, new DiffOptions { DetectArrayShifts = false });

        // Assert
        Assert.Equal(
            [ChangeKind.Changed, ChangeKind.Changed, ChangeKind.Added],
            changes.Select(c => c.Kind));
        Assert.Equal(["/0", "/1", "/2"], changes.Select(c => c.Path));
    }

    /// <summary>
    /// Tests that removing an element from the middle is reported as one Removed change at the
    /// removed element's index, not as Changed entries for every element after it.
    /// </summary>
    [Fact]
    public void RemoveFromMiddle_Primitives_ReportsSingleRemoved()
    {
        // Arrange
        const string left = "[1,2,3,4]";
        const string right = "[1,3,4]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/1", c.Path);
        Assert.Equal("2", c.Left?.GetRawText());
    }

    /// <summary>
    /// Tests that inserting an element into the middle is reported as one Added change at the
    /// inserted element's index.
    /// </summary>
    [Fact]
    public void InsertIntoMiddle_Primitives_ReportsSingleAdded()
    {
        // Arrange
        const string left = "[1,3,4]";
        const string right = "[1,2,3,4]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/1", c.Path);
        Assert.Equal("2", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that a middle removal inside a run of duplicate values is reported at the last
    /// occurrence of the run, which is the deterministic choice when the matching prefix is longest.
    /// </summary>
    [Fact]
    public void RemoveFromDuplicateRun_Primitives_ReportsLastOccurrenceOfRun()
    {
        // Arrange
        const string left = "[1,2,2,3]";
        const string right = "[1,2,3]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/2", c.Path);
        Assert.Equal("2", c.Left?.GetRawText());
    }

    /// <summary>
    /// Tests that when the removed value also appears at the start of the array, the prefix match is
    /// preferred, so the removal is reported at the trailing occurrence.
    /// </summary>
    [Fact]
    public void RemoveDuplicateValue_PrefixMatchWins_ReportsTrailingOccurrence()
    {
        // Arrange
        const string left = "[1,2,1]";
        const string right = "[1,2]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/2", c.Path);
    }

    /// <summary>
    /// Tests that when the removed value also appears at the start of the array and no prefix matches,
    /// the suffix match is used, so the removal is reported at the leading occurrence.
    /// </summary>
    [Fact]
    public void RemoveDuplicateValue_SuffixMatchWins_ReportsLeadingOccurrence()
    {
        // Arrange
        const string left = "[1,1,2]";
        const string right = "[1,2]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/0", c.Path);
        Assert.Equal("1", c.Left?.GetRawText());
    }

    /// <summary>
    /// Tests that an equal-length array with rotated duplicate values is not shift-detected: shift
    /// detection only runs when lengths differ, so each position is reported as Changed.
    /// </summary>
    [Fact]
    public void EqualLength_RotatedDuplicates_ReportsPositionalChanges()
    {
        // Arrange
        const string left = "[1,1,2]";
        const string right = "[1,2,1]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        Assert.Equal(2, changes.Count);
        Assert.All(changes, c => Assert.Equal(ChangeKind.Changed, c.Kind));
        Assert.Equal(["/1", "/2"], changes.Select(c => c.Path));
    }

    /// <summary>
    /// Tests that an array replaced by a scalar at the same path is reported as a single Changed
    /// change at that path, not as a Removed change for each element.
    /// </summary>
    [Fact]
    public void ArrayReplacedByScalar_ReportsSingleChanged()
    {
        // Arrange
        const string left = "{\"a\":[1,2,3]}";
        const string right = "{\"a\":5}";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
        Assert.Equal("[1,2,3]", c.Left?.GetRawText());
        Assert.Equal("5", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that an array replaced by an object at the same path is reported as a single Changed
    /// change at that path.
    /// </summary>
    [Fact]
    public void ArrayReplacedByObject_ReportsSingleChanged()
    {
        // Arrange
        const string left = "{\"a\":[{\"id\":1},{\"id\":2}]}";
        const string right = "{\"a\":{\"id\":1}}";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
    }

    /// <summary>
    /// Tests that an empty array replaced by a scalar is reported as a single Changed change,
    /// and that the reverse direction is also a single Changed change.
    /// </summary>
    [Theory]
    [InlineData("{\"a\":[]}", "{\"a\":\"x\"}")]
    [InlineData("{\"a\":\"x\"}", "{\"a\":[]}")]
    public void EmptyArrayAndScalar_ReportsSingleChanged(string left, string right)
    {
        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Changed, c.Kind);
        Assert.Equal("/a", c.Path);
    }

    /// <summary>
    /// Tests that growing an empty array to a non-empty one reports each new element as Added and
    /// that shrinking it to empty reports each element as Removed.
    /// </summary>
    [Fact]
    public void EmptyToNonEmpty_ReportsEachElementAdded_AndBackRemoved()
    {
        // Act
        var grown = JsonDiffer.Diff("[]", "[1,2]", ShiftOptions);
        var shrunk = JsonDiffer.Diff("[1,2]", "[]", ShiftOptions);

        // Assert
        Assert.Equal(2, grown.Count);
        Assert.All(grown, c => Assert.Equal(ChangeKind.Added, c.Kind));
        Assert.Equal(["/0", "/1"], grown.Select(c => c.Path));

        Assert.Equal(2, shrunk.Count);
        Assert.All(shrunk, c => Assert.Equal(ChangeKind.Removed, c.Kind));
        Assert.Equal(["/0", "/1"], shrunk.Select(c => c.Path));
    }

    /// <summary>
    /// Tests that two empty arrays produce no changes.
    /// </summary>
    [Fact]
    public void EmptyToEmpty_ProducesNoChanges()
    {
        // Act
        var changes = JsonDiffer.Diff("[]", "[]", ShiftOptions);

        // Assert
        Assert.Empty(changes);
    }

    /// <summary>
    /// Tests that arrays with elements of different JSON kinds at the same positions are diffed
    /// element by element, reporting one Changed per position whose kind differs.
    /// </summary>
    [Fact]
    public void SameLength_DifferentElementTypes_DiffsElementByElement()
    {
        // Arrange
        const string left = "[1,2,3]";
        const string right = "[\"1\",\"2\",\"3\"]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        Assert.All(changes, c => Assert.Equal(ChangeKind.Changed, c.Kind));
        Assert.Equal(["/0", "/1", "/2"], changes.Select(c => c.Path));
    }

    /// <summary>
    /// Tests that when element types differ and lengths differ, the type mismatch does not
    /// count as a shift match: the shorter prefix is compared positionally and the extra element
    /// is reported as Added.
    /// </summary>
    [Fact]
    public void DifferentLength_DifferentElementTypes_TypeMismatchIsNotAShift()
    {
        // Arrange
        const string left = "[1,2]";
        const string right = "[\"1\",\"2\",\"3\"]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        Assert.Equal(
            [ChangeKind.Changed, ChangeKind.Changed, ChangeKind.Added],
            changes.Select(c => c.Kind));
        Assert.Equal(["/0", "/1", "/2"], changes.Select(c => c.Path));
    }

    /// <summary>
    /// Tests that nested arrays of different element types are diffed element by element, and
    /// that the shift detection applies within the nested array.
    /// </summary>
    [Fact]
    public void NestedArrays_MixedTypes_ShiftDetectedWithinInnerArray()
    {
        // Arrange
        const string left = "[[1,\"a\"],[true]]";
        const string right = "[[1,2,\"a\"],[true]]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/0/1", c.Path);
        Assert.Equal("2", c.Right?.GetRawText());
    }

    /// <summary>
    /// Tests that a shift is recognised when a matching element differs only in the number lexical
    /// form (1 vs 1.0), since shift detection uses the same numeric comparison as the rest of the diff.
    /// </summary>
    [Fact]
    public void Shift_NumericallyEqualElements_AreMatched()
    {
        // Arrange
        const string left = "[1,2]";
        const string right = "[1.0,2,3]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/2", c.Path);
    }

    /// <summary>
    /// Tests that a shift is recognised when a matching object element has its properties in a
    /// different order, since property order is irrelevant to the diff.
    /// </summary>
    [Fact]
    public void Shift_ObjectElementWithReorderedProperties_IsMatched()
    {
        // Arrange
        const string left = "[{\"a\":1,\"b\":2}]";
        const string right = "[{\"b\":2,\"a\":1},{\"a\":1,\"b\":2}]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/1", c.Path);
    }

    /// <summary>
    /// Tests that a shift is recognised when a matching nested object element differs only in
    /// numeric lexical form (1.0 vs 1), so the removal is reported as a single change.
    /// </summary>
    [Fact]
    public void Shift_NestedObjectWithNumericallyEqualValue_IsMatched()
    {
        // Arrange
        const string left = "[{\"v\":0},{\"v\":2.0}]";
        const string right = "[{\"v\":2}]";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Removed, c.Kind);
        Assert.Equal("/0", c.Path);
    }

    /// <summary>
    /// Tests that a shift inside an object property is reported at the nested array's path.
    /// </summary>
    [Fact]
    public void InsertAtFront_NestedUnderProperty_ReportsAddedAtNestedPath()
    {
        // Arrange
        const string left = "{\"items\":[{\"id\":2},{\"id\":3}]}";
        const string right = "{\"items\":[{\"id\":1},{\"id\":2},{\"id\":3}]}";

        // Act
        var changes = JsonDiffer.Diff(left, right, ShiftOptions);

        // Assert
        var c = Assert.Single(changes);
        Assert.Equal(ChangeKind.Added, c.Kind);
        Assert.Equal("/items/0", c.Path);
    }
}
