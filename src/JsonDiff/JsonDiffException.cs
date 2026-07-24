using System;

namespace JsonDiff;

/// <summary>
/// The exception that is thrown when a JSON diff operation encounters a library-level error.
/// This includes malformed input, incompatible input types, and other invalid operations
/// that prevent the diff or patch formatting from completing successfully.
/// </summary>
/// <remarks>
/// This exception serves as the common exception type for both <see cref="JsonDiffer"/>
/// and <see cref="JsonPatchFormatter"/> to ensure consistent error handling across the library.
/// </remarks>
public sealed class JsonDiffException : Exception
{
    /// <summary>
    /// Gets the path in the JSON document where the error occurred, if applicable.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDiffException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonDiffException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDiffException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonDiffException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDiffException"/> class with a specified error message
    /// and the path where the error occurred.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="path">The JSON path where the error occurred, if applicable.</param>
    public JsonDiffException(string message, string? path)
        : base(message)
    {
        Path = path;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDiffException"/> class with a specified error message,
    /// the path where the error occurred, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="path">The JSON path where the error occurred, if applicable.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonDiffException(string message, string? path, Exception? innerException)
        : base(message, innerException)
    {
        Path = path;
    }
}