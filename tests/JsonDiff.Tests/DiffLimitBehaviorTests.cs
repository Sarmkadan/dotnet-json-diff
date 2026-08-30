using System.Text.Json;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Tests limit boundaries and option validation for <see cref="JsonDiffer"/>.
/// </summary>
public class DiffLimitBehaviorTests
{
    [Fact]
    public void Diff_MoreChangesThanConfiguredLimit_ThrowsLimitExceededException()
    {
        const string left = "{\"a\":1,\"b\":2,\"c\":3}";
        const string right = "{\"a\":4,\"b\":5,\"c\":6}";
        var options = new DiffOptions { MaxChanges = 2, MaxNodeCount = 1 };

        Assert.Throws<JsonDiffLimitExceededException>(() => JsonDiffer.Diff(left, right, options));
    }

    [Fact]
    public void Diff_MaxNodeCountExceededByWideDocument_ThrowsLimitExceededException()
    {
        const string left = "{\"a\":{\"value\":1},\"b\":{\"value\":2},\"c\":{\"value\":3}}";
        var options = new DiffOptions { MaxNodeCount = 2 };

        var exception = Assert.Throws<JsonDiffLimitExceededException>(
            () => JsonDiffer.Diff(left, left, options));

        Assert.Contains("maximum node count limit of 2", exception.Message);
    }

    [Fact]
    public void Diff_MaxDepthAtBoundary_DoesNotThrow()
    {
        var options = new DiffOptions { MaxDepth = 2 };

        var changes = JsonDiffer.Diff(
            "{\"level1\":{\"level2\":1}}",
            "{\"level1\":{\"level2\":2}}",
            options);

        var change = Assert.Single(changes);
        Assert.Equal("/level1/level2", change.Path);
    }

    [Fact]
    public void Diff_DifferenceBeyondMaxDepth_IsReportedAtBoundary()
    {
        var options = new DiffOptions { MaxDepth = 2 };

        var changes = JsonDiffer.Diff(
            "{\"level1\":{\"level2\":{\"value\":1}}}",
            "{\"level1\":{\"level2\":{\"value\":2}}}",
            options);

        var change = Assert.Single(changes);
        Assert.Equal("/level1/level2", change.Path);
    }

    [Fact]
    public void Diff_MaxDepthZero_ThrowsArgumentOutOfRangeException()
    {
        var options = new DiffOptions { MaxDepth = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => JsonDiffer.Diff("{}", "{}", options));
    }

    [Fact]
    public void Diff_NegativeMaxNodeCount_ThrowsArgumentOutOfRangeException()
    {
        var options = new DiffOptions { MaxNodeCount = -1 };

        Assert.Throws<ArgumentOutOfRangeException>(() => JsonDiffer.Diff("{}", "{}", options));
    }

    [Fact]
    public void Diff_MaxChangesZero_ThrowsArgumentOutOfRangeException()
    {
        var options = new DiffOptions { MaxChanges = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => JsonDiffer.Diff("{}", "{}", options));
    }

    [Fact]
    public void DeepEquals_StringOverload_MaxNodeCountExceeded_ThrowsLimitExceededException()
    {
        const string json = "{\"outer\":{\"inner\":1}}";
        var options = new DiffOptions { MaxNodeCount = 1 };

        Assert.Throws<JsonDiffLimitExceededException>(() => JsonDiffer.DeepEquals(json, json, options));
    }

    [Fact]
    public void DeepEquals_JsonElementOverload_MaxNodeCountExceeded_ThrowsLimitExceededException()
    {
        using var document = JsonDocument.Parse("{\"outer\":{\"inner\":1}}");
        var options = new DiffOptions { MaxNodeCount = 1 };

        Assert.Throws<JsonDiffLimitExceededException>(
            () => JsonDiffer.DeepEquals(document.RootElement, document.RootElement, options));
    }

    [Fact]
    public void DeepEquals_DifferenceBeyondMaxDepth_ReturnsFalseWithoutThrowing()
    {
        var options = new DiffOptions { MaxDepth = 2 };

        var equal = JsonDiffer.DeepEquals(
            "{\"level1\":{\"level2\":{\"value\":1}}}",
            "{\"level1\":{\"level2\":{\"value\":2}}}",
            options);

        Assert.False(equal);
    }

    [Fact]
    public void DeepEquals_InvalidMaxChanges_ThrowsArgumentOutOfRangeException()
    {
        var options = new DiffOptions { MaxChanges = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => JsonDiffer.DeepEquals("{}", "{}", options));
    }

    [Fact]
    public void Diff_MalformedJson_WrapsJsonException()
    {
        var exception = Assert.Throws<JsonDiffException>(() => JsonDiffer.Diff("{", "{}"));

        Assert.IsType<JsonException>(exception.InnerException);
    }
}
