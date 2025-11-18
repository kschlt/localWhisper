using System;

namespace LocalWhisper.Models;

/// <summary>
/// Exception thrown when clipboard is locked by another application.
/// </summary>
public class ClipboardLockedException : Exception
{
    public int RetryCount { get; }

    public ClipboardLockedException(string message, int retryCount) : base(message)
    {
        RetryCount = retryCount;
    }

    public ClipboardLockedException(string message, int retryCount, Exception innerException)
        : base(message, innerException)
    {
        RetryCount = retryCount;
    }
}
