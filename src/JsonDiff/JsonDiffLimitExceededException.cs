using System;

namespace JsonDiff;

/// <summary>
/// The exception that is thrown when a JSON diff operation exceeds configured limits
/// (e.g., maximum depth or maximum number of changes).
/// </summary>
public sealed class JsonDiffLimitExceededException : Exception
{
    /// <summary>
    /// Gets the path in the JSON document where the limit was exceeded.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDiffLimitExceededException"/> class
    /// with a specified error message and the path where the limit was exceeded.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="path">The JSON path where the limit was exceeded.</param>
    public JsonDiffLimitExceededException(string message, string path)
        : base(message)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDiffLimitExceededException"/> class
    /// with a specified error message, the path where the limit was exceeded, and a reference
    /// to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="path">The JSON path where the limit was exceeded.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonDiffLimitExceededException(string message, string path, Exception? innerException)
        : base(message, innerException)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }
}