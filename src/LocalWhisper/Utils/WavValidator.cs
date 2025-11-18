using System;
using System.IO;
using LocalWhisper.Core;

namespace LocalWhisper.Utils;

/// <summary>
/// Validates WAV file format for speech-to-text processing.
/// </summary>
/// <remarks>
/// Implements US-011: WAV File Validation
/// - Validates WAV header (RIFF format)
/// - Checks format specifications: 16kHz, mono, 16-bit PCM
/// - Moves invalid files to failed/ subdirectory
///
/// See: docs/iterations/iteration-02-audio-recording.md (US-011)
/// See: docs/specification/functional-requirements.md (FR-011)
/// </remarks>
public static class WavValidator
{
    // Expected WAV format for Whisper
    private const int ExpectedSampleRate = 16000;
    private const short ExpectedChannels = 1;
    private const short ExpectedBitsPerSample = 16;
    private const short ExpectedAudioFormat = 1; // PCM

    /// <summary>
    /// Validate a WAV file against Whisper requirements.
    /// </summary>
    /// <param name="filePath">Path to WAV file</param>
    /// <param name="errorMessage">Error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool ValidateWavFile(string filePath, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            if (!File.Exists(filePath))
            {
                errorMessage = $"File not found: {filePath}";
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < 44) // Minimum WAV file size (header only)
            {
                errorMessage = "Invalid WAV file: File too small";
                return false;
            }

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                // Read and validate RIFF header
                var riffHeader = new string(br.ReadChars(4));
                if (riffHeader != "RIFF")
                {
                    errorMessage = "Invalid WAV file header: Expected 'RIFF'";
                    return false;
                }

                var fileSize = br.ReadInt32(); // File size - 8
                var waveHeader = new string(br.ReadChars(4));
                if (waveHeader != "WAVE")
                {
                    errorMessage = "Invalid WAV file header: Expected 'WAVE' format";
                    return false;
                }

                // Read and validate fmt chunk
                var fmtHeader = new string(br.ReadChars(4));
                if (fmtHeader != "fmt ")
                {
                    errorMessage = "Invalid WAV file: Missing 'fmt ' chunk";
                    return false;
                }

                var fmtChunkSize = br.ReadInt32();
                if (fmtChunkSize < 16)
                {
                    errorMessage = "Invalid WAV file: fmt chunk too small";
                    return false;
                }

                // Validate audio format
                var audioFormat = br.ReadInt16();
                if (audioFormat != ExpectedAudioFormat)
                {
                    errorMessage = $"Invalid audio format: Expected PCM (1), got {audioFormat}";
                    return false;
                }

                // Validate channels
                var channels = br.ReadInt16();
                if (channels != ExpectedChannels)
                {
                    errorMessage = $"Invalid channel count: Expected {ExpectedChannels} (mono), got {channels}";
                    return false;
                }

                // Validate sample rate
                var sampleRate = br.ReadInt32();
                if (sampleRate != ExpectedSampleRate)
                {
                    errorMessage = $"Invalid sample rate: Expected {ExpectedSampleRate} Hz, got {sampleRate} Hz";
                    return false;
                }

                var byteRate = br.ReadInt32();
                var blockAlign = br.ReadInt16();

                // Validate bits per sample
                var bitsPerSample = br.ReadInt16();
                if (bitsPerSample != ExpectedBitsPerSample)
                {
                    errorMessage = $"Invalid bit depth: Expected {ExpectedBitsPerSample} bits, got {bitsPerSample} bits";
                    return false;
                }

                // All validations passed
                AppLogger.LogInformation("WAV file validated successfully", new
                {
                    FilePath = filePath,
                    SampleRate = sampleRate,
                    Channels = channels,
                    BitsPerSample = bitsPerSample,
                    FileSize = fileInfo.Length
                });

                return true;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error validating WAV file: {ex.Message}";
            AppLogger.LogError("WAV validation failed", ex, new { FilePath = filePath });
            return false;
        }
    }

    /// <summary>
    /// Move an invalid WAV file to the failed/ subdirectory.
    /// </summary>
    /// <param name="filePath">Path to invalid WAV file</param>
    public static void MoveToFailedDirectory(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                AppLogger.LogWarning("Cannot move file to failed directory: File not found", new { FilePath = filePath });
                return;
            }

            // Get parent directory and create failed/ subdirectory
            var directory = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("Cannot determine file directory");
            var failedDirectory = Path.Combine(directory, "failed");
            Directory.CreateDirectory(failedDirectory);

            // Move file preserving filename
            var filename = Path.GetFileName(filePath);
            var targetPath = Path.Combine(failedDirectory, filename);

            // If target already exists, append timestamp
            if (File.Exists(targetPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
                var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
                var ext = Path.GetExtension(filename);
                filename = $"{nameWithoutExt}_{timestamp}{ext}";
                targetPath = Path.Combine(failedDirectory, filename);
            }

            File.Move(filePath, targetPath);

            AppLogger.LogInformation("Moved invalid WAV file to failed directory", new
            {
                OriginalPath = filePath,
                NewPath = targetPath
            });
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to move invalid WAV file", ex, new { FilePath = filePath });
            throw;
        }
    }
}
