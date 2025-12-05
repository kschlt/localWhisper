using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LocalWhisper.Models;

/// <summary>
/// Represents a speech-to-text transcription result from Whisper CLI.
/// </summary>
/// <remarks>
/// Parsed from JSON output with structure:
/// {
///   "text": "Full transcript",
///   "language": "de",
///   "duration_sec": 5.2,
///   "segments": [...],
///   "meta": {...}
/// }
///
/// See: docs/iterations/iteration-03-stt-whisper.md (JSON Output Format)
/// </remarks>
public class STTResult
{
    /// <summary>
    /// Full transcription text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Detected or specified language code (e.g., "de", "en").
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Audio duration in seconds.
    /// </summary>
    [JsonPropertyName("duration_sec")]
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Optional array of timestamped segments.
    /// </summary>
    [JsonPropertyName("segments")]
    public List<STTSegment>? Segments { get; set; }

    /// <summary>
    /// Optional metadata about processing.
    /// </summary>
    [JsonPropertyName("meta")]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Check if the transcript is empty (no speech detected).
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
}

/// <summary>
/// Represents a timestamped segment within a transcription.
/// </summary>
public class STTSegment
{
    /// <summary>
    /// Start time in seconds.
    /// </summary>
    [JsonPropertyName("start")]
    public double Start { get; set; }

    /// <summary>
    /// End time in seconds.
    /// </summary>
    [JsonPropertyName("end")]
    public double End { get; set; }

    /// <summary>
    /// Segment text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Wrapper for actual whisper-cli JSON output format with -oj flag.
/// </summary>
/// <remarks>
/// Actual whisper-cli with -oj produces this structure:
/// {
///   "result": { "language": "de" },
///   "transcription": [ { "text": "...", "timestamps": {...}, "offsets": {...} } ]
/// }
/// </remarks>
internal class WhisperCliJsonOutput
{
    [JsonPropertyName("result")]
    public WhisperResult? Result { get; set; }

    [JsonPropertyName("transcription")]
    public List<WhisperTranscriptionSegment>? Transcription { get; set; }

    /// <summary>
    /// Convert to STTResult by concatenating all transcription segments.
    /// </summary>
    public STTResult ToSTTResult()
    {
        var text = string.Empty;
        var language = Result?.Language ?? string.Empty;

        if (Transcription != null && Transcription.Count > 0)
        {
            // Concatenate all segment texts
            text = string.Join("", Transcription.Select(t => t.Text ?? string.Empty));
        }

        return new STTResult
        {
            Text = text.Trim(),
            Language = language,
            DurationSeconds = 0 // Not provided in this format
        };
    }
}

internal class WhisperResult
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }
}

internal class WhisperTranscriptionSegment
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("timestamps")]
    public Dictionary<string, string>? Timestamps { get; set; }

    [JsonPropertyName("offsets")]
    public Dictionary<string, int>? Offsets { get; set; }
}
