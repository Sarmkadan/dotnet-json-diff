# Architecture

## Overview

JsonDiff is a single small class library (`src/JsonDiff`) that computes a
semantic diff between two JSON documents and returns it as a flat list of
`JsonChange` records. It is built directly on `System.Text.Json`
(`JsonDocument` / `JsonElement`) and has no third-party dependencies. There is
no runtime, no DI, no I/O - the whole public surface is one static class plus
two small data types.

```
src/JsonDiff/
  JsonDiffer.cs    - the diff algorithm (static, stateless)
  JsonChange.cs    - one reported difference (readonly record struct)
  ChangeKind.cs    - Added | Removed | Changed
  DiffOptions.cs   - per-run tunables (NumericTolerance, IgnorePropertyCase)
tests/JsonDiff.Tests/
  DiffTests.cs     - xUnit tests covering the semantics below
```

## Data flow

1. `JsonDiffer.Diff(string, string, DiffOptions?)` parses both inputs with
   `JsonDocument.Parse` (throws `JsonException` on invalid JSON) and delegates
   to the `JsonElement` overload. The two `JsonDocument`s are disposed before
   returning; every element that ends up inside a `JsonChange` is `.Clone()`d
   first, so results outlive the parse buffers.
2. `Diff(JsonElement, JsonElement, DiffOptions?)` allocates a `List<JsonChange>`
   sink and recursively walks both trees in lockstep (`Walk`), appending a
   change whenever the trees disagree.
3. The walk dispatches on `JsonValueKind`:
   - **Kind mismatch** at a path (e.g. object vs number) -> a single `Changed`
     at that path, no descent into either subtree.
   - **Objects** (`WalkObject`) - the right side's properties are loaded into a
     `Dictionary<string, JsonElement>` keyed with `StringComparer.Ordinal` (or
     `OrdinalIgnoreCase` when `IgnorePropertyCase` is set). Left properties are
     matched against it: match -> recurse, miss -> `Removed`. A second pass over
     the right side reports properties not seen on the left as `Added`.
     Property order is therefore irrelevant by construction.
   - **Arrays** (`WalkArray`) - compared positionally by index. The common
     prefix recurses element-by-element; extra tail elements become
     `Removed`/`Added`. There is no move detection: reordering an array shows
     up as per-index `Changed` entries.
   - **Scalars** (`ScalarEquals`) - null/true/false are equal by kind alone;
     strings compare ordinally; numbers compare as `double` when
     `NumericTolerance` is on (so `1`, `1.0`, `1e0` are equal), otherwise by
     raw text.

## Paths

Paths are RFC 6901 (JSON Pointer) style: `/user/roles/0`, with `~` and `/` in
property names escaped as `~0`/`~1` (`Join` in `JsonDiffer.cs`, covered by
`PathSegmentsWithSlash_AreEscaped`). One deliberate deviation from the RFC: the
root document is addressed as `/` rather than the empty string, because `/` is
a friendlier value to log and display.

## Key design decisions

- **Static + stateless.** The differ holds no state between calls; options are
  an immutable `init`-only object passed per call, with a shared internal
  `DiffOptions.Default`. This makes the API thread-safe by construction and
  removes any need for DI or instance lifetime concerns.
- **Flat change list, not a patch document.** The output is
  `IReadOnlyList<JsonChange>` rather than an RFC 6902 patch. Trade-off: you
  cannot mechanically apply the result to transform left into right, but a flat
  list is what the intended uses (logging, test assertions, rendering) actually
  consume, and it avoids patch-op ordering semantics entirely.
- **`readonly record struct` for `JsonChange`.** Changes are small value
  bundles; a record struct gives value equality and `with` support without
  per-change heap allocation, and `ToString()` provides the compact
  `+/-/~ path: value` rendering.
- **Positional array comparison.** LCS/heuristic array matching (as in
  JsonPatch-style libraries) produces smaller diffs for insertions in the
  middle, but is O(n*m), needs an element-identity heuristic, and its output is
  harder to explain. Index comparison is O(n), predictable, and honest about
  the fact that JSON arrays are ordered. Consequence: inserting one element at
  the head of an n-element array reports n changes.
- **Numeric tolerance via `double`.** `1` vs `1.0` differing is almost never
  what a caller wants, so it defaults on. The cost is `double` precision: two
  distinct numbers beyond 2^53 or with >15-17 significant digits can round to
  the same `double` and be reported as equal. Callers who need exact numeric
  comparison set `NumericTolerance = false`, which falls back to raw-text
  comparison.

## Extension points

The library is deliberately closed (`JsonDiffer` is static, `DiffOptions` is
sealed); extension happens by adding options, not by subclassing:

- New comparison behaviours belong on `DiffOptions` as `init` properties,
  consumed inside `Walk`/`ScalarEquals`/`WalkObject` (see how
  `IgnorePropertyCase` selects the dictionary comparer).
- New output formats belong on `JsonChange` (alongside `ToString()`) or as
  external formatters over the returned list - the change list carries cloned
  `JsonElement`s, so formatters can re-render values freely.

## Known limitations

- Array reordering and mid-array insertion produce noisy positional diffs (see
  above); there is no move/rename detection.
- The change list cannot be applied as a patch (no RFC 6902 output).
- Duplicate property names within one JSON object (legal per RFC 8259, unusual
  in practice) collapse: the right side's dictionary keeps the last occurrence,
  and with `IgnorePropertyCase` two left-side properties differing only in case
  both compare against the same right value.
- `NumericTolerance` inherits `double` semantics for very large or very
  precise numbers (see design decisions).
- Recursion depth follows document depth; pathologically deep documents are
  bounded in practice by `JsonDocument.Parse`'s own `MaxDepth` (default 64)
  before the walker's stack matters.
