using System;
using System.IO;
using System.Windows.Input;
using LocalWhisper.Models;
using LocalWhisper.Utils;

namespace LocalWhisper.Core;

/// <summary>
/// Manages first-run wizard orchestration.
/// </summary>
/// <remarks>
/// Implements US-040, US-041a, US-042: Wizard Steps 1-3
/// - Creates data root folder structure
/// - Copies model file to models/ directory
/// - Generates initial config.toml
/// - Validates all wizard inputs
///
/// See: docs/iterations/iteration-05a-wizard-core.md (Task 8)
/// </remarks>
public class WizardManager
{
    /// <summary>
    /// Create data root folder structure.
    /// </summary>
    /// <param name="dataRoot">Data root directory path</param>
    public void CreateDataRootStructure(string dataRoot)
    {
        var folders = new[] { "config", "models", "history", "logs", "tmp", "tmp/failed" };

        foreach (var folder in folders)
        {
            var path = Path.Combine(dataRoot, folder);
            Directory.CreateDirectory(path);
            AppLogger.LogInformation($"Created folder: {folder}", new { Path = path });
        }

        AppLogger.LogInformation("Data root structure created", new { DataRoot = dataRoot });
    }

    /// <summary>
    /// Copy model file to models/ directory.
    /// </summary>
    /// <param name="sourceFilePath">Source model file path</param>
    /// <param name="dataRoot">Data root directory</param>
    /// <param name="model">Model definition</param>
    /// <returns>Destination file path</returns>
    public string CopyModelFile(string sourceFilePath, string dataRoot, ModelDefinition model)
    {
        var modelsDir = Path.Combine(dataRoot, "models");
        var destFilePath = Path.Combine(modelsDir, model.FileName);

        // Ensure models directory exists
        Directory.CreateDirectory(modelsDir);

        // Copy file
        File.Copy(sourceFilePath, destFilePath, overwrite: true);

        AppLogger.LogInformation("Model file copied", new
        {
            Source = sourceFilePath,
            Destination = destFilePath,
            Model = model.Name
        });

        return destFilePath;
    }

    /// <summary>
    /// Generate initial config.toml from wizard results.
    /// </summary>
    /// <param name="dataRoot">Data root directory</param>
    /// <param name="sourceModelFilePath">Path to source model file (will be copied to models/ folder)</param>
    /// <param name="language">Selected language (de/en)</param>
    /// <param name="hotkeyModifiers">Hotkey modifiers</param>
    /// <param name="hotkeyKey">Hotkey main key</param>
    /// <param name="postProcessingEnabled">Enable post-processing (Iteration 7)</param>
    public void GenerateInitialConfig(
        string dataRoot,
        string sourceModelFilePath,
        string language,
        ModifierKeys hotkeyModifiers,
        Key hotkeyKey,
        bool postProcessingEnabled = false)
    {
        // Check source file exists
        if (!File.Exists(sourceModelFilePath))
        {
            throw new FileNotFoundException($"Source model file not found: {sourceModelFilePath}");
        }

        // Security: Validate source path to prevent path traversal
        var fullSourcePath = Path.GetFullPath(sourceModelFilePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException($"Invalid source path (after normalization): {fullSourcePath}");
        }

        // Copy model file to models/ directory
        var modelFileName = Path.GetFileName(fullSourcePath); // Use normalized path
        if (string.IsNullOrWhiteSpace(modelFileName) || modelFileName.Contains(".."))
        {
            throw new ArgumentException($"Invalid model filename: {modelFileName}");
        }

        var modelsDir = Path.Combine(dataRoot, "models");
        var destModelPath = Path.GetFullPath(Path.Combine(modelsDir, modelFileName));

        // Security: Ensure destination is within data root
        if (!destModelPath.StartsWith(Path.GetFullPath(dataRoot)))
        {
            throw new ArgumentException($"Invalid destination path (outside data root): {destModelPath}");
        }

        Directory.CreateDirectory(modelsDir);
        File.Copy(fullSourcePath, destModelPath, overwrite: true);

        AppLogger.LogInformation("Model file copied to data root", new
        {
            Source = sourceModelFilePath,
            Destination = destModelPath
        });

        // Convert ModifierKeys to list of strings
        var modifiers = new System.Collections.Generic.List<string>();
        if (hotkeyModifiers.HasFlag(ModifierKeys.Control))
            modifiers.Add("Ctrl");
        if (hotkeyModifiers.HasFlag(ModifierKeys.Alt))
            modifiers.Add("Alt");
        if (hotkeyModifiers.HasFlag(ModifierKeys.Shift))
            modifiers.Add("Shift");
        if (hotkeyModifiers.HasFlag(ModifierKeys.Windows))
            modifiers.Add("Win");

        // Create config
        var config = new AppConfig
        {
            DataRoot = dataRoot,
            Language = language,
            FileFormat = ".md",
            Hotkey = new HotkeyConfig
            {
                Modifiers = modifiers,
                Key = hotkeyKey.ToString()
            },
            Whisper = new WhisperConfig
            {
                CLIPath = "whisper-cli",
                ModelPath = destModelPath, // Use destination path
                Language = language,
                TimeoutSeconds = 60
            },
            PostProcessing = new PostProcessingConfig
            {
                Enabled = postProcessingEnabled,
                LlmCliPath = string.Empty,  // Configured later in Settings or via download
                ModelPath = string.Empty,   // Configured later in Settings or via download
                TimeoutSeconds = 5,
                GpuAcceleration = true,
                UseGlossary = false,
                GlossaryPath = string.Empty,
                Temperature = 0.0f,
                TopP = 0.25f,
                RepeatPenalty = 1.05f,
                MaxTokens = 512
            }
        };

        // Validate
        config.Hotkey.Validate();
        config.Whisper.Validate();
        // Note: PostProcessing validation skipped if disabled (paths can be empty)

        // Save
        var configPath = PathHelpers.GetConfigPath(dataRoot);
        ConfigManager.Save(configPath, config);

        AppLogger.LogInformation("Initial config created", new
        {
            ConfigPath = configPath,
            Language = language,
            ModelPath = destModelPath,
            Hotkey = UI.Controls.HotkeyTextBox.FormatHotkey(hotkeyModifiers, hotkeyKey)
        });
    }

    /// <summary>
    /// Validate data root directory for write access.
    /// </summary>
    /// <param name="path">Directory path to validate</param>
    /// <returns>True if writable, false otherwise</returns>
    public bool ValidateDataRoot(string path)
    {
        try
        {
            // Check if directory exists
            if (!Directory.Exists(path))
            {
                AppLogger.LogWarning("Data root directory does not exist", new { Path = path });
                return false;
            }

            // Test write access
            var testFile = Path.Combine(path, ".write_test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            AppLogger.LogWarning("No write access to data root", new { Path = path });
            return false;
        }
        catch (IOException ex)
        {
            AppLogger.LogError("Cannot access data root", ex, new { Path = path });
            return false;
        }
    }
}
