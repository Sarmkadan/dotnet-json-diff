using System.Linq;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Test suite for JSON diffing functionality.
/// Validates that the JsonDiffer correctly identifies and reports differences between JSON documents.
/// </summary>
public class DiffTests
{
	/// <summary>
	/// Tests that identical JSON documents produce no differences.
	/// </summary>
	[Fact]
	public void IdenticalDocuments_ProduceNoChanges()
	{
		var changes = JsonDiffer.Diff("""{"a":1,"b":2}""", """{"a":1,"b":2}""");
		Assert.Empty(changes);
	}

	/// <summary>
	/// Tests that property order differences are ignored during comparison.
	/// </summary>
	[Fact]
	public void KeyOrder_IsIgnored()
	{
		var changes = JsonDiffer.Diff("""{"a":1,"b":2}""", """{"b":2,"a":1}""");
		Assert.Empty(changes);
	}

	/// <summary>
	/// Tests that adding a property is correctly detected and reported.
	/// </summary>
	[Fact]
	public void AddedProperty_IsReported()
	{
		var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":1,"b":2}""");
		var c = Assert.Single(changes);
		Assert.Equal(ChangeKind.Added, c.Kind);
		Assert.Equal("/b", c.Path);
	}

	/// <summary>
	/// Tests that removing a property is correctly detected and reported.
	/// </summary>
	[Fact]
	public void RemovedProperty_IsReported()
	{
		var changes = JsonDiffer.Diff("""{"a":1,"b":2}""", """{"a":1}""");
		var c = Assert.Single(changes);
		Assert.Equal(ChangeKind.Removed, c.Kind);
		Assert.Equal("/b", c.Path);
	}

	/// <summary>
	/// Tests that changing a scalar value is correctly detected and reported.
	/// </summary>
	[Fact]
	public void ChangedScalar_IsReported()
	{
		var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":2}""");
		var c = Assert.Single(changes);
		Assert.Equal(ChangeKind.Changed, c.Kind);
		Assert.Equal("/a", c.Path);
	}

	/// <summary>
	/// Tests that changing the kind of a value (e.g., from object to scalar) is reported at the parent level.
	/// </summary>
	[Fact]
	public void KindChange_IsReportedWithoutDescent()
	{
		var changes = JsonDiffer.Diff("""{"a":{"x":1}}""", """{"a":5}""");
		var c = Assert.Single(changes);
		Assert.Equal(ChangeKind.Changed, c.Kind);
		Assert.Equal("/a", c.Path);
	}

	/// <summary>
	/// Tests that nested property paths use slash notation for separation.
	/// </summary>
	[Fact]
	public void NestedPaths_UseSlashSeparator()
	{
		var changes = JsonDiffer.Diff("""{"user":{"name":"a"}}""", """{"user":{"name":"b"}}""");
		var c = Assert.Single(changes);
		Assert.Equal("/user/name", c.Path);
	}

	/// <summary>
	/// Tests that array elements are compared by their index positions.
	/// </summary>
	[Fact]
	public void Arrays_DiffByIndex()
	{
		var changes = JsonDiffer.Diff("""[1,2,3]""", """[1,9,3]""");
		var c = Assert.Single(changes);
		Assert.Equal(ChangeKind.Changed, c.Kind);
		Assert.Equal("/1", c.Path);
	}

	/// <summary>
	/// Tests that removing elements from the end of an array is detected.
	/// </summary>
	[Fact]
	public void ShorterArray_ReportsRemovedTail()
	{
		var changes = JsonDiffer.Diff("""[1,2,3]""", """[1,2]""");
		var c = Assert.Single(changes);
		Assert.Equal(ChangeKind.Removed, c.Kind);
		Assert.Equal("/2", c.Path);
	}

	/// <summary>
	/// Tests that adding elements to the end of an array is detected.
	/// </summary>
	[Fact]
	public void LongerArray_ReportsAddedTail()
	{
		var changes = JsonDiffer.Diff("""[1]""", """[1,2,3]""");
		Assert.Equal(2, changes.Count);
		Assert.All(changes, c => Assert.Equal(ChangeKind.Added, c.Kind));
	}

	/// <summary>
	/// Tests that numeric tolerance treats equivalent numbers (1 vs 1.0) as equal.
	/// </summary>
	[Fact]
	public void NumericTolerance_TreatsEquivalentNumbersAsEqual()
	{
		var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":1.0}""");
		Assert.Empty(changes);
	}

	/// <summary>
	/// Tests that disabling numeric tolerance reports raw text differences between numbers.
	/// </summary>
	[Fact]
	public void NumericTolerance_Off_ReportsRawTextDifference()
	{
		var opts = new DiffOptions { NumericTolerance = false };
		var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":1.0}""", opts);
		Assert.Single(changes);
	}

	/// <summary>
	/// Tests that property name case differences can be ignored when configured.
	/// </summary>
	[Fact]
	public void IgnorePropertyCase_MatchesRegardlessOfCase()
	{
		var opts = new DiffOptions { IgnorePropertyCase = true };
		var changes = JsonDiffer.Diff("""{"Name":"a"}""", """{"name":"a"}""", opts);
		Assert.Empty(changes);
	}

	/// <summary>
	/// Tests that path segments containing forward slashes are properly escaped.
	/// </summary>
	[Fact]
	public void PathSegmentsWithSlash_AreEscaped()
	{
		var changes = JsonDiffer.Diff("""{"a/b":1}""", """{"a/b":2}""");
		var c = Assert.Single(changes);
		Assert.Equal("/a~1b", c.Path);
	}

	/// <summary>
	/// Tests that the ToString method formats change information correctly.
	/// </summary>
	[Fact]
	public void ToString_FormatsChange()
	{
		var changes = JsonDiffer.Diff("""{"a":1}""", """{"a":2}""");
		Assert.Equal("~ /a: 1 -> 2", changes.Single().ToString());
	}
}