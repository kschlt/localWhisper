using System.IO;

namespace LocalWhisper.Utils;

/// <summary>
/// Helper class for path resolution and data root management.
/// </summary>
public static class PathHelpers
{
    /// <summary>
    /// Get the default data root path.
    /// </summary>
    /// <returns>%LOCALAPPDATA%\LocalWhisper\</returns>
    /// <remarks>
    /// TODO(PH-004, Iter-5): User-chosen path from wizard.
    /// For Iteration 1, this is hardcoded to LOCALAPPDATA.
    /// See: docs/meta/placeholders-tracker.md (PH-004)
    /// </remarks>
    public static string GetDataRoot()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "LocalWhisper");
    }

    /// <summary>
    /// Ensure the data root folder structure exists.
    /// Creates: config/, logs/ subdirectories.
    /// </summary>
    /// <remarks>
    /// Iteration 1 creates minimal structure (config/, logs/).
    /// Iteration 5 will add: models/, history/, tmp/
    /// </remarks>
    public static void EnsureDataRootExists(string dataRoot)
    {
        // Create base directory
        Directory.CreateDirectory(dataRoot);

        // Create subdirectories for Iteration 1
        Directory.CreateDirectory(Path.Combine(dataRoot, "config"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "logs"));

        // Future iterations will add:
        // Directory.CreateDirectory(Path.Combine(dataRoot, "models"));
        // Directory.CreateDirectory(Path.Combine(dataRoot, "history"));
        // Directory.CreateDirectory(Path.Combine(dataRoot, "tmp"));
    }

    /// <summary>
    /// Get the path to the config file.
    /// </summary>
    public static string GetConfigPath(string dataRoot)
    {
        return Path.Combine(dataRoot, "config", "config.toml");
    }

    /// <summary>
    /// Get the path to the logs directory.
    /// </summary>
    public static string GetLogsPath(string dataRoot)
    {
        return Path.Combine(dataRoot, "logs");
    }

    /// <summary>
    /// Get the path to the history directory.
    /// </summary>
    public static string GetHistoryPath(string dataRoot)
    {
        return Path.Combine(dataRoot, "history");
    }

    /// <summary>
    /// Get the path to the models directory.
    /// </summary>
    public static string GetModelsPath(string dataRoot)
    {
        return Path.Combine(dataRoot, "models");
    }

    /// <summary>
    /// Get the path to the tmp directory.
    /// </summary>
    public static string GetTmpPath(string dataRoot)
    {
        return Path.Combine(dataRoot, "tmp");
    }
}
