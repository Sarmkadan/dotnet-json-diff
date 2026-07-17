# JsonDiff

A tiny semantic diff for JSON. It compares two documents by structure and value -
**object key order does not matter** - and reports exactly what was added, removed
or changed, each anchored to a path.

Built on `System.Text.Json`, no third‑party dependencies, single small assembly.

## Why

`string.Equals` on serialized JSON is noise: reorder two keys and everything looks
"changed". A byte diff has the same problem. JsonDiff answers the question you
usually actually have - *what changed semantically?* - and gives you a flat list
of changes you can log, assert on in tests, or render.

## Install

```bash
dotnet add package JsonDiff
```

## Usage

```csharp
using JsonDiff;

var left  = """{ "user": { "name": "ann", "roles": ["admin"] }, "active": true }""";
var right = """{ "active": true, "user": { "roles": ["admin", "ops"], "name": "ann" } }""";

foreach (var change in JsonDiffer.Diff(left, right))
    Console.WriteLine(change);

// + /user/roles/1: "ops"
```

Key order (`active`/`user`, `name`/`roles`) is ignored; only the genuinely new
array element shows up.

You can also diff already‑parsed elements:

```csharp
using var l = JsonDocument.Parse(left);
using var r = JsonDocument.Parse(right);
IReadOnlyList<JsonChange> changes = JsonDiffer.Diff(l.RootElement, r.RootElement);
```

## Change model

Each `JsonChange` carries:

| Field   | Meaning                                                          |
|---------|-----------------------------------------------------------------|
| `Kind`  | `Added`, `Removed` or `Changed`                                 |
| `Path`  | RFC 6901-style pointer, e.g. `/user/roles/0`; root is `/`       |
| `Left`  | value on the left doc (`null` for `Added`)                      |
| `Right` | value on the right doc (`null` for `Removed`)                   |

## Options

```csharp
var opts = new DiffOptions
{
    NumericTolerance   = true,  // 1 == 1.0 == 1e0 (default true)
    IgnorePropertyCase = false, // match "Name" and "name" (default false)
};
JsonDiffer.Diff(left, right, opts);
```

## DiffTests

`DiffTests` is a test suite that verifies the behavior of `JsonDiffer`. It contains a collection of `[Fact]` methods that each assert a specific diff scenario, such as handling of identical documents, property order, added/removed properties, scalar changes, array handling, numeric tolerance, case‑insensitive property matching, escaped path segments, and the `ToString` formatting of `JsonChange`.

Example usage (e.g., in a console app or another test project) can invoke the individual test methods directly:

```csharp
using JsonDiff;
using Xunit;

var suite = new JsonDiff.Tests.DiffTests();

suite.IdenticalDocuments_ProduceNoChanges();
suite.KeyOrder_IsIgnored();
suite.AddedProperty_IsReported();
suite.RemovedProperty_IsReported();
suite.ChangedScalar_IsReported();
suite.KindChange_IsReportedWithoutDescent();
suite.NestedPaths_UseSlashSeparator();
suite.Arrays_DiffByIndex();
suite.ShorterArray_ReportsRemovedTail();
suite.LongerArray_ReportsAddedTail();
suite.NumericTolerance_TreatsEquivalentNumbersAsEqual();
suite.NumericTolerance_Off_ReportsRawTextDifference();
suite.IgnorePropertyCase_MatchesRegardlessOfCase();
suite.PathSegmentsWithSlash_AreEscaped();
suite.ToString_FormatsChange();
```

Running the above will execute the same assertions that the automated test runner performs.

## Architecture

The library is one static differ plus three small data types; the algorithm,
path semantics, design trade‑offs and known limitations are documented in
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Building

```bash
dotnet build
dotnet test
```

## License

MIT
