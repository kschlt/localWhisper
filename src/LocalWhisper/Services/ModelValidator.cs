using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LocalWhisper.Core;

namespace LocalWhisper.Services;

/// <summary>
/// Validates Whisper model files using SHA-1 hash verification.
/// </summary>
/// <remarks>
/// Implements US-041a: Model Verification (File Selection)
/// - Computes SHA-1 hash of model files
/// - Compares against known-good hashes from whisper.cpp
/// - Logs validation results
///
/// Note: Uses SHA-1 (not SHA-256) to match whisper.cpp standard.
///
/// See: docs/iterations/iteration-05a-wizard-core.md
/// See: docs/reference/whisper-models.md
/// </remarks>
public class ModelValidator
{
    /// <summary>
    /// Validate model file SHA-1 hash.
    /// </summary>
    /// <param name="filePath">Path to model file</param>
    /// <param name="expectedSHA1">Expected SHA-1 hash (lowercase hex)</param>
    /// <returns>True if hash matches, false otherwise</returns>
    public async Task<bool> ValidateAsync(string filePath, string expectedSHA1)
    {
        if (!File.Exists(filePath))
        {
            AppLogger.LogError("Model file not found", new { FilePath = filePath });
            return false;
        }

        try
        {
            var startTime = DateTime.Now;

            // Compute SHA-1 hash
            using var sha1 = SHA1.Create();
            using var stream = File.OpenRead(filePath);

            var hashBytes = await sha1.ComputeHashAsync(stream);
            var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            var elapsed = DateTime.Now - startTime;
            var matches = computedHash.Equals(expectedSHA1, StringComparison.OrdinalIgnoreCase);

            AppLogger.LogInformation("Model hash validation completed", new
            {
                FilePath = filePath,
                ComputedHash = computedHash,
                ExpectedHash = expectedSHA1,
                Matches = matches,
                Duration_Ms = elapsed.TotalMilliseconds
            });

            return matches;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to compute model hash", ex, new { FilePath = filePath });
            return false;
        }
    }

    /// <summary>
    /// Compute SHA-1 hash without validation (for display purposes).
    /// </summary>
    /// <param name="filePath">Path to model file</param>
    /// <returns>SHA-1 hash as lowercase hex string, or empty string on error</returns>
    public async Task<string> ComputeSHA1Async(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        try
        {
            using var sha1 = SHA1.Create();
            using var stream = File.OpenRead(filePath);

            var hashBytes = await sha1.ComputeHashAsync(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to compute hash", ex, new { FilePath = filePath });
            return string.Empty;
        }
    }
}
