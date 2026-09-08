using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LocalWhisper.Models;

/// <summary>
/// Represents a speech-to-text transcription result from Whisper CLI.
/// </summary>
/// <remarks>
/// Normalized representation. The raw whisper-cli JSON (see
/// <see cref="WhisperCliJsonOutput"/>) is converted into this shape by
/// <see cref="WhisperCliJsonOutput.ToSTTResult"/>.
///
/// See: docs/architecture/interface-contracts.md (Whisper CLI JSON Output Format)
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
/// Raw whisper-cli JSON output format (produced with the -oj flag).
/// </summary>
/// <remarks>
/// whisper-cli (whisper.cpp) with -oj writes this structure:
/// {
///   "systeminfo": "...", "model": {...}, "params": {...},
///   "result": { "language": "de" },
///   "transcription": [
///     {
///       "timestamps": { "from": "00:00:00,000", "to": "00:00:02,340" },
///       "offsets": { "from": 0, "to": 2340 },
///       "text": " Dies ist ein Test."
///     }
///   ]
/// }
/// Unknown fields are ignored. Offsets are milliseconds.
/// </remarks>
internal class WhisperCliJsonOutput
{
    [JsonPropertyName("result")]
    public WhisperResult? Result { get; set; }

    [JsonPropertyName("transcription")]
    public List<WhisperTranscriptionSegment>? Transcription { get; set; }

    /// <summary>
    /// Convert to STTResult: concatenates all segment texts, maps offsets to
    /// seconds and derives the duration from the last segment end.
    /// </summary>
    public STTResult ToSTTResult()
    {
        var segments = new List<STTSegment>();
        var text = string.Empty;

        if (Transcription != null && Transcription.Count > 0)
        {
            text = string.Concat(Transcription.Select(t => t.Text ?? string.Empty));

            foreach (var t in Transcription)
            {
                segments.Add(new STTSegment
                {
                    Start = MsToSeconds(t.Offsets, "from"),
                    End = MsToSeconds(t.Offsets, "to"),
                    Text = (t.Text ?? string.Empty).Trim()
                });
            }
        }

        return new STTResult
        {
            Text = text.Trim(),
            Language = Result?.Language ?? string.Empty,
            DurationSeconds = segments.Count > 0 ? segments[^1].End : 0,
            Segments = segments.Count > 0 ? segments : null
        };
    }

    private static double MsToSeconds(Dictionary<string, long>? offsets, string key)
    {
        return offsets != null && offsets.TryGetValue(key, out var ms) ? ms / 1000.0 : 0;
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
    public Dictionary<string, long>? Offsets { get; set; }
}
