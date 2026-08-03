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
