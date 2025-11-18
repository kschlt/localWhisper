using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Utils;

namespace LocalWhisper.Services;

/// <summary>
/// Writes dictation history files to disk.
/// </summary>
/// <remarks>
/// Implements US-031: History File Creation
/// - Creates markdown files with YAML front-matter
/// - Organizes files in date-based directory structure: history/YYYY/YYYY-MM/YYYY-MM-DD/
/// - Generates filename with timestamp and slug: YYYYMMDD_HHmmssfff_{slug}.md
/// - Handles duplicate filenames by appending counter (_2, _3, etc.)
/// - Graceful error handling (logs but doesn't throw)
///
/// See: docs/iterations/iteration-04-clipboard-history-flyout.md (US-031)
/// See: docs/specification/functional-requirements.md (FR-014)
/// </remarks>
public class HistoryWriter
{
    /// <summary>
    /// Write history entry to disk.
    /// </summary>
    /// <param name="entry">History entry with transcript and metadata</param>
    /// <param name="dataRoot">Application data root directory</param>
    /// <returns>Absolute path to created file</returns>
    /// <exception cref="IOException">If file write fails (logged and re-thrown)</exception>
    public async Task<string> WriteAsync(HistoryEntry entry, string dataRoot)
    {
        try
        {
            // 1. Generate slug from transcript text
            var slug = SlugGenerator.Generate(entry.Text);

            // 2. Build directory path: {dataRoot}/history/YYYY/YYYY-MM/YYYY-MM-DD/
            var directoryPath = entry.GetAbsoluteDirectory(dataRoot);

            // 3. Ensure directory exists
            Directory.CreateDirectory(directoryPath);

            // 4. Build filename with timestamp and slug
            var baseFileName = entry.GetFileName(slug);
            var filePath = Path.Combine(directoryPath, baseFileName);

            // 5. Handle duplicate filenames
            filePath = GetUniqueFilePath(filePath);

            // 6. Generate markdown content
            var markdown = entry.ToMarkdown();

            // 7. Write file (UTF-8 encoding)
            await File.WriteAllTextAsync(filePath, markdown, Encoding.UTF8);

            AppLogger.LogInformation("History file created", new
            {
                Path = filePath,
                Slug = slug,
                TextLength = entry.Text.Length,
                FileSize = new FileInfo(filePath).Length
            });

            return filePath;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to write history file", ex, new
            {
                DataRoot = dataRoot,
                TextPreview = entry.Text.Length > 50 ? entry.Text.Substring(0, 50) + "..." : entry.Text
            });
            throw;
        }
    }

    /// <summary>
    /// Get unique file path by appending counter if file already exists.
    /// </summary>
    /// <param name="originalPath">Original file path</param>
    /// <returns>Unique file path (original or with _2, _3, etc.)</returns>
    private static string GetUniqueFilePath(string originalPath)
    {
        if (!File.Exists(originalPath))
        {
            return originalPath;
        }

        var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        var counter = 2;
        string uniquePath;

        do
        {
            var newFileName = $"{fileNameWithoutExt}_{counter}{extension}";
            uniquePath = Path.Combine(directory, newFileName);
            counter++;
        }
        while (File.Exists(uniquePath) && counter < 1000); // Safety limit

        if (counter >= 1000)
        {
            // Fallback: append GUID if too many duplicates
            var guidFileName = $"{fileNameWithoutExt}_{Guid.NewGuid():N}{extension}";
            uniquePath = Path.Combine(directory, guidFileName);
        }

        return uniquePath;
    }
}
