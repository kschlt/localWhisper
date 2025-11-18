using System;

namespace LocalWhisper.Models;

/// <summary>
/// Base exception for Speech-to-Text errors.
/// </summary>
public class STTException : Exception
{
    public int? ExitCode { get; set; }
    public string? StandardError { get; set; }

    public STTException(string message) : base(message)
    {
    }

    public STTException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public STTException(string message, int exitCode, string? stderr = null) : base(message)
    {
        ExitCode = exitCode;
        StandardError = stderr;
    }
}

/// <summary>
/// Exception thrown when Whisper model file is not found.
/// </summary>
/// <remarks>
/// Maps to Whisper CLI exit code 2.
/// User action: Check model path in configuration.
/// </remarks>
public class ModelNotFoundException : STTException
{
    public string? ModelPath { get; set; }

    public ModelNotFoundException(string message) : base(message)
    {
    }

    public ModelNotFoundException(string message, string modelPath) : base(message)
    {
        ModelPath = modelPath;
    }

    public ModelNotFoundException(string message, int exitCode, string? stderr = null)
        : base(message, exitCode, stderr)
    {
    }
}

/// <summary>
/// Exception thrown when audio device is unavailable during STT.
/// </summary>
/// <remarks>
/// Maps to Whisper CLI exit code 3.
/// User action: Check audio device connections.
/// </remarks>
public class AudioDeviceException : STTException
{
    public AudioDeviceException(string message) : base(message)
    {
    }

    public AudioDeviceException(string message, int exitCode, string? stderr = null)
        : base(message, exitCode, stderr)
    {
    }
}

/// <summary>
/// Exception thrown when STT processing exceeds timeout.
/// </summary>
/// <remarks>
/// Maps to Whisper CLI exit code 4 or process timeout.
/// User action: Try shorter audio or check system resources.
/// </remarks>
public class STTTimeoutException : STTException
{
    public TimeSpan Timeout { get; set; }

    public STTTimeoutException(string message, TimeSpan timeout) : base(message)
    {
        Timeout = timeout;
    }

    public STTTimeoutException(string message, int exitCode, string? stderr = null)
        : base(message, exitCode, stderr)
    {
    }
}

/// <summary>
/// Exception thrown when audio file format is invalid for STT.
/// </summary>
/// <remarks>
/// Maps to Whisper CLI exit code 5.
/// User action: Check WAV file format (should be 16kHz, mono, 16-bit PCM).
/// </remarks>
public class InvalidAudioException : STTException
{
    public string? AudioFilePath { get; set; }

    public InvalidAudioException(string message) : base(message)
    {
    }

    public InvalidAudioException(string message, string audioFilePath) : base(message)
    {
        AudioFilePath = audioFilePath;
    }

    public InvalidAudioException(string message, int exitCode, string? stderr = null)
        : base(message, exitCode, stderr)
    {
    }
}
