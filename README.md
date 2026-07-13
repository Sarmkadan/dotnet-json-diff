# JsonDiff

A tiny semantic diff for JSON. It compares two documents by structure and value -
**object key order does not matter** - and reports exactly what was added, removed
or changed, each anchored to a path.

Built on `System.Text.Json`, no third-party dependencies, single small assembly.

## Why

`string.Equals` on serialized JSON is noise: reorder two keys and everything looks
"changed". A byte diff has the same problem. JsonDiff answers the question you
usually actually have - *what changed semantically?* - and gives you a flat list
of changes you can log, assert on in tests, or render.

## Install

```
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

You can also diff already-parsed elements:

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

## Semantics

- **Objects** are matched by property name, order-independent.
- **Arrays** are compared positionally (by index). Extra tail elements read as
  `Added`/`Removed`. Reordering an array *is* reported - order is meaningful in
  arrays, unlike object keys.
- A node whose JSON kind changes (e.g. object becomes a number) is reported as a
  single `Changed` at that path, without descending.

## Options

```csharp
var opts = new DiffOptions
{
    NumericTolerance   = true,  // 1 == 1.0 == 1e0 (default true)
    IgnorePropertyCase = false, // match "Name" and "name" (default false)
};
JsonDiffer.Diff(left, right, opts);
```

## Building

```
dotnet build
dotnet test
```

## License

MIT
