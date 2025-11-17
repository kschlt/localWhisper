# Icon Style Guide

**Purpose:** Define visual appearance and implementation of all app icons
**Audience:** Developers, designers, AI agents
**Status:** Normative (implementation must follow these specifications)
**Last Updated:** 2025-11-17

---

## Design System

**Font Family:** Segoe MDL2 Assets (built-in Windows 10/11 icon font)

**Rationale:**
- ✅ Available on all Windows 10+ systems (no external dependencies)
- ✅ Vector-based (scales perfectly at any DPI)
- ✅ Consistent with Windows UI language
- ✅ No licensing concerns
- ✅ Easy to implement in WPF (TextBlock with FontFamily)

**Alternative:** If Segoe MDL2 is unavailable (older Windows), fall back to Unicode symbols (○, ●, ⟳).

---

## Tray Icon States

### State: Idle

**Visual:** Empty circle (gray)

**Unicode:** `\uE91F` (Circle)

**Color:** `#808080` (Gray)

**XAML Implementation:**
```xaml
<TextBlock
    FontFamily="Segoe MDL2 Assets"
    Text="&#xE91F;"
    Foreground="#808080"
    FontSize="16" />
```

**C# Constant:**
```csharp
public static class IconResources
{
    public const string IdleIcon = "\uE91F";
    public static readonly Brush IdleColor = new SolidColorBrush(Color.FromRgb(128, 128, 128));
}
```

**Tooltip:** `"LocalWhisper: Bereit"` (German) / `"LocalWhisper: Ready"` (English)

---

### State: Recording

**Visual:** Solid circle (red)

**Unicode:** `\uE7C8` (RecordSolid)

**Color:** `#D13438` (Red - Windows accent red)

**XAML Implementation:**
```xaml
<TextBlock
    FontFamily="Segoe MDL2 Assets"
    Text="&#xE7C8;"
    Foreground="#D13438"
    FontSize="16" />
```

**C# Constant:**
```csharp
public const string RecordingIcon = "\uE7C8";
public static readonly Brush RecordingColor = new SolidColorBrush(Color.FromRgb(209, 52, 56));
```

**Tooltip:** `"LocalWhisper: Aufnahme..."` (German) / `"LocalWhisper: Recording..."` (English)

**Animation (optional):** Subtle pulse effect (opacity 80% ↔ 100%, 1s cycle)

---

### State: Processing

**Visual:** Progress ring (blue)

**Unicode:** `\uE9F3` (ProgressRing) or `\uEB9E` (ProgressRingDots)

**Color:** `#0078D4` (Blue - Windows accent blue)

**XAML Implementation:**
```xaml
<TextBlock
    FontFamily="Segoe MDL2 Assets"
    Text="&#xEB9E;"
    Foreground="#0078D4"
    FontSize="16">
    <TextBlock.RenderTransform>
        <RotateTransform x:Name="ProcessingRotation" CenterX="8" CenterY="8" />
    </TextBlock.RenderTransform>
    <TextBlock.Triggers>
        <EventTrigger RoutedEvent="Loaded">
            <BeginStoryboard>
                <Storyboard RepeatBehavior="Forever">
                    <DoubleAnimation
                        Storyboard.TargetName="ProcessingRotation"
                        Storyboard.TargetProperty="Angle"
                        From="0" To="360" Duration="0:0:1.5" />
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </TextBlock.Triggers>
</TextBlock>
```

**C# Constant:**
```csharp
public const string ProcessingIcon = "\uEB9E";
public static readonly Brush ProcessingColor = new SolidColorBrush(Color.FromRgb(0, 120, 212));
```

**Tooltip:** `"LocalWhisper: Verarbeitung..."` (German) / `"LocalWhisper: Processing..."` (English)

**Animation:** Continuous rotation (360° in 1.5 seconds)

---

## Dialog Icons

### Error Dialog

**Visual:** Error badge (red X)

**Unicode:** `\uEA39` (ErrorBadge)

**Color:** `#D13438` (Red)

**Usage:** Shown in error dialogs for microphone issues, model errors, etc.

```csharp
public const string ErrorIcon = "\uEA39";
```

---

### Warning Dialog

**Visual:** Warning (yellow triangle)

**Unicode:** `\uE7BA` (Warning)

**Color:** `#FFC83D` (Yellow/Orange)

**Usage:** Shown for hotkey conflicts, repair prompts, confirmation dialogs.

```csharp
public const string WarningIcon = "\uE7BA";
```

---

