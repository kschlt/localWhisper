using System;

namespace LocalWhisper.Models;

/// <summary>
/// Represents a Whisper model definition with metadata.
/// </summary>
/// <remarks>
/// Implements US-041a: Model Verification (File Selection)
/// - Stores model metadata (name, size, hash, speed)
/// - Provides download URL for Iteration 5b
/// - Used for wizard model selection and validation
///
/// See: docs/iterations/iteration-05a-wizard-core.md
/// See: docs/reference/whisper-models.md
/// </remarks>
public class ModelDefinition
{
    /// <summary>
    /// Model name (e.g., "small", "base.en").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File name (e.g., "ggml-small.bin").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Model size in megabytes.
    /// </summary>
    public int SizeMB { get; set; }

    /// <summary>
    /// SHA-1 hash for verification (lowercase hex).
    /// </summary>
    public string SHA1 { get; set; } = string.Empty;

    /// <summary>
    /// Download URL (HuggingFace).
    /// </summary>
    public string DownloadURL { get; set; } = string.Empty;

    /// <summary>
    /// Speed factor relative to large model (e.g., 4.0 = 4x faster).
    /// </summary>
    public double SpeedFactor { get; set; }

    /// <summary>
    /// VRAM requirement in GB.
    /// </summary>
    public int VramGB { get; set; }

    /// <summary>
    /// Description for UI display (localized).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is an English-only model.
    /// </summary>
    public bool IsEnglishOnly => Name.EndsWith(".en");

    /// <summary>
    /// Get all available models (hardcoded for Iteration 5a).
    /// </summary>
    /// <remarks>
    /// TODO(Iter-6): Load from configuration file instead of hardcoding.
    /// For now, hardcoded to avoid chicken-egg problem (no config exists during first-run wizard).
    /// </remarks>
    public static ModelDefinition[] GetAvailableModels()
    {
        return new[]
        {
            // German/Multilingual models
            new ModelDefinition
            {
                Name = "base",
                FileName = "ggml-base.bin",
                SizeMB = 142,
                SHA1 = "465707469ff3a37a2b9b8d8f89f2f99de7299dac",
                DownloadURL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin",
                SpeedFactor = 7.0,
                VramGB = 1,
                Description = "Schnell (142 MB) - Gut für Echtzeit"
            },
            new ModelDefinition
            {
                Name = "small",
                FileName = "ggml-small.bin",
                SizeMB = 466,
                SHA1 = "55356645c2b361a969dfd0ef2c5a50d530afd8d5",
                DownloadURL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin",
                SpeedFactor = 4.0,
                VramGB = 2,
                Description = "Empfohlen (466 MB) - Beste Balance ⭐"
            },
            new ModelDefinition
            {
                Name = "medium",
                FileName = "ggml-medium.bin",
                SizeMB = 1536,
                SHA1 = "fd9727b6e1217c2f614f9b698455c4ffd82463b4",
                DownloadURL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin",
                SpeedFactor = 2.0,
                VramGB = 5,
                Description = "Hohe Qualität (1.5 GB) - Langsamer"
            },
            new ModelDefinition
            {
                Name = "large-v3",
                FileName = "ggml-large-v3.bin",
                SizeMB = 2960,
                SHA1 = "ad82bf6a9043ceed055076d0fd39f5f186ff8062",
                DownloadURL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin",
                SpeedFactor = 1.0,
                VramGB = 10,
                Description = "Höchste Qualität (2.9 GB) - Am langsamsten"
            },

            // English-only models
            new ModelDefinition
            {
                Name = "base.en",
                FileName = "ggml-base.en.bin",
                SizeMB = 142,
                SHA1 = "137c40403d78fd54d454da0f9bd998f78703390c",
                DownloadURL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin",
                SpeedFactor = 7.0,
                VramGB = 1,
                Description = "Fast (142 MB) - Good for real-time"
            },
            new ModelDefinition
            {
                Name = "small.en",
                FileName = "ggml-small.en.bin",
                SizeMB = 466,
                SHA1 = "db8a495a91d927739e50b3fc1cc4c6b8f6c2d022",
                DownloadURL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.en.bin",
                SpeedFactor = 4.0,
                VramGB = 2,
                Description = "Recommended (466 MB) - Best balance ⭐"
            },
            new ModelDefinition
            {
                Name = "medium.en",
                FileName = "ggml-medium.en.bin",
                SizeMB = 1536,
                SHA1 = "8c30f0e44ce9560643ebd10bbe50cd20eafd3723",
                DownloadURL = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.en.bin",
                SpeedFactor = 2.0,
                VramGB = 5,
                Description = "High quality (1.5 GB) - Slower"
            }
        };
    }
}
