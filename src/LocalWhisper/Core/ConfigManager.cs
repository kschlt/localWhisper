using System.IO;
using LocalWhisper.Models;
using Tomlyn;
using Tomlyn.Model;

namespace LocalWhisper.Core;

/// <summary>
/// Manages loading and saving configuration files (TOML format).
/// </summary>
/// <remarks>
/// Iteration 1: Minimal config schema ([hotkey] section only).
/// Iteration 3: Added [whisper] section for STT configuration.
/// Iteration 5: Full schema with all sections.
/// Iteration 6: Expanded schema for Settings (data_root, language, file_format).
///
/// See: docs/meta/placeholders-tracker.md (PH-003)
/// See: docs/specification/data-structures.md (lines 49-110)
/// </remarks>
public static class ConfigManager
{
    /// <summary>
    /// Load configuration from TOML file.
    /// </summary>
    /// <param name="configPath">Path to config.toml file</param>
    /// <returns>Loaded configuration, or default if file doesn't exist</returns>
    /// <exception cref="Exception">If TOML syntax is invalid</exception>
    /// <exception cref="InvalidOperationException">If configuration validation fails</exception>
    public static AppConfig Load(string configPath)
    {
        // Return defaults if file doesn't exist
        if (!File.Exists(configPath))
        {
            AppLogger.LogInformation("Config file not found, using defaults", new { ConfigPath = configPath });
            return GetDefault();
        }

        try
        {
            // Read and parse TOML
            var tomlContent = File.ReadAllText(configPath);
            var tomlTable = Toml.ToModel(tomlContent);

            // Build config object
            var config = new AppConfig();

            // Parse [hotkey] section
            if (tomlTable.ContainsKey("hotkey") && tomlTable["hotkey"] is TomlTable hotkeyTable)
            {
                config.Hotkey = new HotkeyConfig
                {
                    Modifiers = ParseStringArray(hotkeyTable, "modifiers", new List<string> { "Ctrl", "Shift" }),
                    Key = ParseString(hotkeyTable, "key", "D")
                };
            }

            // Parse data_root (Iteration 6)
            config.DataRoot = ParseString(tomlTable, "data_root", string.Empty);

            // Parse language (Iteration 6) - UI language
            config.Language = ParseString(tomlTable, "language", "de");

            // Parse file_format (Iteration 6)
            config.FileFormat = ParseString(tomlTable, "file_format", ".md");

            // Parse [whisper] section (Iteration 3)
            if (tomlTable.ContainsKey("whisper") && tomlTable["whisper"] is TomlTable whisperTable)
            {
                config.Whisper = new WhisperConfig
                {
                    CLIPath = ParseString(whisperTable, "cli_path", "whisper-cli"),
                    ModelPath = ParseString(whisperTable, "model_path", ""),
                    Language = ParseString(whisperTable, "language", "de"),
                    TimeoutSeconds = ParseInt(whisperTable, "timeout_seconds", 60)
                };
            }

            // Parse [postprocessing] section (Iteration 7)
            if (tomlTable.ContainsKey("postprocessing") && tomlTable["postprocessing"] is TomlTable ppTable)
            {
                config.PostProcessing = new PostProcessingConfig
                {
                    Enabled = ParseBool(ppTable, "enabled", false),
                    LlmCliPath = ParseString(ppTable, "llm_cli_path", ""),
                    ModelPath = ParseString(ppTable, "model_path", ""),
                    TimeoutSeconds = ParseInt(ppTable, "timeout_seconds", 5),
                    GpuAcceleration = ParseBool(ppTable, "gpu_acceleration", true),
                    UseGlossary = ParseBool(ppTable, "use_glossary", false),
                    GlossaryPath = ParseString(ppTable, "glossary_path", ""),
                    Temperature = ParseFloat(ppTable, "temperature", 0.0f),
                    TopP = ParseFloat(ppTable, "top_p", 0.25f),
                    RepeatPenalty = ParseFloat(ppTable, "repeat_penalty", 1.05f),
                    MaxTokens = ParseInt(ppTable, "max_tokens", 512)
                };
            }

            // Validate configuration
            config.Hotkey.Validate();
            if (config.Whisper != null)
            {
                config.Whisper.Validate();
            }
            config.PostProcessing.Validate();

            AppLogger.LogInformation("Config loaded successfully", new { ConfigPath = configPath });
            return config;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // Wrap parsing exceptions with more context
            throw new Exception($"Failed to parse config.toml at '{configPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Save configuration to TOML file.
    /// </summary>
    /// <param name="configPath">Path to config.toml file</param>
    /// <param name="config">Configuration to save</param>
    /// <param name="skipPostProcessingValidation">Skip PostProcessing validation (used during wizard when paths not yet configured)</param>
    public static void Save(string configPath, AppConfig config, bool skipPostProcessingValidation = false)
    {
        // Validate before saving
        config.Hotkey.Validate();
        if (config.Whisper != null)
        {
            config.Whisper.Validate();
        }

        // Skip PostProcessing validation during wizard (paths configured later)
        if (!skipPostProcessingValidation)
        {
            config.PostProcessing.Validate();
        }

        // Build TomlArray from modifiers list
        var modifiersArray = new TomlArray();
        foreach (var modifier in config.Hotkey.Modifiers)
        {
            modifiersArray.Add(modifier);
        }

        // Build TOML structure
        var tomlTable = new TomlTable
        {
            ["hotkey"] = new TomlTable
            {
                ["modifiers"] = modifiersArray,
                ["key"] = config.Hotkey.Key
            }
        };

        // Add data_root (Iteration 6)
        if (!string.IsNullOrEmpty(config.DataRoot))
        {
            tomlTable["data_root"] = config.DataRoot;
        }

        // Add language (Iteration 6) - UI language
        tomlTable["language"] = config.Language;

        // Add file_format (Iteration 6)
        tomlTable["file_format"] = config.FileFormat;

        // Add [whisper] section (Iteration 3)
        if (config.Whisper != null)
        {
            tomlTable["whisper"] = new TomlTable
            {
                ["cli_path"] = config.Whisper.CLIPath,
                ["model_path"] = config.Whisper.ModelPath,
                ["language"] = config.Whisper.Language,
                ["timeout_seconds"] = config.Whisper.TimeoutSeconds
            };
        }

        // Add [postprocessing] section (Iteration 7)
        tomlTable["postprocessing"] = new TomlTable
        {
            ["enabled"] = config.PostProcessing.Enabled,
            ["llm_cli_path"] = config.PostProcessing.LlmCliPath,
            ["model_path"] = config.PostProcessing.ModelPath,
            ["timeout_seconds"] = config.PostProcessing.TimeoutSeconds,
            ["gpu_acceleration"] = config.PostProcessing.GpuAcceleration,
            ["use_glossary"] = config.PostProcessing.UseGlossary,
            ["glossary_path"] = config.PostProcessing.GlossaryPath,
            ["temperature"] = config.PostProcessing.Temperature,
            ["top_p"] = config.PostProcessing.TopP,
            ["repeat_penalty"] = config.PostProcessing.RepeatPenalty,
            ["max_tokens"] = config.PostProcessing.MaxTokens
        };

        // Serialize to TOML string
        var tomlContent = Toml.FromModel(tomlTable);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to file
        File.WriteAllText(configPath, tomlContent);

        AppLogger.LogInformation("Config saved successfully", new { ConfigPath = configPath });
    }

    /// <summary>
    /// Get default configuration.
    /// </summary>
    public static AppConfig GetDefault()
    {
        return new AppConfig
        {
            Hotkey = new HotkeyConfig
            {
                Modifiers = new List<string> { "Ctrl", "Shift" },
                Key = "D"
            },
            DataRoot = string.Empty,
            Language = "de",
            FileFormat = ".md",
            Whisper = new WhisperConfig
            {
                CLIPath = "whisper-cli",
                ModelPath = "",
                Language = "de",
                TimeoutSeconds = 60
            }
        };
    }

    // Helper methods for TOML parsing

    private static List<string> ParseStringArray(TomlTable table, string key, List<string> defaultValue)
    {
        if (!table.ContainsKey(key))
        {
            return defaultValue;
        }

        if (table[key] is TomlArray array)
        {
            var result = new List<string>();
            foreach (var item in array)
            {
                if (item is string str)
                {
                    result.Add(str);
                }
            }
            // Return the parsed array even if empty - validation will catch invalid configs
            return result;
        }

        return defaultValue;
    }

    private static string ParseString(TomlTable table, string key, string defaultValue)
    {
        if (!table.ContainsKey(key))
        {
            return defaultValue;
        }

        return table[key] is string str ? str : defaultValue;
    }

    private static int ParseInt(TomlTable table, string key, int defaultValue)
    {
        if (!table.ContainsKey(key))
        {
            return defaultValue;
        }

        // TOML integers are stored as long
        if (table[key] is long longValue)
        {
            return (int)longValue;
        }

        return defaultValue;
    }

    private static bool ParseBool(TomlTable table, string key, bool defaultValue)
    {
        if (!table.ContainsKey(key))
        {
            return defaultValue;
        }

        return table[key] is bool boolValue ? boolValue : defaultValue;
    }

    private static float ParseFloat(TomlTable table, string key, float defaultValue)
    {
        if (!table.ContainsKey(key))
        {
            return defaultValue;
        }

        // TOML floats are stored as double
        if (table[key] is double doubleValue)
        {
            return (float)doubleValue;
        }

        // Also support integers cast to float
        if (table[key] is long longValue)
        {
            return (float)longValue;
        }

        return defaultValue;
    }
}
