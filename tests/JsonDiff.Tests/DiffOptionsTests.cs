using System.Text.Json;
using JsonDiff;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Tests fail-fast validation of <see cref="DiffOptions"/> and that entry points reject invalid options before parsing input.
/// </summary>
public class DiffOptionsTests
{
    [Fact]
    public void Default_Validate_DoesNotThrow()
    {
        // Arrange
        var options = new DiffOptions();

        // Act
        var ex = Record.Exception(options.Validate);

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void Default_Diff_ReturnsChangesWithoutThrowing()
    {
        // Arrange
        const string left = "{\"a\":1}";
        const string right = "{\"a\":2}";

        // Act
        var changes = JsonDiffer.Diff(left, right, null);

        // Assert
        Assert.Single(changes);
        Assert.Equal("/a", changes[0].Path);
    }

    [Fact]
    public void KeyedBy_WithKeySelector_DoesNotThrow()
    {
        // Arrange
        var options = new DiffOptions { ArrayComparison = ArrayComparison.KeyedBy, ArrayKeySelector = "/id" };

        // Act
        var ex = Record.Exception(options.Validate);

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void DetectArrayShifts_WithOrdered_DoesNotThrow()
    {
        // Arrange
        var options = new DiffOptions { DetectArrayShifts = true, ArrayComparison = ArrayComparison.Ordered };

        // Act
        var ex = Record.Exception(options.Validate);

        // Assert
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxDepth_NotPositive_ThrowsArgumentOutOfRangeExceptionWithPropertyName(int value)
    {
        // Arrange
        var options = new DiffOptions { MaxDepth = value };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.MaxDepth), ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxNodeCount_NotPositive_ThrowsArgumentOutOfRangeExceptionWithPropertyName(int value)
    {
        // Arrange
        var options = new DiffOptions { MaxNodeCount = value };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.MaxNodeCount), ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxChanges_NotPositive_ThrowsArgumentOutOfRangeExceptionWithPropertyName(int value)
    {
        // Arrange
        var options = new DiffOptions { MaxChanges = value };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.MaxChanges), ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void MaxArrayShiftDetectionSize_NotPositive_ThrowsArgumentOutOfRangeExceptionWithPropertyName(int value)
    {
        // Arrange
        var options = new DiffOptions { MaxArrayShiftDetectionSize = value };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.MaxArrayShiftDetectionSize), ex.ParamName);
    }

    [Fact]
    public void ArrayComparison_UndefinedValue_ThrowsArgumentOutOfRangeExceptionWithPropertyName()
    {
        // Arrange
        var options = new DiffOptions { ArrayComparison = (ArrayComparison)999 };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.ArrayComparison), ex.ParamName);
    }

    [Fact]
    public void NumberComparison_UndefinedValue_ThrowsArgumentOutOfRangeExceptionWithPropertyName()
    {
        // Arrange
        var options = new DiffOptions { NumberComparison = (NumberComparison)999 };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.NumberComparison), ex.ParamName);
    }

    [Fact]
    public void KeyedBy_NullKeySelector_ThrowsArgumentExceptionWithPropertyName()
    {
        // Arrange
        var options = new DiffOptions { ArrayComparison = ArrayComparison.KeyedBy, ArrayKeySelector = null };

        // Act
        var ex = Assert.Throws<ArgumentException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.ArrayKeySelector), ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void KeyedBy_BlankKeySelector_ThrowsArgumentExceptionWithPropertyName(string selector)
    {
        // Arrange
        var options = new DiffOptions { ArrayComparison = ArrayComparison.KeyedBy, ArrayKeySelector = selector };

        // Act
        var ex = Assert.Throws<ArgumentException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.ArrayKeySelector), ex.ParamName);
    }

    [Theory]
    [InlineData(ArrayComparison.Ordered)]
    [InlineData(ArrayComparison.Unordered)]
    public void ArrayKeySelector_WithoutKeyedBy_ThrowsArgumentExceptionWithPropertyName(ArrayComparison comparison)
    {
        // Arrange
        var options = new DiffOptions { ArrayComparison = comparison, ArrayKeySelector = "/id" };

        // Act
        var ex = Assert.Throws<ArgumentException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.ArrayKeySelector), ex.ParamName);
    }

    [Theory]
    [InlineData(ArrayComparison.Unordered)]
    [InlineData(ArrayComparison.KeyedBy)]
    public void DetectArrayShifts_WithNonOrderedComparison_ThrowsArgumentExceptionWithPropertyName(ArrayComparison comparison)
    {
        // Arrange
        var options = new DiffOptions
        {
            DetectArrayShifts = true,
            ArrayComparison = comparison,
            ArrayKeySelector = comparison == ArrayComparison.KeyedBy ? "/id" : null,
        };

        // Act
        var ex = Assert.Throws<ArgumentException>(options.Validate);

        // Assert
        Assert.Equal(nameof(DiffOptions.DetectArrayShifts), ex.ParamName);
    }

    [Fact]
    public void Diff_InvalidOptions_ThrowsBeforeParsingInput()
    {
        // Arrange: the input is not valid JSON, so only up-front validation can produce the expected exception
        var options = new DiffOptions { MaxDepth = 0 };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => JsonDiffer.Diff("not json", "{", options));

        // Assert
        Assert.Equal(nameof(DiffOptions.MaxDepth), ex.ParamName);
    }

    [Fact]
    public void DeepEquals_InvalidOptions_ThrowsBeforeParsingInput()
    {
        // Arrange
        var options = new DiffOptions { ArrayComparison = ArrayComparison.KeyedBy };

        // Act
        var ex = Assert.Throws<ArgumentException>(() => JsonDiffer.DeepEquals("not json", "{", options));

        // Assert
        Assert.Equal(nameof(DiffOptions.ArrayKeySelector), ex.ParamName);
    }

    [Fact]
    public void Diff_JsonElementOverload_InvalidOptions_ThrowsArgumentException()
    {
        // Arrange
        using var left = JsonDocument.Parse("{\"a\":1}");
        using var right = JsonDocument.Parse("{\"a\":1}");
        var options = new DiffOptions { DetectArrayShifts = true, ArrayComparison = ArrayComparison.Unordered };

        // Act
        var ex = Assert.Throws<ArgumentException>(() => JsonDiffer.Diff(left.RootElement, right.RootElement, options));

        // Assert
        Assert.Equal(nameof(DiffOptions.DetectArrayShifts), ex.ParamName);
    }

    [Fact]
    public void Diff_ExtensionBytesOverload_InvalidOptions_ThrowsBeforeParsingInput()
    {
        // Arrange: empty input would normally short-circuit, so validation must run first
        var options = new DiffOptions { NumberComparison = (NumberComparison)999 };

        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => JsonDifferExtensions.Diff(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, options));

        // Assert
        Assert.Equal(nameof(DiffOptions.NumberComparison), ex.ParamName);
    }
}
