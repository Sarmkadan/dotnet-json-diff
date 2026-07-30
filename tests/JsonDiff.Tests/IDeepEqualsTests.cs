namespace JsonDiff.Tests;

public interface IDeepEqualsTests
{
    void IdenticalDocuments_AreEqual();
    void KeyOrder_IsIgnored();
    void AddedProperty_MakesNotEqual();
    void RemovedProperty_MakesNotEqual();
    void ChangedScalar_MakesNotEqual();
    void KindChange_MakesNotEqual();
    void NestedPropertyDifference_IsDetected();
    void ArrayElementDifference_IsDetected();
    void ArrayLengthDifference_IsDetected();
    void NumericTolerance_TreatsEquivalentNumbersAsEqual();
    void NumericTolerance_Off_ReportsEquivalentNumbersAsDifferent();
    void IgnorePropertyCase_MatchesRegardlessOfCase();
    void PropertyCaseDifference_IsDetected_WhenNotConfigured();
    void NullValues_AreEqual();
    void BooleanValues_AreEqual();
    void StringValues_AreEqual();
    void DeepEquals_JsonElementOverload_WorksCorrectly();
    void DeepEquals_JsonElementOverload_DetectsDifferences();
    void MaxDepth_Limited_AllowsTraversalToDepth2();
    void MaxDepth_WithLimit1_ComparesAtRootLevel();
}
