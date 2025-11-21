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
/// Used in Settings window (US-053) and First-run wizard (US-040).
/// Note: Uses SHA-1 (not SHA-256) to match whisper.cpp standard.
///
/// See: docs/iterations/iteration-05a-wizard-core.md
/// See: docs/iterations/iteration-06-settings.md (ModelVerificationTests section)
/// See: docs/reference/whisper-models.md
/// </remarks>
public class ModelValidator
{
    /// <summary>
    /// Validate model file by computing SHA-1 hash.
    /// </summary>
    /// <param name="modelPath">Path to model file</param>
    /// <param name="expectedHash">Expected SHA-1 hash (hexadecimal string)</param>
    /// <param name="progress">Optional progress callback (reports 0.0 to 1.0)</param>
    /// <returns>Tuple (isValid, message)</returns>
    public virtual (bool IsValid, string Message) ValidateModel(string modelPath, string expectedHash, IProgress<double>? progress = null)
    {
        try
        {
            // Check file exists
            if (!File.Exists(modelPath))
            {
                return (false, $"Model file not found: {modelPath}");
            }

            // Compute SHA-1 hash
            var computedHash = ComputeSha1(modelPath, progress);

            // Compare hashes (case-insensitive)
            if (string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.LogInformation("Model validation successful", new { ModelPath = modelPath, Hash = computedHash });
                return (true, "Model hash matches expected value");
            }
            else
            {
                AppLogger.LogWarning("Model hash mismatch", new
                {
                    ModelPath = modelPath,
                    Expected = expectedHash,
                    Computed = computedHash
                });
                return (false, $"Model hash mismatch. Expected: {expectedHash}, Got: {computedHash}");
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Model validation failed", ex, new { ModelPath = modelPath });
            return (false, $"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate model file asynchronously by computing SHA-1 hash.
    /// </summary>
    /// <param name="modelPath">Path to model file</param>
    /// <param name="expectedHash">Expected SHA-1 hash (hexadecimal string)</param>
    /// <param name="progress">Optional progress callback (reports 0.0 to 1.0)</param>
    /// <returns>True if validation successful, false otherwise</returns>
    /// <exception cref="FileNotFoundException">If model file does not exist</exception>
    public virtual async Task<bool> ValidateAsync(string modelPath, string expectedHash, IProgress<double>? progress = null)
    {
        return await Task.Run(() =>
        {
            // Check file exists first (throw if not - more appropriate than returning false)
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"Model file not found: {modelPath}", modelPath);
            }

            var (isValid, _) = ValidateModel(modelPath, expectedHash, progress);
            return isValid;
        });
    }

    /// <summary>
    /// Compute SHA-1 hash of a file.
    /// </summary>
    /// <param name="filePath">Path to file</param>
    /// <param name="progress">Optional progress callback (reports 0.0 to 1.0)</param>
    /// <returns>SHA-1 hash as hexadecimal string (lowercase)</returns>
    private string ComputeSha1(string filePath, IProgress<double>? progress = null)
    {
        using var sha1 = SHA1.Create();
        using var stream = File.OpenRead(filePath);

        var fileSize = stream.Length;
        var buffer = new byte[81920]; // 80 KB buffer
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalRead += bytesRead;

            // Check if we've reached end of file (Position == Length after Read)
            if (stream.Position < fileSize)
            {
                // More data to come - use TransformBlock
                sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
            else
            {
                // Final block - use TransformFinalBlock
                sha1.TransformFinalBlock(buffer, 0, bytesRead);
            }

            // Report progress
            if (progress != null && fileSize > 0)
            {
                var progressPercent = (double)totalRead / fileSize;
                progress.Report(progressPercent);
            }
        }

        // Handle empty files (totalRead == 0)
        if (totalRead == 0)
        {
            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        }

        var hash = sha1.Hash ?? Array.Empty<byte>();
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Validate model file existence only (no hash check).
    /// </summary>
    /// <param name="modelPath">Path to model file</param>
    /// <returns>True if file exists and has reasonable size</returns>
    public virtual bool QuickValidate(string modelPath)
    {
        if (!File.Exists(modelPath))
            return false;

        // Model files should be at least 10 MB (very small models)
        var fileInfo = new FileInfo(modelPath);
        return fileInfo.Length > 10 * 1024 * 1024;
    }
}
