# Color Palette - Windows Native

**Purpose:** Define consistent colors across the application for native Windows look and feel
**Status:** Stable (v0.1)
**Last Updated:** 2025-11-18

---

## Windows 10/11 Standard Colors

**Usage:** All UI elements MUST use these exact colors for consistency and native feel.

### Primary Colors

```csharp
// Windows Brand Colors
public static class AppColors
{
    // Primary Actions (Buttons, Links, Active states)
    public static readonly Color AccentBlue = Color.FromRgb(0, 120, 212);      // #0078D4

    // Status Colors
    public static readonly Color SuccessGreen = Color.FromRgb(16, 124, 16);    // #107C10
    public static readonly Color WarningOrange = Color.FromRgb(255, 185, 0);   // #FFB900
    public static readonly Color ErrorRed = Color.FromRgb(232, 17, 35);        // #E81123

    // Text Colors
    public static readonly Color TextPrimary = Color.FromRgb(0, 0, 0);         // #000000
    public static readonly Color TextSecondary = Color.FromRgb(102, 102, 102); // #666666
    public static readonly Color TextDisabled = Color.FromRgb(161, 161, 161);  // #A1A1A1

    // Background & Borders
    public static readonly Color BackgroundWhite = Color.FromRgb(255, 255, 255); // #FFFFFF
    public static readonly Color BackgroundGray = Color.FromRgb(243, 243, 243);  // #F3F3F3
    public static readonly Color BorderGray = Color.FromRgb(209, 209, 209);      // #D1D1D1

    // Validation States
    public static readonly Color ValidationError = Color.FromRgb(232, 17, 35);   // #E81123 (same as ErrorRed)
    public static readonly Color ValidationSuccess = Color.FromRgb(16, 124, 16); // #107C10 (same as SuccessGreen)
}
```

---

## Usage Guidelines

### Buttons
- **Primary Action (Save):** `AccentBlue` background, white text
- **Secondary Action (Cancel):** `ErrorRed` background, white text
- **Disabled:** `TextDisabled` text, `BackgroundGray` background

### Text
- **Headings:** `TextPrimary` (black)
- **Body Text:** `TextSecondary` (gray)
- **Disabled Text:** `TextDisabled` (light gray)

### Status Indicators
- **Success:** `SuccessGreen` (e.g., "Model OK ✓")
- **Warning:** `WarningOrange` (e.g., "Hotkey conflict")
- **Error:** `ErrorRed` (e.g., "File not found")

### Borders & Backgrounds
- **Window Background:** `BackgroundWhite`
- **Section Background:** `BackgroundGray` (for grouped sections)
- **Borders:** `BorderGray`

### Validation States
- **Error Border:** `ValidationError` (red border on TextBox)
- **Success Indicator:** `ValidationSuccess` (green checkmark)

---

## WPF XAML Examples

### Button Styles
```xaml
<!-- Primary Button (Save) -->
<Button Background="#0078D4" Foreground="White" Content="Speichern"/>

<!-- Cancel Button -->
<Button Background="#E81123" Foreground="White" Content="Abbrechen"/>

<!-- Disabled Button -->
<Button Background="#F3F3F3" Foreground="#A1A1A1" IsEnabled="False" Content="Speichern"/>
```

### Validation Feedback
```xaml
<!-- Error State -->
<TextBox BorderBrush="#E81123" BorderThickness="2"/>
<TextBlock Foreground="#E81123" Text="⚠ Path not found"/>

<!-- Success State -->
<TextBlock Foreground="#107C10" Text="✓ Model OK"/>
```

---

## Configurable Colors

**Note:** While these are the default Windows colors, the color constants are defined in code (not XAML resources) to allow future customization if needed.

**Location:** `src/LocalWhisper/UI/Styles/AppColors.cs`

---

## Related Documents

- `docs/ui/settings-window-specification.md` - Settings window mockup
- `docs/ui/icon-style-guide.md` - Icon color usage

---

**Last updated:** 2025-11-18
