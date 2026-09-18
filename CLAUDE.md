# CLAUDE.md

## Overview
JsonDiff - tiny semantic JSON diff library for .NET (net10.0) built on `System.Text.Json`; ignores key order, reports Added/Removed/Changed paths, formats to JSON Patch / JSON Merge Patch. No third-party runtime dependencies.

## Build
```bash
dotnet build JsonDiff.slnx            # library + tests
dotnet build -c Release               # release
dotnet pack src/JsonDiff -c Release   # NuGet package (PackageId JsonDiff)
dotnet run -c Release --project benchmarks/JsonDiff.Benchmarks   # BenchmarkDotNet (not in .slnx)
```

## Test
```bash
dotnet test JsonDiff.slnx
dotnet test tests/JsonDiff.Tests --filter "FullyQualifiedName~DeepEqualsTests"
dotnet test --collect:"XPlat Code Coverage"     # coverlet
```
Framework: xUnit 2.9 (`<Using Include="Xunit" />` global using in test project).

## Lint / format
No analyzers or .editorconfig configured. `Nullable` and `ImplicitUsings` enabled, `LangVersion latest`, `GenerateDocumentationFile` on for the library - all public members need XML doc comments (warnings otherwise). Use `dotnet format` before committing.

## Key directories and entry points
- `src/JsonDiff/` - the library (single project, single namespace `JsonDiff`)
  - `JsonDiffer.cs` - public static entry points: `Diff(string|JsonElement, ..., DiffOptions?)`, `DeepEquals(...)`; recursive `Walk`/`WalkObject`/`WalkArray`
  - `JsonChange.cs`, `ChangeKind.cs` - result record (`readonly record struct`) and enum Added/Removed/Changed
  - `DiffOptions.cs`, `IDiffOptions.cs` - init-only tunables (NumericTolerance, IgnorePropertyCase, MaxDepth, DetectArrayShifts, MaxArrayShiftDetectionSize, ...)
  - `JsonPatchFormatter.cs`, `JsonMergePatchFormatter.cs` - RFC 6902 / RFC 7386 output
  - `JsonPointer.cs` - RFC 6901 path escaping
  - `JsonDiffException.cs`, `JsonDiffLimitExceededException.cs` - error types
  - `*Extensions.cs` - extension helpers for changes/options/differ
- `tests/JsonDiff.Tests/` - xUnit tests, one class per feature area (`DiffTests`, `DeepEqualsTests`, `PointerEscapingTests`, `ScalarEdgeCaseTests`, `JsonPatchFormatterTests`, ...)
- `benchmarks/JsonDiff.Benchmarks/` - BenchmarkDotNet project
- `docs/ARCHITECTURE.md` - data flow and algorithm description; `IMPLEMENTATION_SUMMARY.md` - array shift detection notes
- `JsonDiff.slnx` - solution (XML slnx format; includes src and tests only)

## Conventions
- Static, stateless API; no DI, no I/O. Results are `IReadOnlyList<JsonChange>`; every `JsonElement` stored in a change is `.Clone()`d so it outlives the parse.
- Paths are JSON Pointers (`/a/b/0`); object property order is irrelevant by construction.
- Options are immutable `init` properties with defaults on `DiffOptions`; add new knobs there and document them with `<summary>`/`<remarks>`.
- Test naming: `Scenario_ExpectedOutcome` (e.g. `KeyOrder_IsIgnored`, `NumericTolerance_False_1_vs_1dot0_Changed`); Arrange/Act/Assert layout; shared helpers in `*TestsExtensions.cs` / `*Constants.cs`.
- File-scoped namespaces, nullable-clean code, XML docs on all public members.
- Commit messages: conventional prefixes (`docs:`, `feat:`, `fix:`, `perf:`).
