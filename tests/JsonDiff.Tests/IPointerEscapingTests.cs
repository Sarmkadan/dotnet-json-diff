namespace JsonDiff.Tests
{
    public interface IPointerEscapingTests
    {
        void SlashInPropertyName_IsEscaped();
        void TildeInPropertyName_IsEscaped();
        void NestedEscaping_CombinesEscapesCorrectly();
    }
}
