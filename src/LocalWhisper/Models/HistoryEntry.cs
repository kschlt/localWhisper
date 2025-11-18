using System;
using System.Globalization;
using System.Text;
using LocalWhisper.Utils;

namespace LocalWhisper.Models;

/// <summary>
/// Represents a dictation history entry with metadata.
/// </summary>
/// <remarks>
/// Implements US-031: History File Creation
/// - Stores transcript text and metadata (timestamp, language, model, duration)
/// - Generates markdown files with YAML front-matter
/// - Provides filename and directory path generation
///
/// See: docs/iterations/iteration-04-clipboard-history-flyout.md (US-031)
/// See: docs/specification/functional-requirements.md (FR-014)
/// See: docs/specification/data-structures.md (History file format)
/// </remarks>
public class HistoryEntry
{
    /// <summary>
    /// Timestamp when dictation was created (UTC).
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Transcript text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Language code (e.g., "de", "en").
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// STT model used (e.g., "whisper-small").
    /// </summary>
    public string SttModel { get; set; } = string.Empty;

    /// <summary>
    /// Audio duration in seconds.
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Whether post-processing (LLM) was applied.
    /// </summary>
    public bool PostProcessed { get; set; }

    /// <summary>
    /// Generate markdown content with YAML front-matter.
    /// </summary>
    /// <returns>Complete markdown file content</returns>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();

        // YAML front-matter
        sb.AppendLine("---");
        sb.AppendLine($"created: {Created.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"lang: {Language}");
        sb.AppendLine($"stt_model: {SttModel}");
        sb.AppendLine($"duration_sec: {DurationSeconds.ToString("F1", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"post_processed: {PostProcessed.ToString().ToLowerInvariant()}");
        sb.AppendLine("---");
        sb.AppendLine();

        // Heading (German date format: DD.MM.YYYY HH:mm)
        var localTime = Created.ToLocalTime();
        var dateString = localTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("de-DE"));
        sb.AppendLine($"# Diktat – {dateString}");
        sb.AppendLine();

        // Transcript body
        sb.AppendLine(Text);

        return sb.ToString();
    }

    /// <summary>
    /// Get filename for history file.
    /// </summary>
    /// <param name="slug">Slug generated from transcript text</param>
    /// <returns>Filename in format: YYYYMMDD_HHmmssfff_{slug}.md</returns>
    public string GetFileName(string slug)
    {
        var localTime = Created.ToLocalTime();
        var timestamp = localTime.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
        return $"{timestamp}_{slug}.md";
    }

    /// <summary>
    /// Get relative directory path for history file.
    /// </summary>
    /// <returns>Relative path: history/YYYY/YYYY-MM/YYYY-MM-DD/</returns>
    public string GetRelativeDirectory()
    {
        var localTime = Created.ToLocalTime();
        var year = localTime.ToString("yyyy", CultureInfo.InvariantCulture);
        var yearMonth = localTime.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var yearMonthDay = localTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return $"history/{year}/{yearMonth}/{yearMonthDay}/";
    }

    /// <summary>
    /// Get absolute directory path for history file.
    /// </summary>
    /// <param name="dataRoot">Application data root directory</param>
    /// <returns>Absolute directory path</returns>
    public string GetAbsoluteDirectory(string dataRoot)
    {
        return System.IO.Path.Combine(dataRoot, GetRelativeDirectory());
    }
}
