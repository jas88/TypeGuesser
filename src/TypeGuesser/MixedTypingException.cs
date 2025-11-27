using System;
using System.Text.Json.Serialization;

namespace TypeGuesser;

/// <summary>
/// Thrown when passing both strongly typed objects (e.g. 12.0f) and untyped strings (e.g. "12.0") to a single <see cref="Guesser"/>.  Input to a
/// guesser must be of a consistent format (either all typed or all untyped).
/// </summary>
[Serializable]
public class MixedTypingException:Exception
{
    private readonly string? _message;

    /// <summary>
    /// Gets or initializes the exception message for JSON serialization.
    /// </summary>
    [JsonInclude]
    public string? ExceptionMessage
    {
        get => Message;
        init => _message = value;
    }

    /// <summary>
    /// Gets the exception message.
    /// </summary>
    public override string Message => _message ?? base.Message;

    /// <summary>
    /// Creates a new instance with default message (for serialization)
    /// </summary>
    public MixedTypingException()
    {
    }

    /// <summary>
    /// Creates a new instance with the given message and inner Exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="ex"></param>
    public MixedTypingException(string message, Exception? ex):base(message,ex)
    {
        _message = message;
    }

    /// <summary>
    /// Creates a new instance with the given message
    /// </summary>
    /// <param name="message"></param>
    public MixedTypingException(string message) : base(message)
    {
        _message = message;
    }

}