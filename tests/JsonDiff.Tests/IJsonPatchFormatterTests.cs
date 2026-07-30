namespace JsonDiff.Tests
{
    public interface IJsonPatchFormatterTests
    {
        void AddOperation_RendersCorrectly();
        void RemoveOperation_RendersCorrectly();
        void ReplaceOperation_RendersCorrectly();
        void MultipleChanges_RendersArray();
    }
}