using System.Text.Json;

namespace JsonDiff.Tests;

public class JsonChangeExtensionsTests
{
    [Fact]
    public void HasChanges_WithEmptyCollection_ReturnsFalse()
    {
        // Arrange
        var changes = Array.Empty<JsonChange>();

        // Act
        var result = changes.HasChanges();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasChanges_WithNonEmptyCollection_ReturnsTrue()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Added, "/a", null, JsonSerializer.SerializeToElement("value")),
            new JsonChange(ChangeKind.Removed, "/b", JsonSerializer.SerializeToElement("old"), null)
        };

        // Act
        var result = changes.HasChanges();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OfKind_WithMatchingChanges_ReturnsOnlyMatching()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Added, "/a", null, JsonSerializer.SerializeToElement("1")),
            new JsonChange(ChangeKind.Removed, "/b", JsonSerializer.SerializeToElement("2"), null),
            new JsonChange(ChangeKind.Added, "/c", null, JsonSerializer.SerializeToElement("3")),
            new JsonChange(ChangeKind.Changed, "/d", JsonSerializer.SerializeToElement("4"), JsonSerializer.SerializeToElement("5"))
        };

        // Act
        var addedChanges = changes.OfKind(ChangeKind.Added).ToList();
        var removedChanges = changes.OfKind(ChangeKind.Removed).ToList();
        var changedChanges = changes.OfKind(ChangeKind.Changed).ToList();
        var movedChanges = changes.OfKind(ChangeKind.Moved).ToList();

        // Assert
        Assert.Equal(2, addedChanges.Count);
        Assert.Equal(ChangeKind.Added, addedChanges[0].Kind);
        Assert.Equal(ChangeKind.Added, addedChanges[1].Kind);

        Assert.Single(removedChanges);
        Assert.Equal(ChangeKind.Removed, removedChanges[0].Kind);

        Assert.Single(changedChanges);
        Assert.Equal(ChangeKind.Changed, changedChanges[0].Kind);

        Assert.Empty(movedChanges);
    }

    [Fact]
    public void OfKind_WithNoMatchingChanges_ReturnsEmpty()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Added, "/a", null, JsonSerializer.SerializeToElement("1")),
            new JsonChange(ChangeKind.Added, "/b", null, JsonSerializer.SerializeToElement("2"))
        };

        // Act
        var result = changes.OfKind(ChangeKind.Removed);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UnderPath_WithExactMatch_ReturnsMatchingChange()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Changed, "/user/name", JsonSerializer.SerializeToElement("old"), JsonSerializer.SerializeToElement("new")),
            new JsonChange(ChangeKind.Added, "/age", null, JsonSerializer.SerializeToElement(30))
        };

        // Act
        var result = changes.UnderPath("/user/name").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("/user/name", result[0].Path);
    }

    [Fact]
    public void UnderPath_WithPrefixMatch_ReturnsMatchingChanges()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Changed, "/user/name", JsonSerializer.SerializeToElement("old"), JsonSerializer.SerializeToElement("new")),
            new JsonChange(ChangeKind.Changed, "/user/age", JsonSerializer.SerializeToElement(25), JsonSerializer.SerializeToElement(30)),
            new JsonChange(ChangeKind.Added, "/address/street", null, JsonSerializer.SerializeToElement("Main St")),
            new JsonChange(ChangeKind.Removed, "/oldField", JsonSerializer.SerializeToElement("old"), null),
            new JsonChange(ChangeKind.Changed, "/settings/theme", JsonSerializer.SerializeToElement("light"), JsonSerializer.SerializeToElement("dark"))
        };

        // Act
        var result = changes.UnderPath("/user").ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("/user/name", result[0].Path);
        Assert.Equal("/user/age", result[1].Path);
    }

    [Fact]
    public void UnderPath_WithRootPath_ReturnsAllChanges()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Added, "/a", null, JsonSerializer.SerializeToElement("1")),
            new JsonChange(ChangeKind.Removed, "/b", JsonSerializer.SerializeToElement("2"), null),
            new JsonChange(ChangeKind.Changed, "/c", JsonSerializer.SerializeToElement("3"), JsonSerializer.SerializeToElement("4"))
        };

        // Act
        var result = changes.UnderPath("/").ToList();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void UnderPath_WithNonMatchingPrefix_DoesNotReturnChanges()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Changed, "/user/name", JsonSerializer.SerializeToElement("old"), JsonSerializer.SerializeToElement("new")),
            new JsonChange(ChangeKind.Added, "/age", null, JsonSerializer.SerializeToElement(30))
        };

        // Act
        var result = changes.UnderPath("/settings").ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void UnderPath_WithPartialSegmentMatch_DoesNotMatch()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Changed, "/ab", JsonSerializer.SerializeToElement("1"), JsonSerializer.SerializeToElement("2")),
            new JsonChange(ChangeKind.Changed, "/abc", JsonSerializer.SerializeToElement("3"), JsonSerializer.SerializeToElement("4")),
            new JsonChange(ChangeKind.Changed, "/a/b", JsonSerializer.SerializeToElement("5"), JsonSerializer.SerializeToElement("6"))
        };

        // Act
        var result = changes.UnderPath("/a").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("/a/b", result[0].Path);
    }

    [Fact]
    public void UnderPath_WithTrailingSlash_ReturnsMatchingChanges()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Changed, "/user/name", JsonSerializer.SerializeToElement("old"), JsonSerializer.SerializeToElement("new")),
            new JsonChange(ChangeKind.Added, "/user/age", null, JsonSerializer.SerializeToElement(30))
        };

        // Act
        var result = changes.UnderPath("/user/").ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ToSummaryString_WithEmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var changes = Array.Empty<JsonChange>();

        // Act
        var result = changes.ToSummaryString();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToSummaryString_WithSingleChange_ReturnsFormattedString()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Changed, "/a/b", JsonSerializer.SerializeToElement(1), JsonSerializer.SerializeToElement(2))
        };

        // Act
        var result = changes.ToSummaryString();

        // Assert
        Assert.Equal("~ /a/b: 1 -> 2", result);
    }

    [Fact]
    public void ToSummaryString_WithMultipleChanges_ReturnsMultiLineString()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Added, "/a", null, JsonSerializer.SerializeToElement("1")),
            new JsonChange(ChangeKind.Removed, "/b", JsonSerializer.SerializeToElement("2"), null),
            new JsonChange(ChangeKind.Changed, "/c", JsonSerializer.SerializeToElement(3), JsonSerializer.SerializeToElement(4))
        };

        // Act
        var result = changes.ToSummaryString();

        // Assert
        var lines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("+ /a", lines[0]);
        Assert.StartsWith("- /b", lines[1]);
        Assert.StartsWith("~ /c", lines[2]);
    }

    [Fact]
    public void ToSummaryString_WithMixedChanges_ReturnsAllChanges()
    {
        // Arrange
        var changes = new JsonChange[]
        {
            new JsonChange(ChangeKind.Moved, "/array/1", JsonSerializer.SerializeToElement("item"), JsonSerializer.SerializeToElement("item")),
            new JsonChange(ChangeKind.Added, "/newProp", null, JsonSerializer.SerializeToElement("value")),
            new JsonChange(ChangeKind.Removed, "/oldProp", JsonSerializer.SerializeToElement("old"), null)
        };

        // Act
        var result = changes.ToSummaryString();

        // Assert
        Assert.Contains("+ /newProp", result);
        Assert.Contains("- /oldProp", result);
        Assert.Contains("→ /array/1", result);
    }
}
