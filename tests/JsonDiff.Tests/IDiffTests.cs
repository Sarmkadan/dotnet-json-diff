namespace JsonDiff.Tests;

public interface IDiffTests
{
    void IdenticalDocuments_ProduceNoChanges();
    void KeyOrder_IsIgnored();
    void AddedProperty_IsReported();
    void RemovedProperty_IsReported();
    void ChangedValue_IsReported();
    void NestedObjectDifference_IsReported();
    void ArrayElementAddition_IsReported();
    void ArrayElementRemoval_IsReported();
    void ArrayElementChange_IsReported();
    void MaxDepth_WithDepthLimit_ReportsSubtreeAsSingleChange();
    void MaxDepth_WithLimit1_NestedObjectDifference();
    void MaxDepth_WithLimit2_AllowsDeeperTraversal();
    void MaxDepth_Null_AllowsUnlimitedTraversal();
    void MaxDepth_WithArrays_StopsAtDepthLimit();
    void DetectArrayShifts_RemovedFirstElement_ReportsSingleRemovedChange();
    void DetectArrayShifts_AddedFirstElement_ReportsSingleAddedChange();
    void DetectArrayShifts_ChangedMiddleElement_ReportsSingleChangedChange();
    void MaxNodeCount_WithLimit_ThrowsWhenExceeded();
    void MaxNodeCount_UnderLimit_ProcessesSuccessfully();
    void MaxNodeCount_CustomLimit_AllowsLargerDocuments();
    void MaxNodeCount_Zero_ThrowsArgumentOutOfRangeException();
    void MaxNodeCount_Null_AllowsUnlimitedTraversal();
}
