using System.Windows.Media;

namespace LocalWhisper.UI.Styles;

/// <summary>
/// Windows 10/11 native color palette for consistent, native-looking UI.
/// </summary>
/// <remarks>
/// Colors match Windows design system for familiar user experience.
/// See: docs/ui/color-palette.md
/// Iteration 6: Settings window styling
/// </remarks>
public static class AppColors
{
    // Primary Actions

    /// <summary>
    /// Accent Blue - Primary action color (#0078D4).
    /// Used for buttons, links, and primary UI elements.
    /// </summary>
    public static readonly Color AccentBlue = Color.FromRgb(0, 120, 212);

    /// <summary>
    /// Accent Blue Hover - Hover state for accent blue (#005A9E).
    /// </summary>
    public static readonly Color AccentBlueHover = Color.FromRgb(0, 90, 158);

    /// <summary>
    /// Accent Blue Pressed - Pressed state for accent blue (#004578).
    /// </summary>
    public static readonly Color AccentBluePressed = Color.FromRgb(0, 69, 120);

    // Status Colors

    /// <summary>
    /// Success Green - Success states and confirmations (#107C10).
    /// </summary>
    public static readonly Color SuccessGreen = Color.FromRgb(16, 124, 16);

    /// <summary>
    /// Warning Orange - Warnings and non-critical alerts (#FFB900).
    /// </summary>
    public static readonly Color WarningOrange = Color.FromRgb(255, 185, 0);

    /// <summary>
    /// Error Red - Error states and destructive actions (#E81123).
    /// </summary>
    public static readonly Color ErrorRed = Color.FromRgb(232, 17, 35);

    /// <summary>
    /// Info Blue - Informational messages (#0099BC).
    /// </summary>
    public static readonly Color InfoBlue = Color.FromRgb(0, 153, 188);

    // Text Colors

    /// <summary>
    /// Text Primary - Main text color (#000000).
    /// </summary>
    public static readonly Color TextPrimary = Color.FromRgb(0, 0, 0);

    /// <summary>
    /// Text Secondary - Secondary/helper text color (#666666).
    /// </summary>
    public static readonly Color TextSecondary = Color.FromRgb(102, 102, 102);

    /// <summary>
    /// Text Disabled - Disabled text color (#A0A0A0).
    /// </summary>
    public static readonly Color TextDisabled = Color.FromRgb(160, 160, 160);

    /// <summary>
    /// Text On Accent - Text color on accent backgrounds (#FFFFFF).
    /// </summary>
    public static readonly Color TextOnAccent = Color.FromRgb(255, 255, 255);

    // Background Colors

    /// <summary>
    /// Background - Main background color (#FFFFFF).
    /// </summary>
    public static readonly Color Background = Color.FromRgb(255, 255, 255);

    /// <summary>
    /// Background Alt - Alternate background for sections (#F3F3F3).
    /// </summary>
    public static readonly Color BackgroundAlt = Color.FromRgb(243, 243, 243);

    /// <summary>
    /// Background Disabled - Disabled background (#F0F0F0).
    /// </summary>
    public static readonly Color BackgroundDisabled = Color.FromRgb(240, 240, 240);

    // Border Colors

    /// <summary>
    /// Border - Default border color (#D1D1D1).
    /// </summary>
    public static readonly Color Border = Color.FromRgb(209, 209, 209);

    /// <summary>
    /// Border Hover - Hover state border (#A0A0A0).
    /// </summary>
    public static readonly Color BorderHover = Color.FromRgb(160, 160, 160);

    /// <summary>
    /// Border Focus - Focused element border (AccentBlue).
    /// </summary>
    public static readonly Color BorderFocus = AccentBlue;

    // Helper Methods

    /// <summary>
    /// Convert Color to SolidColorBrush.
    /// </summary>
    public static SolidColorBrush ToBrush(Color color)
    {
        return new SolidColorBrush(color);
    }

    /// <summary>
    /// Get brush for accent blue.
    /// </summary>
    public static SolidColorBrush AccentBlueBrush => ToBrush(AccentBlue);

    /// <summary>
    /// Get brush for success green.
    /// </summary>
    public static SolidColorBrush SuccessGreenBrush => ToBrush(SuccessGreen);

    /// <summary>
    /// Get brush for warning orange.
    /// </summary>
    public static SolidColorBrush WarningOrangeBrush => ToBrush(WarningOrange);

    /// <summary>
    /// Get brush for error red.
    /// </summary>
    public static SolidColorBrush ErrorRedBrush => ToBrush(ErrorRed);

    /// <summary>
    /// Get brush for info blue.
    /// </summary>
    public static SolidColorBrush InfoBlueBrush => ToBrush(InfoBlue);

    /// <summary>
    /// Get brush for text primary.
    /// </summary>
    public static SolidColorBrush TextPrimaryBrush => ToBrush(TextPrimary);

    /// <summary>
    /// Get brush for text secondary.
    /// </summary>
    public static SolidColorBrush TextSecondaryBrush => ToBrush(TextSecondary);

    /// <summary>
    /// Get brush for background.
    /// </summary>
    public static SolidColorBrush BackgroundBrush => ToBrush(Background);

    /// <summary>
    /// Get brush for border.
    /// </summary>
    public static SolidColorBrush BorderBrush => ToBrush(Border);
}
