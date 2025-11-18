using System.IO;
using LocalWhisper.Models;
using LocalWhisper.Utils;

namespace LocalWhisper.Services;

/// <summary>
/// Validates data root folder structure and configuration.
/// </summary>
/// <remarks>
/// Implements US-043: Repair Flow (Data Root Missing)
/// - Checks data root existence
/// - Validates folder structure
/// - Verifies config.toml exists
/// - Verifies model file exists
/// - Returns detailed errors and warnings
///
/// Used in Settings window (US-051) and Repair flow (US-043).
/// See: docs/iterations/iteration-05b-download-repair.md (Task 3)
/// See: docs/iterations/iteration-06-settings.md
/// </remarks>
public class DataRootValidator
{
    /// <summary>
    /// Validate data root directory structure.
    /// </summary>
    /// <param name="dataRoot">Data root path to validate</param>
    /// <returns>Validation result with errors and warnings</returns>
    public ValidationResult ValidateStructure(string dataRoot)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(dataRoot))
        {
            result.AddError("Data root path is empty");
            return result;
        }

        if (!Directory.Exists(dataRoot))
        {
            result.AddError($"Data root does not exist: {dataRoot}");
            return result; // No point checking further
        }

        // Check folder structure
        var requiredFolders = new[] { "config", "models" };
        var optionalFolders = new[] { "history", "logs", "tmp" };

        foreach (var folder in requiredFolders)
        {
            var path = Path.Combine(dataRoot, folder);
            if (!Directory.Exists(path))
            {
                result.AddError($"Missing required folder: {folder}");
            }
        }

        foreach (var folder in optionalFolders)
        {
            var path = Path.Combine(dataRoot, folder);
            if (!Directory.Exists(path))
            {
                result.AddWarning($"Missing optional folder: {folder} (will be created)");
            }
        }

        return result;
    }

    /// <summary>
    /// Validate data root directory and verify config file exists.
    /// </summary>
    /// <param name="dataRoot">Data root path to validate</param>
    /// <param name="checkConfig">Whether to check for config.toml existence</param>
    /// <returns>Validation result with errors and warnings</returns>
    public ValidationResult Validate(string dataRoot, bool checkConfig = false)
    {
        var result = ValidateStructure(dataRoot);

        if (!result.IsValid)
        {
            return result;
        }

        // Check config.toml if requested
        if (checkConfig)
        {
            var configPath = PathHelpers.GetConfigPath(dataRoot);
            if (!File.Exists(configPath))
            {
                result.AddError("config.toml not found");
            }
        }

        return result;
    }

    /// <summary>
    /// Validate data root directory and configuration.
    /// </summary>
    /// <param name="dataRoot">Data root path to validate</param>
    /// <param name="config">App configuration</param>
    /// <returns>Validation result with errors and warnings</returns>
    public ValidationResult Validate(string dataRoot, AppConfig config)
    {
        var result = Validate(dataRoot, checkConfig: true);

        if (!result.IsValid)
        {
            return result;
        }

        // Check model file
        if (config.Whisper != null && !string.IsNullOrEmpty(config.Whisper.ModelPath))
        {
            var modelPath = config.Whisper.ModelPath;
            if (!File.Exists(modelPath))
            {
                result.AddError($"Model file not found: {modelPath}");
            }
        }
        else if (config.Whisper == null)
        {
            result.AddError("Whisper configuration is missing");
        }
        else if (string.IsNullOrEmpty(config.Whisper.ModelPath))
        {
            result.AddError("Model path is not configured");
        }

        return result;
    }
}
