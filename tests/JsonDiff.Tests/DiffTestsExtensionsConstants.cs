namespace JsonDiff.Tests
{
    /// <summary>
    /// Centralised constants used by <see cref="DiffTestsExtensions"/>.
    /// </summary>
    internal static class DiffTestsExtensionsConstants
    {
        // Exception messages
        public const string NoChangeFoundMessage = "No change found at path '{0}'";
        public const string ExpectedExactlyOneChangeNoneMessage = "Expected exactly one change but found none.";
        public const string ExpectedExactlyOneChangeCountMessage = "Expected exactly one change but found {0}.";
        public const string ExpectedChangesButFoundNoneMessage = "Expected changes but found none.";
        public const string ExpectedAllChangesKindMessage = "Expected all changes to have kind '{0}' but found mismatches.";
        public const string PropertyNotFoundMessage = "Property '{0}' not found at path '{1}'";
        public const string InvalidArrayIndexMessage = "Invalid array index '{0}' at path '{1}'";
        public const string ArrayIndexOutOfRangeMessage = "Array index {0} out of range at path '{1}'";
        public const string CannotNavigateMessage = "Cannot navigate into non-object/array value at path '{0}'";
    }
}
