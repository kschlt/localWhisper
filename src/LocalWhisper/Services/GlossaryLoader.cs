using System.IO;
using System.Text;

namespace LocalWhisper.Services;

/// <summary>
/// Loads and formats glossary files for LLM post-processing.
/// Glossary format: "abbreviation = expansion" (one per line).
/// </summary>
/// <remarks>
/// Iteration 7: US-063 (Glossary Support)
/// See: docs/iterations/iteration-07-post-processing-DECISIONS.md
/// </remarks>
public class GlossaryLoader
{
    private const int MaxGlossaryEntries = 500;

    /// <summary>
    /// Load glossary from file.
    /// </summary>
    /// <param name="glossaryPath">Path to glossary file</param>
    /// <returns>Dictionary of abbreviation -> expansion mappings (max 500 entries)</returns>
    /// <remarks>
    /// File format:
    /// - Lines starting with # are comments (skipped)
    /// - Valid entries: "asap = as soon as possible"
    /// - Invalid lines are skipped silently
    /// - Whitespace is trimmed
    /// - Max 500 entries (truncated if larger)
    /// </remarks>
    public Dictionary<string, string> LoadGlossary(string glossaryPath)
    {
        var entries = new Dictionary<string, string>();

        // Return empty dictionary if file doesn't exist
        if (!File.Exists(glossaryPath))
        {
            return entries;
        }

        try
        {
            var lines = File.ReadAllLines(glossaryPath);
            foreach (var line in lines)
            {
                // Stop at max entries
                if (entries.Count >= MaxGlossaryEntries)
                {
                    AppLogger.LogWarning("Glossary truncated at 500 entries", new { GlossaryPath = glossaryPath });
                    break;
                }

                // Skip empty lines and comments
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // Parse "key = value" format
                var parts = trimmedLine.Split('=', 2);
                if (parts.Length != 2)
                {
                    // Invalid line - skip silently
                    continue;
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    entries[key] = value;
                }
            }

            AppLogger.LogInformation("Glossary loaded", new { EntryCount = entries.Count, GlossaryPath = glossaryPath });
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to load glossary", ex, new { GlossaryPath = glossaryPath });
            return new Dictionary<string, string>(); // Return empty on error
        }

        return entries;
    }

    /// <summary>
    /// Format glossary entries for LLM prompt injection.
    /// </summary>
    /// <param name="entries">Glossary entries</param>
    /// <returns>Formatted string to append to system prompt (empty if no entries)</returns>
    public string FormatGlossaryForPrompt(Dictionary<string, string> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("APPLY THESE ABBREVIATIONS:");

        foreach (var (key, value) in entries)
        {
            sb.AppendLine($"{key} = {value}");
        }

        return sb.ToString();
    }
}
