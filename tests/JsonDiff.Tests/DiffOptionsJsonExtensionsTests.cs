using System.Text.Json;
using Xunit;

namespace JsonDiff.Tests;

/// <summary>
/// Test suite for DiffOptionsJsonExtensions serialization and deserialization methods.
/// Validates that DiffOptions can be correctly serialized to JSON and deserialized back.
/// </summary>
public class DiffOptionsJsonExtensionsTests
{
    /// <summary>
    /// Tests that ToJson serializes a DiffOptions instance to JSON.
    /// </summary>
    [Fact]
    public void ToJson_SerializesOptionsToJson()
    {
        // Arrange
        var options = new DiffOptions
        {
            NumericTolerance = false,
            IgnorePropertyCase = true,
            MaxDepth = 10,
            DetectArrayShifts = true,
            ArrayComparison = ArrayComparison.Unordered,
            ArrayKeySelector = "/id",
            MaxArrayShiftDetectionSize = 500,
            MaxChanges = 100
        };

        // Act
        var json = options.ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"numericTolerance\"", json);
        Assert.Contains("false", json);
        Assert.Contains("\"ignorePropertyCase\"", json);
        Assert.Contains("true", json);
        Assert.Contains("\"maxDepth\"", json);
        Assert.Contains("10", json);
    }

    /// <summary>
    /// Tests that ToJson with indented parameter produces formatted JSON.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedTrue_ProducesFormattedJson()
    {
        // Arrange
        var options = new DiffOptions { NumericTolerance = false };

        // Act
        var json = options.ToJson(indented: true);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\n", json); // Should contain newlines for formatting
        Assert.Contains("\"numericTolerance\":", json);
        Assert.Contains("false", json);
    }

    /// <summary>
    /// Tests that ToJson with indented parameter false produces compact JSON.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedFalse_ProducesCompactJson()
    {
        // Arrange
        var options = new DiffOptions { NumericTolerance = false };

        // Act
        var json = options.ToJson(indented: false);

        // Assert
        Assert.NotNull(json);
        Assert.DoesNotContain("\n", json); // Should not contain newlines
    }

    /// <summary>
    /// Tests that ToJson throws ArgumentNullException when value is null.
    /// </summary>
    [Fact]
    public void ToJson_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        DiffOptions? options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => options!.ToJson());
    }

    /// <summary>
    /// Tests that FromJson deserializes valid JSON back to DiffOptions.
    /// </summary>
    [Fact]
    public void FromJson_DeserializesValidJsonToOptions()
    {
        // Arrange
        var json = "{\"numericTolerance\":false,\"ignorePropertyCase\":true,\"maxDepth\":10}";

        // Act
        var options = DiffOptionsJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(options);
        Assert.False(options.NumericTolerance);
        Assert.True(options.IgnorePropertyCase);
        Assert.Equal(10, options.MaxDepth);
    }

    /// <summary>
    /// Tests that FromJson returns null for empty or whitespace JSON.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void FromJson_WithEmptyOrWhitespaceJson_ReturnsNull(string json)
    {
        // Act
        var options = DiffOptionsJsonExtensions.FromJson(json);

        // Assert
        Assert.Null(options);
    }

    /// <summary>
    /// Tests that FromJson throws ArgumentNullException when json is null.
    /// </summary>
    [Fact]
    public void FromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => DiffOptionsJsonExtensions.FromJson(null!));
    }

    /// <summary>
    /// Tests that FromJson throws JsonException for invalid JSON.
    /// </summary>
    [Fact]
    public void FromJson_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{invalid json";

        // Act & Assert
        Assert.Throws<JsonException>(() => DiffOptionsJsonExtensions.FromJson(invalidJson));
    }

    /// <summary>
    /// Tests that TryFromJson successfully deserializes valid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDeserializes()
    {
        // Arrange
        var json = "{\"numericTolerance\":false,\"ignorePropertyCase\":true}";

        // Act
        var result = DiffOptionsJsonExtensions.TryFromJson(json, out var options);

        // Assert
        Assert.True(result);
        Assert.NotNull(options);
        Assert.False(options!.NumericTolerance);
        Assert.True(options.IgnorePropertyCase);
    }

    /// <summary>
    /// Tests that TryFromJson returns false for invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "{invalid";

        // Act
        var result = DiffOptionsJsonExtensions.TryFromJson(invalidJson, out var options);

        // Assert
        Assert.False(result);
        Assert.Null(options);
    }

    /// <summary>
    /// Tests that TryFromJson throws ArgumentNullException when json is null.
    /// </summary>
    [Fact]
    public void TryFromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => DiffOptionsJsonExtensions.TryFromJson(null!, out _));
    }

    /// <summary>
    /// Tests that round-trip serialization and deserialization preserves all properties.
    /// </summary>
    [Fact]
    public void RoundTrip_SerializationPreservesAllProperties()
    {
        // Arrange - create options with all properties set
        var originalOptions = new DiffOptions
        {
            NumericTolerance = false,
            IgnorePropertyCase = true,
            MaxDepth = 100,
            DetectArrayShifts = true,
            ArrayComparison = ArrayComparison.KeyedBy,
            ArrayKeySelector = "/user/id",
            MaxArrayShiftDetectionSize = 2000,
            MaxChanges = 50
        };

        // Act - serialize and deserialize
        var json = originalOptions.ToJson();
        var deserializedOptions = DiffOptionsJsonExtensions.FromJson(json);

        // Assert - all properties should be preserved
        Assert.NotNull(deserializedOptions);
        Assert.False(deserializedOptions.NumericTolerance);
        Assert.True(deserializedOptions.IgnorePropertyCase);
        Assert.Equal(100, deserializedOptions.MaxDepth);
        Assert.True(deserializedOptions.DetectArrayShifts);
        Assert.Equal(ArrayComparison.KeyedBy, deserializedOptions.ArrayComparison);
        Assert.Equal("/user/id", deserializedOptions.ArrayKeySelector);
        Assert.Equal(2000, deserializedOptions.MaxArrayShiftDetectionSize);
        Assert.Equal(50, deserializedOptions.MaxChanges);
    }

    /// <summary>
    /// Tests that default DiffOptions can be serialized and deserialized.
    /// </summary>
    [Fact]
    public void RoundTrip_DefaultOptions_SerializesAndDeserializes()
    {
        // Arrange
        var originalOptions = new DiffOptions();

        // Act
        var json = originalOptions.ToJson();
        var deserializedOptions = DiffOptionsJsonExtensions.FromJson(json);

        // Assert - default values should be preserved
        Assert.NotNull(deserializedOptions);
        Assert.True(deserializedOptions.NumericTolerance); // default
        Assert.False(deserializedOptions.IgnorePropertyCase); // default
        Assert.Null(deserializedOptions.MaxDepth); // default
        Assert.False(deserializedOptions.DetectArrayShifts); // default
        Assert.Equal(ArrayComparison.Ordered, deserializedOptions.ArrayComparison); // default
        Assert.Null(deserializedOptions.ArrayKeySelector); // default
        Assert.Equal(1000, deserializedOptions.MaxArrayShiftDetectionSize); // default
        Assert.Null(deserializedOptions.MaxChanges); // default
    }

    /// <summary>
    /// Tests that TryFromJson with invalid JSON sets out parameter to null.
    /// </summary>
    [Fact]
    public void TryFromJson_InvalidJson_SetsOutParameterToNull()
    {
        // Arrange
        var invalidJson = "not valid json";

        // Act
        var result = DiffOptionsJsonExtensions.TryFromJson(invalidJson, out var options);

        // Assert
        Assert.False(result);
        Assert.Null(options);
    }
}