### Info Dialog

**Visual:** Info badge (blue i)

**Unicode:** `\uE946` (Info)

**Color:** `#0078D4` (Blue)

**Usage:** Shown for informational messages.

```csharp
public const string InfoIcon = "\uE946";
```

---

### Success Dialog

**Visual:** Checkmark (green)

**Unicode:** `\uE73E` (CheckMark)

**Color:** `#107C10` (Green)

**Usage:** Shown for successful operations (model verified, setup complete).

```csharp
public const string SuccessIcon = "\uE73E";
```

---

## Context Menu Icons

### Settings

**Unicode:** `\uE713` (Settings)

**Usage:** Tray context menu → "Einstellungen..."

---

### History

**Unicode:** `\uE81C` (History)

**Usage:** Tray context menu → "Verlauf anzeigen..."

---

### Exit

**Unicode:** `\uE711` (Cancel/Close)

**Usage:** Tray context menu → "Beenden"

---

### Reset/Uninstall

**Unicode:** `\uE74D` (Delete)

**Usage:** Tray context menu → "Zurücksetzen/Deinstallieren..."

---

## Implementation Helper Class

**File:** `src/LocalWhisper/Utils/IconResources.cs`

```csharp
using System.Windows.Media;

namespace LocalWhisper.Utils
{
    /// <summary>
    /// Icon constants and helpers using Segoe MDL2 Assets font.
    /// See: docs/ui/icon-style-guide.md
    /// </summary>
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
        public static readonly Brush IdleColor = new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Gray
        public static readonly Brush RecordingColor = new SolidColorBrush(Color.FromRgb(209, 52, 56)); // Red
        public static readonly Brush ProcessingColor = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Blue
        public static readonly Brush ErrorColor = new SolidColorBrush(Color.FromRgb(209, 52, 56)); // Red
        public static readonly Brush WarningColor = new SolidColorBrush(Color.FromRgb(255, 200, 61)); // Yellow
        public static readonly Brush InfoColor = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Blue
        public static readonly Brush SuccessColor = new SolidColorBrush(Color.FromRgb(16, 124, 16)); // Green

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
        public static Brush GetStateColor(AppState state)
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
        public static string GetStateTooltip(AppState state, string language)
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
}
```

---

## Fallback Strategy (Older Windows Versions)

If Segoe MDL2 Assets is not available (pre-Windows 10), fall back to Unicode characters:

| State | Fallback Unicode | Character |
|-------|------------------|-----------|
| Idle | `\u25CB` | ○ |
| Recording | `\u25CF` | ● |
| Processing | `\u27F3` | ⟳ |

**Detection:**
```csharp
public static bool IsSegoeMDL2Available()
{
    var fonts = System.Drawing.FontFamily.Families;
    return fonts.Any(f => f.Name == "Segoe MDL2 Assets");
}
```

---

## Icon Sizes

| Context | Font Size | DPI-Aware |
|---------|-----------|-----------|
| Tray Icon (16x16) | 16pt | Yes (scales to 32pt at 200% DPI) |
| Dialog Icon (32x32) | 32pt | Yes |
| Context Menu Icon (16x16) | 14pt | Yes |

**Implementation Note:** WPF automatically handles DPI scaling when using font-based icons.

---

## Accessibility

**Color Blindness:**
- Idle (gray) vs. Recording (red) vs. Processing (blue) have sufficient luminance contrast
- Icons also differ in shape (not just color)

**High Contrast Mode:**
- Use system colors in high contrast mode: `SystemColors.ControlTextBrush`

**Screen Readers:**
- Tray icon tooltip provides textual state (read by screen readers)

---

## Testing Checklist

- [ ] Icons render correctly at 100%, 125%, 150%, 200% DPI
- [ ] Icons are visible in light theme
- [ ] Icons are visible in dark theme (Windows 11)
- [ ] Icons are distinguishable in high contrast mode
- [ ] Tooltip text is readable and localized correctly
- [ ] Animation (Processing state) is smooth and doesn't stutter

---

## Related Documents

- **Architecture:** `docs/architecture/architecture-overview.md`
- **Project Structure:** `docs/architecture/project-structure.md`
- **FR-015:** Custom flyout notification
- **US-002:** Tray icon shows status

---

## References

**Segoe MDL2 Assets Icon List:**
- https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font

**WPF Icon Font Best Practices:**
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/typography-in-wpf

---

**Last updated:** 2025-11-17
**Version:** v0.1 (Initial icon style guide for LocalWhisper)
