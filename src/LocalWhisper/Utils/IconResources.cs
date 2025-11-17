using System.Windows.Media;
using LocalWhisper.Models;

namespace LocalWhisper.Utils;

/// <summary>
/// Icon constants and helpers using Segoe MDL2 Assets font.
/// </summary>
/// <remarks>
/// See: docs/ui/icon-style-guide.md
/// </remarks>
public static class IconResources
{
    // Tray icon states
    public const string IdleIcon = "\uE91F";       // Circle (gray)
    public const string RecordingIcon = "\uE7C8";  // RecordSolid (red)
    public const string ProcessingIcon = "\uEB9E"; // ProgressRingDots (blue)

    // Dialog icons
    public const string ErrorIcon = "\uEA39";      // ErrorBadge (red)
    public const string WarningIcon = "\uE7BA";    // Warning (yellow)
    public const string InfoIcon = "\uE946";       // Info (blue)
    public const string SuccessIcon = "\uE73E";    // CheckMark (green)

    // Context menu icons
    public const string SettingsIcon = "\uE713";   // Settings
    public const string HistoryIcon = "\uE81C";    // History
    public const string ExitIcon = "\uE711";       // Cancel
    public const string DeleteIcon = "\uE74D";     // Delete

    // Colors
    public static readonly SolidColorBrush IdleColor = new(Color.FromRgb(128, 128, 128)); // Gray
    public static readonly SolidColorBrush RecordingColor = new(Color.FromRgb(209, 52, 56)); // Red
    public static readonly SolidColorBrush ProcessingColor = new(Color.FromRgb(0, 120, 212)); // Blue
    public static readonly SolidColorBrush ErrorColor = new(Color.FromRgb(209, 52, 56)); // Red
    public static readonly SolidColorBrush WarningColor = new(Color.FromRgb(255, 200, 61)); // Yellow
    public static readonly SolidColorBrush InfoColor = new(Color.FromRgb(0, 120, 212)); // Blue
    public static readonly SolidColorBrush SuccessColor = new(Color.FromRgb(16, 124, 16)); // Green

    /// <summary>
    /// Get icon glyph for given app state.
    /// </summary>
    public static string GetStateIcon(AppState state)
    {
        return state switch
        {
            AppState.Idle => IdleIcon,
            AppState.Recording => RecordingIcon,
            AppState.Processing => ProcessingIcon,
            _ => IdleIcon
        };
    }

    /// <summary>
    /// Get icon color for given app state.
    /// </summary>
    public static SolidColorBrush GetStateColor(AppState state)
    {
        return state switch
        {
            AppState.Idle => IdleColor,
            AppState.Recording => RecordingColor,
            AppState.Processing => ProcessingColor,
            _ => IdleColor
        };
    }

    /// <summary>
    /// Get localized tooltip for given app state.
    /// </summary>
    /// <param name="state">Application state</param>
    /// <param name="language">UI language ("de" or "en")</param>
    public static string GetStateTooltip(AppState state, string language = "de")
    {
        if (language == "de")
        {
            return state switch
            {
                AppState.Idle => "LocalWhisper: Bereit",
                AppState.Recording => "LocalWhisper: Aufnahme...",
                AppState.Processing => "LocalWhisper: Verarbeitung...",
                _ => "LocalWhisper"
            };
        }
        else // English
        {
            return state switch
            {
                AppState.Idle => "LocalWhisper: Ready",
                AppState.Recording => "LocalWhisper: Recording...",
                AppState.Processing => "LocalWhisper: Processing...",
                _ => "LocalWhisper"
            };
        }
    }
}
