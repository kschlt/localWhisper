using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalWhisper.Utils;

/// <summary>
/// Generates URL-safe slugs from transcript text for history filenames.
/// </summary>
/// <remarks>
/// Implements US-036: Slug Generation for History Filenames
/// - Converts to kebab-case (lowercase with hyphens)
/// - Normalizes German umlauts (ä→a, ö→o, ü→u, ß→ss)
/// - Normalizes other accented characters (é→e, ñ→n, etc.)
/// - Removes special characters
/// - Truncates to maximum length (default 50)
/// - Compresses multiple hyphens to single hyphen
/// - Returns "transcript" fallback for empty input
///
/// See: docs/iterations/iteration-04-clipboard-history-flyout.md (US-036)
/// See: docs/specification/functional-requirements.md (FR-024)
/// </remarks>
public static class SlugGenerator
{
    private const string DefaultSlug = "transcript";

    /// <summary>
    /// Generate a URL-safe slug from text.
    /// </summary>
    /// <param name="text">Input text (typically transcript)</param>
    /// <param name="maxLength">Maximum slug length (default 50)</param>
    /// <returns>Kebab-case slug, or "transcript" if input is empty</returns>
    public static string Generate(string text, int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return DefaultSlug;
        }

        // 1. Normalize German umlauts explicitly (before general normalization)
        var normalized = NormalizeGermanUmlauts(text);

        // 2. Normalize accented characters (é→e, ñ→n, etc.) using Unicode normalization
        normalized = RemoveDiacritics(normalized);

        // 3. Convert to lowercase
        normalized = normalized.ToLowerInvariant();

        // 4. Replace spaces and underscores with hyphens
        normalized = Regex.Replace(normalized, @"[\s_]+", "-");

        // 5. Remove all characters except alphanumeric and hyphens
        normalized = Regex.Replace(normalized, @"[^a-z0-9\-]", "");

        // 6. Compress multiple hyphens to single hyphen
        normalized = Regex.Replace(normalized, @"-+", "-");

        // 7. Trim leading and trailing hyphens
        normalized = normalized.Trim('-');

        // 8. If empty after normalization, return default
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultSlug;
        }

        // 9. Truncate to max length (break at word boundary if possible)
        if (normalized.Length > maxLength)
        {
            normalized = TruncateAtWordBoundary(normalized, maxLength);
        }

        // 10. Ensure no trailing hyphen after truncation
        normalized = normalized.TrimEnd('-');

        // 11. Final safety check
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultSlug;
        }

        return normalized;
    }

    /// <summary>
    /// Normalize German umlauts to ASCII equivalents.
    /// </summary>
    private static string NormalizeGermanUmlauts(string text)
    {
        var result = new StringBuilder(text);
        result.Replace("ä", "a");
        result.Replace("ö", "o");
        result.Replace("ü", "u");
        result.Replace("Ä", "A");
        result.Replace("Ö", "O");
        result.Replace("Ü", "U");
        result.Replace("ß", "ss");
        return result.ToString();
    }

    /// <summary>
    /// Remove diacritics (accents) from characters using Unicode normalization.
    /// </summary>
    /// <remarks>
    /// Uses FormD (canonical decomposition) to separate base characters from combining diacritics,
    /// then filters out the diacritics.
    /// Example: é (U+00E9) → e (U+0065) + combining acute (U+0301) → e (U+0065)
    /// </remarks>
    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Truncate text at word boundary (hyphen) if possible, otherwise hard truncate.
    /// </summary>
    private static string TruncateAtWordBoundary(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        // Try to find last hyphen before maxLength
        var truncated = text.Substring(0, maxLength);
        var lastHyphen = truncated.LastIndexOf('-');

        if (lastHyphen > 0 && lastHyphen > maxLength / 2) // Only break at hyphen if it's in the latter half
        {
            return truncated.Substring(0, lastHyphen);
        }

        // Hard truncate if no good hyphen position found
        return truncated;
    }
}
