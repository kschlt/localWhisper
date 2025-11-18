using System.IO;
using LocalWhisper.Core;
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
/// See: docs/iterations/iteration-05b-download-repair.md (Task 3)
/// </remarks>
public class DataRootValidator
{
    /// <summary>
    /// Validate data root directory and configuration.
    /// </summary>
    /// <param name="dataRoot">Data root path to validate</param>
    /// <param name="config">App configuration</param>
    /// <returns>Validation result with errors and warnings</returns>
    public ValidationResult Validate(string dataRoot, AppConfig config)
    {
        var result = new ValidationResult();

        // Check existence
        if (!Directory.Exists(dataRoot))
        {
            result.IsValid = false;
            result.Errors.Add($"Data root does not exist: {dataRoot}");
            return result; // No point checking further if root doesn't exist
        }

        // Check folder structure
        var requiredFolders = new[] { "config", "models", "history", "logs", "tmp" };
        foreach (var folder in requiredFolders)
        {
            var path = System.IO.Path.Combine(dataRoot, folder);
            if (!Directory.Exists(path))
            {
                result.Warnings.Add($"Missing folder: {folder}");
            }
        }

        // Check config.toml
        var configPath = PathHelpers.GetConfigPath(dataRoot);
        if (!File.Exists(configPath))
        {
            result.IsValid = false;
            result.Errors.Add("config.toml not found");
        }

        // Check model file
        if (config.Whisper != null && !string.IsNullOrEmpty(config.Whisper.ModelPath))
        {
            var modelPath = config.Whisper.ModelPath;
            if (!File.Exists(modelPath))
            {
                result.IsValid = false;
                result.Errors.Add($"Model file not found: {modelPath}");
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}
