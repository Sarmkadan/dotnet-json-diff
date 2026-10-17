## Getting Started

### Install

```bash
dotnet add package JsonDiff
```

### Diff two JSON strings

```csharp
using JsonDiff;

var left = """{"name":"Ada","age":36,"tags":["a","b"]}""";
var right = """{"name":"Ada","age":37,"tags":["a","b","c"]}""";

IReadOnlyList<JsonChange> changes = JsonDiffer.Diff(left, right);
foreach (var change in changes)
    Console.WriteLine(change);

// ~ /age: 36 -> 37
// + /tags/2: "c"
```

Each `JsonChange` has a `Kind` (`ChangeKind.Added`, `Removed`, `Changed`), a JSON Pointer `Path`, and the `Left` / `Right` values.

### Customize the diff with `DiffOptions`

```csharp
using JsonDiff;

var before = """{"Items":[{"id":"x","qty":1.0},{"id":"y","qty":2}]}""";
var after  = """{"items":[{"id":"y","qty":2.0},{"id":"x","qty":3}]}""";

var options = new DiffOptions
{
    IgnorePropertyCase = true,                    // "Items" matches "items"
    NumericTolerance = true,                      // 2 and 2.0 are equal
    ArrayComparison = ArrayComparison.KeyedBy,    // match array elements by key
    ArrayKeySelector = "/id",                     // ...using their "id" property
};

foreach (var change in JsonDiffer.Diff(before, after, options))
    Console.WriteLine(change);

// ~ /Items/0/qty: 1.0 -> 3
```

Other options (`MaxDepth`, `MaxChanges`, `MaxNodeCount`, `DetectArrayShifts`, `NumberComparison`, ...) are documented on `DiffOptions`.

### Export a diff as a JSON Patch (RFC 6902)

JsonDiff produces patches but does not apply them. To apply the output to a document, pass it to an RFC 6902 implementation.

```csharp
using JsonDiff;

var left = """{"name":"Ada","age":36,"tags":["a","b"]}""";
var right = """{"name":"Ada","age":37,"tags":["a","b","c"]}""";

var changes = JsonDiffer.Diff(left, right);
string patch = JsonPatchFormatter.ToJsonPatch(changes, indented: true);
Console.WriteLine(patch);

// [
//   {
//     "op": "replace",
//     "path": "/age",
//     "value": 37
//   },
//   {
//     "op": "add",
//     "path": "/tags/2",
//     "value": "c"
//   }
// ]
```

### Export a diff as a JSON Merge Patch (RFC 7386)

Removed properties become `null`. Merge Patch replaces arrays as whole values, so a change to an individual array element throws `NotSupportedException`.

```csharp
using JsonDiff;

var before = """{"name":"Ada","age":36,"role":"admin"}""";
var after  = """{"name":"Ada","age":37}""";

var changes = JsonDiffer.Diff(before, after);
string mergePatch = JsonMergePatchFormatter.ToJsonMergePatch(changes);
Console.WriteLine(mergePatch);

// {"age":37,"role":null}
```

### Check deep equality

`JsonDiffer.DeepEquals` returns a `bool` and stops at the first difference. Key order is ignored, and numeric values compare equal by default (`NumericTolerance` defaults to `true`).

```csharp
using JsonDiff;

var a = """{"a":1,"b":{"c":true}}""";
var b = """{"b":{"c":true},"a":1.0}""";
var c = """{"a":1,"b":{"c":false}}""";

Console.WriteLine(JsonDiffer.DeepEquals(a, b));                              // True
Console.WriteLine(a.DeepEquals(b));                                          // True (extension method)
Console.WriteLine(JsonDiffer.DeepEquals(a, c));                              // False
Console.WriteLine(a.DeepEquals(b, new DiffOptions { NumericTolerance = false })); // False: 1 vs 1.0 is a change
```

## API overview

| Class | Purpose |
|---|---|
| `JsonDiffer` | Static entry points: `Diff` lists the changes between two documents; `DeepEquals` checks whether they are semantically equal. |
| `JsonChange` | Readonly record struct for one change: `Kind`, JSON Pointer `Path`, and the `Left` / `Right` values. |
| `DiffOptions` | Immutable options (`init` properties) for comparison behavior and limits. |
| `JsonPatchFormatter` | Renders a list of changes as an RFC 6902 JSON Patch document. |
| `JsonMergePatchFormatter` | Renders a list of changes as an RFC 7386 JSON Merge Patch document. |

## DeepEqualsTests

The DeepEqualsTests class contains tests for the DeepEquals method, which checks for deep equality between two JSON objects. It verifies that the method correctly handles various scenarios, such as identical documents, key order differences, added or removed properties, and more.

Example usage:
```csharp
public void IdenticalDocuments_AreEqual
public void KeyOrder_IsIgnored
public void AddedProperty_MakesNotEqual
public void RemovedProperty_MakesNotEqual
public void ChangedScalar_MakesNotEqual
public void KindChange_MakesNotEqual
public void NestedPropertyDifference_IsDetected
public void ArrayElementDifference_IsDetected
public void ArrayLengthDifference_IsDetected
public void NumericTolerance_TreatsEquivalentNumbersAsEqual
public void NumericTolerance_Off_ReportsEquivalentNumbersAsDifferent
public void IgnorePropertyCase_MatchesRegardlessOfCase
public void PropertyCaseDifference_IsDetected_WhenNotConfigured
public void NullValues_AreEqual
public void BooleanValues_AreEqual
public void StringValues_AreEqual
public void DeepEquals_JsonElementOverload_WorksCorrectly
public void DeepEquals_JsonElementOverload_DetectsDifferences
public void MaxDepth_Limited_AllowsTraversalToDepth2
public void MaxDepth_WithLimit1_ComparesAtRootLevel
```
## ScalarEdgeCaseTests

ScalarEdgeCaseTests contains tests for edge cases related to scalar values in JSON patches.

Example usage:
```csharp
public void NumericTolerance_True_1_vs_1dot0_NoChanges
public void NumericTolerance_False_1_vs_1dot0_Changed
public void NumericTolerance_True_LargeNumbers_NoChanges
public void NumericTolerance_False_LargeNumbers_Changed
public void StringCaseSensitivity_Changed
public void BooleanTrueVsFalse_Changed
public void NullVsMissingProperty_Removed
public void MissingPropertyVsNull_Added
public void EmptyObjectVsEmptyArray_Changed
```
## JsonPatchFormatterTestsExtensions

The JsonPatchFormatterTestsExtensions class provides extension methods for creating and asserting on JSON Patch operations in tests. It simplifies the process of generating JSON Patch strings from changes and validating the resulting operations, such as checking operation count, extracting paths and values, and asserting on specific operation types and values.

Example usage:
```csharp
// Arrange: set up a test instance and some changes
var test = new JsonPatchFormatterTests();
var changes = new List<JsonChange> { /* ... */ };

// Act: generate the JSON Patch and get the document
var (json, document) = test.ToJsonPatchWithDocument(changes, "add");

// Assert: check the number of operations
document.HasOperationCount(1);

// Get the first operation and assert its properties
var operation = document[0];
Assert.AreEqual("/test/path", test.GetPath(operation));
test.HasValue<int>(operation, 42);
```
