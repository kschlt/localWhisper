using System;

namespace LocalWhisper.Services;

/// <summary>
/// Exception thrown when model download fails after all retries.
/// </summary>
public class ModelDownloadException : Exception
{
    public ModelDownloadException(string message) : base(message)
    {
    }

    public ModelDownloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
