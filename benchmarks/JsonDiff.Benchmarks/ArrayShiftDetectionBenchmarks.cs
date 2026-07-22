using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using JsonDiff;

namespace JsonDiff.Benchmarks;

/// <summary>
/// Benchmarks for array shift detection optimization.
/// Tests performance with large arrays (10k elements) with insertions at different positions.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ArrayShiftDetectionBenchmarks
{
    private const int ArraySize = 10_000;
    private string _baseJson = string.Empty;
    private string _insertAtStart = string.Empty;
    private string _insertAtMiddle = string.Empty;
    private string _insertAtEnd = string.Empty;
    private DiffOptions _optionsWithShiftDetection;
    private DiffOptions _optionsWithSizeLimit;
    private DiffOptions _optionsWithoutShiftDetection;

    [GlobalSetup]
    public void Setup()
    {
        // Create base array with 10k elements
        var baseArray = new JsonElement[ArraySize];
        for (int i = 0; i < ArraySize; i++)
        {
            baseArray[i] = JsonSerializer.SerializeToElement($"element_{i}");
        }
        _baseJson = JsonSerializer.Serialize(baseArray);

        // Insert at start (shift detection should detect this as a shift)
        var insertAtStartArray = new JsonElement[ArraySize + 1];
        insertAtStartArray[0] = JsonSerializer.SerializeToElement("new_element");
        Array.Copy(baseArray, 0, insertAtStartArray, 1, ArraySize);
        _insertAtStart = JsonSerializer.Serialize(insertAtStartArray);

        // Insert in middle (shift detection should detect this as a shift)
        var insertAtMiddleArray = new JsonElement[ArraySize + 1];
        Array.Copy(baseArray, 0, insertAtMiddleArray, 0, ArraySize / 2);
        insertAtMiddleArray[ArraySize / 2] = JsonSerializer.SerializeToElement("new_element");
        Array.Copy(baseArray, ArraySize / 2, insertAtMiddleArray, ArraySize / 2 + 1, ArraySize - ArraySize / 2);
        _insertAtMiddle = JsonSerializer.Serialize(insertAtMiddleArray);

        // Insert at end (shift detection should detect this as a shift)
        var insertAtEndArray = new JsonElement[ArraySize + 1];
        Array.Copy(baseArray, insertAtEndArray, ArraySize);
        insertAtEndArray[ArraySize] = JsonSerializer.SerializeToElement("new_element");
        _insertAtEnd = JsonSerializer.Serialize(insertAtEndArray);

        // Configure options
        _optionsWithShiftDetection = new DiffOptions
        {
            DetectArrayShifts = true,
            ArrayComparison = ArrayComparison.Ordered
        };

        _optionsWithSizeLimit = new DiffOptions
        {
            DetectArrayShifts = true,
            MaxArrayShiftDetectionSize = 1000,
            ArrayComparison = ArrayComparison.Ordered
        };

        _optionsWithoutShiftDetection = new DiffOptions
        {
            DetectArrayShifts = false,
            ArrayComparison = ArrayComparison.Ordered
        };
    }

    [Benchmark]
    public void ArrayShiftDetection_WithSizeLimit_InsertAtStart()
    {
        JsonDiffer.Diff(_baseJson, _insertAtStart, _optionsWithSizeLimit);
    }

    [Benchmark]
    public void ArrayShiftDetection_WithSizeLimit_InsertAtMiddle()
    {
        JsonDiffer.Diff(_baseJson, _insertAtMiddle, _optionsWithSizeLimit);
    }

    [Benchmark]
    public void ArrayShiftDetection_WithSizeLimit_InsertAtEnd()
    {
        JsonDiffer.Diff(_baseJson, _insertAtEnd, _optionsWithSizeLimit);
    }

    [Benchmark]
    public void ArrayShiftDetection_WithoutSizeLimit_InsertAtStart()
    {
        JsonDiffer.Diff(_baseJson, _insertAtStart, _optionsWithShiftDetection);
    }

    [Benchmark]
    public void ArrayShiftDetection_WithoutShiftDetection_InsertAtStart()
    {
        JsonDiffer.Diff(_baseJson, _insertAtStart, _optionsWithoutShiftDetection);
    }

    [Benchmark]
    public void ArrayShiftDetection_UnorderedComparison_LargeArray()
    {
        // Test unordered comparison with large arrays to ensure it's efficient
        var options = new DiffOptions
        {
            ArrayComparison = ArrayComparison.Unordered
        };

        JsonDiffer.Diff(_baseJson, _insertAtStart, options);
    }

    [Benchmark]
    public void ArrayShiftDetection_KeyedComparison_LargeArray()
    {
        // Create arrays with objects that have an "id" property
        var baseArray = new JsonElement[ArraySize];
        for (int i = 0; i < ArraySize; i++)
        {
            baseArray[i] = JsonSerializer.SerializeToElement(new { id = i, value = $"element_{i}" });
        }
        var baseJson = JsonSerializer.Serialize(baseArray);

        var insertArray = new JsonElement[ArraySize + 1];
        insertArray[0] = JsonSerializer.SerializeToElement(new { id = -1, value = "new_element" });
        Array.Copy(baseArray, 0, insertArray, 1, ArraySize);
        var insertJson = JsonSerializer.Serialize(insertArray);

        var options = new DiffOptions
        {
            ArrayComparison = ArrayComparison.KeyedBy,
            ArrayKeySelector = "/id"
        };

        JsonDiffer.Diff(baseJson, insertJson, options);
    }

    /// <summary>
    /// Entry point for running benchmarks manually.
    /// </summary>
    public static void Run()
    {
        var summary = BenchmarkRunner.Run<ArrayShiftDetectionBenchmarks>();
    }
}