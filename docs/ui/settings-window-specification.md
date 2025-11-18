# Settings Window Specification

**Purpose:** Detailed UI/UX specification for Settings window (Iteration 6)
**Status:** Stable (ready for implementation)
**Last Updated:** 2025-11-18

---

## Overview

The Settings window allows users to modify application configuration after initial setup. All changes are saved to `config.toml` when the user clicks "Speichern" (Save).

**Access:** Tray icon → Right-click → "Einstellungen" (German) / "Settings" (English)

**Related User Stories:**
- US-050: Settings - Hotkey Change
- US-051: Settings - Data Root Change
- US-052: Settings - Language and Format
- US-053: Settings - Model Check/Reload

**Related Requirements:**
- FR-020: Settings (Basis)

---

## Window Properties

### Basic Properties
- **Title:** "LocalWhisper - Einstellungen" / "LocalWhisper - Settings"
- **Size:** 500px (width) × 600px (height)
- **Position:** Center of screen (always, no position memory)
- **Resizable:** No
- **Modal:** Yes (blocks app interaction until closed)
- **Icon:** Same as main app icon

### Layout
- Single scrollable page (no tabs)
- Grouped sections with visual separation
- Save/Cancel buttons at bottom
- Version number at bottom-left

---

## UI Mockup (ASCII)

```
┌─────────────────────────────────────────────────────────┐
│ LocalWhisper - Einstellungen                        [X] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Hotkey                                                 │
│  ┌─────────────────────────────────┐  ┌─────────────┐  │
│  │ Ctrl+Shift+D                    │  │  Ändern...  │  │
│  └─────────────────────────────────┘  └─────────────┘  │
│  ⚠ Warnung: Hotkey bereits belegt   (if conflict)     │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│  Datenordner / Data Root                                │
│  ┌─────────────────────────────────┐  ┌─────────────┐  │
│  │ C:\Users\...\AppData\Local\...  │  │ Durchsuchen │  │
│  └─────────────────────────────────┘  └─────────────┘  │
│  ⚠ Pfad nicht gefunden - bitte korrekten Pfad wählen   │
│    (shown in red if invalid)                            │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│  Sprache / Language                                     │
│  ○ Deutsch    ○ English                                 │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│  Dateiformat / File Format                              │
│  ○ Markdown (.md)    ○ Plain Text (.txt)               │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│  Whisper Modell                                         │
│  Pfad: C:\...\LocalWhisper\models\ggml-small.bin       │
│  ┌─────────────┐  ┌─────────────┐                      │
│  │  Prüfen     │  │   Ändern... │                      │
│  └─────────────┘  └─────────────┘                      │
│  ✓ Modell OK     (or ⚠ Modell ungültig)                │
│                                                         │
│                                                         │
│                                                         │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│ v0.1.0                    ┌──────────┐  ┌──────────┐   │
│                           │ Speichern│  │ Abbrechen│   │
│                           └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────┘
```

---

## Section Details

### 1. Hotkey Section

**Controls:**
- **Label:** "Hotkey" (bold, 14pt)
- **TextBox:** Read-only, displays current hotkey (e.g., "Ctrl+Shift+D")
  - Uses `HotkeyTextBox` control (reused from wizard)
  - Width: 300px
  - Height: 30px
- **Button:** "Ändern..." (Change)
  - Width: 100px
  - Background: `AccentBlue` (#0078D4)
  - Foreground: White
  - Triggers hotkey capture mode

**Behavior:**
1. User clicks "Ändern..." button
2. TextBox becomes active (shows "Click and press key combination...")
3. User presses new hotkey (e.g., Ctrl+Alt+D)
4. Conflict detection runs immediately (same as wizard)
5. If conflict: Show warning "⚠ Hotkey bereits belegt" in orange (#FFB900)
6. If valid: Update TextBox, hide warning

**Validation:**
- At least one modifier required (Ctrl/Alt/Shift/Win)
- Conflict detection via `RegisterHotKey` Win32 API
- Invalid hotkey → Save button disabled

---

### 2. Data Root Section

**Controls:**
- **Label:** "Datenordner / Data Root" (bold, 14pt)
- **TextBox:** Read-only, displays current path
  - Width: 350px
  - Height: 30px
  - Truncates long paths with "..." in middle
- **Button:** "Durchsuchen" (Browse)
  - Width: 100px
  - Background: `AccentBlue`
  - Opens `VistaFolderBrowserDialog`

**Behavior:**
1. User clicks "Durchsuchen" button
2. Folder browser opens (Ookii.Dialogs.Wpf)
3. User selects new folder
4. **Validation runs immediately:**
   - Check folder exists
   - Check has valid structure (config/, models/ subdirectories)
   - Use `DataRootValidator.Validate()` (same as repair flow)
5. If valid: Update TextBox, hide error
6. If invalid: Show error "⚠ Pfad nicht gefunden - bitte korrekten Pfad wählen" in red (#E81123)

**Validation:**
- Folder must exist
- Folder must contain valid LocalWhisper structure
- Invalid path → Save button disabled

**Error Display:**
- Red border on TextBox (2px, `ValidationError` color)
- Red warning text below field
- Save button disabled

---

### 3. Language Section

**Controls:**
- **Label:** "Sprache / Language" (bold, 14pt)
- **RadioButton Group:** Horizontal layout
  - ○ Deutsch
  - ○ English
  - Spacing: 20px between buttons

**Behavior:**
- Select radio button → marks setting as "changed"
- Language change requires restart (indicated after Save)

---

### 4. File Format Section

**Controls:**
- **Label:** "Dateiformat / File Format" (bold, 14pt)
- **RadioButton Group:** Horizontal layout
  - ○ Markdown (.md)
  - ○ Plain Text (.txt)
  - Spacing: 20px between buttons

**Behavior:**
- Select radio button → marks setting as "changed"
- File format change does NOT require restart (applies to next dictation)

---

### 5. Whisper Model Section

**Controls:**
- **Label:** "Whisper Modell" (bold, 14pt)
- **TextBlock:** Read-only path display
  - "Pfad: C:\...\models\ggml-small.bin"
  - Foreground: `TextSecondary` (#666666)
  - Truncates long paths
- **Button:** "Prüfen" (Verify)
  - Width: 100px
  - Background: `AccentBlue`
  - Triggers SHA-1 hash verification
- **Button:** "Ändern..." (Change)
  - Width: 100px
  - Background: `AccentBlue`
  - Opens file picker
- **TextBlock:** Status indicator
  - "✓ Modell OK" (green #107C10) if valid
  - "⚠ Modell ungültig" (red #E81123) if invalid

**Behavior - Verify:**
1. User clicks "Prüfen" button
2. Show progress dialog (reuse `DownloadProgressDialog` UI without download)
   - "Verifiziere Modell..." (Verifying model...)
   - Progress bar (SHA-1 computation)
3. Compute SHA-1 hash (use `ModelValidator`)
4. Compare against expected hash from `ModelDefinition`
5. Show result:
   - Success: "✓ Modell OK" (green)
   - Failure: "⚠ Modell ungültig. Bitte neu herunterladen." (red)

**Behavior - Change:**
1. User clicks "Ändern..." button
2. Open `OpenFileDialog`:
   - Filter: "Whisper Model Files (*.bin)|*.bin|All Files (*.*)|*.*"
   - InitialDirectory: Current models folder
3. User selects file
4. **Validation runs immediately:**
   - File exists
   - SHA-1 hash verification (show progress)
5. If valid: Update path, show "✓ Modell OK"
6. If invalid: Show error, revert to old path

**Validation:**
- File must exist
- SHA-1 hash must match expected value
- Invalid model → Save button disabled

---

## Save/Cancel Buttons

### Save Button

**Properties:**
- Text: "Speichern" (German) / "Save" (English)
- Width: 100px
- Height: 35px
- Background: `AccentBlue` (#0078D4)
- Foreground: White
- Position: Bottom-right (10px margin)

**Behavior:**
1. **Disabled initially** (enabled only when changes detected)
2. **Validation before save:**
   - All fields must be valid
   - If any validation error → button remains disabled
3. **On click:**
   - Save all changes to `config.toml`
   - Detect which settings changed
   - If restart required (hotkey, language, data root):
     - Show restart dialog (see below)
   - If no restart required:
     - Close Settings window
     - Show success notification (optional)

**Restart Dialog:**
```
┌─────────────────────────────────────────┐
│ Neustart erforderlich                   │
├─────────────────────────────────────────┤
│ Einige Änderungen erfordern einen       │
│ Neustart.                               │
│                                         │
│ Jetzt neu starten?                      │
│                                         │
│         ┌────────┐    ┌────────┐       │
│         │   Ja   │    │  Nein  │       │
│         └────────┘    └────────┘       │
└─────────────────────────────────────────┘
```

**Restart Logic:**
- **Ja (Yes):** Close Settings → Restart app
- **Nein (No):** Close Settings → Changes saved, old settings active until manual restart

---

### Cancel Button

**Properties:**
- Text: "Abbrechen" (German) / "Cancel" (English)
- Width: 100px
- Height: 35px
- Background: `ErrorRed` (#E81123)
- Foreground: White
- Position: Bottom-right, left of Save button (10px spacing)

**Behavior:**
1. **If no changes made:** Close window immediately
2. **If changes detected:**
   - Show confirmation dialog:
```
┌─────────────────────────────────────────┐
│ Änderungen verwerfen?                   │
├─────────────────────────────────────────┤
│ Möchten Sie die Änderungen wirklich     │
│ verwerfen?                              │
│                                         │
│         ┌────────┐    ┌────────┐       │
│         │   Ja   │    │  Nein  │       │
│         └────────┘    └────────┘       │
└─────────────────────────────────────────┘
```
   - **Ja (Yes):** Discard changes, close window
   - **Nein (No):** Return to Settings window

---

## Version Number

**Display:**
- Text: "v0.1.0" (or current version)
- Position: Bottom-left corner (10px margin)
- Foreground: `TextSecondary` (#666666)
- Font: 10pt, regular weight

---

## Change Detection

**Mechanism:**
- Track initial values when window opens
- Compare current values to initial on every field change
- Enable Save button if ANY field differs from initial
- Disable Save button if ALL fields match initial OR validation errors exist

**Implementation:**
```csharp
private bool HasChanges()
{
    return _currentHotkey != _initialHotkey ||
           _currentDataRoot != _initialDataRoot ||
           _currentLanguage != _initialLanguage ||
           _currentFileFormat != _initialFileFormat ||
           _currentModelPath != _initialModelPath;
}

private void UpdateSaveButtonState()
{
    SaveButton.IsEnabled = HasChanges() && IsAllValid();
}
```

---

## Validation States

### Field-Level Validation

**Each field can be in one of three states:**

1. **Valid (Default)**
   - No visual indicator
   - Field enabled
   - Contributes to "all valid" check

2. **Invalid (Error)**
   - Red border (2px, `ValidationError` #E81123)
   - Red warning text below field
   - Save button disabled
   - Example: "⚠ Pfad nicht gefunden"

3. **Warning**
   - Orange border (2px, `WarningOrange` #FFB900)
   - Orange warning text below field
   - Save button enabled (warning, not error)
   - Example: "⚠ Hotkey bereits belegt"

---

## Keyboard Navigation

**Tab Order:**
1. Hotkey TextBox / "Ändern..." button
2. Data Root TextBox / "Durchsuchen" button
3. Language RadioButton group
4. File Format RadioButton group
5. Model "Prüfen" button
6. Model "Ändern..." button
7. "Speichern" button
8. "Abbrechen" button

**Shortcuts:**
- **Enter:** Trigger Save (if enabled)
- **Escape:** Trigger Cancel (with confirmation if changes)
- **Alt+S:** Focus Save button
- **Alt+A:** Focus Cancel button

---

## Settings That Require Restart

| Setting | Requires Restart? | Reason |
|---------|-------------------|--------|
| Hotkey | ✅ Yes | Must unregister old hotkey, register new |
| Data Root | ✅ Yes | Must reinitialize paths, reload config |
| Language | ✅ Yes | Must reload UI strings |
| File Format | ❌ No | Applies to next dictation only |
| Model Path | ❌ No | Adapter can reload model on next transcription |

**Restart Prompt Logic:**
```csharp
private bool RequiresRestart()
{
    return _hotkeyChanged || _dataRootChanged || _languageChanged;
}
```

---

## Error Handling

### Validation Errors
- **Display:** Inline below field, red text
- **Logging:** All validation errors logged to `logs/app.log`
- **Save Button:** Disabled until all errors resolved

### Save Errors
- **Scenario:** `config.toml` write fails (permissions, disk full)
- **Dialog:**
```
┌─────────────────────────────────────────┐
│ Fehler beim Speichern                   │
├─────────────────────────────────────────┤
│ Einstellungen konnten nicht gespeichert │
│ werden:                                 │
│                                         │
│ [Error message]                         │
│                                         │
│             ┌────────┐                  │
│             │   OK   │                  │
│             └────────┘                  │
└─────────────────────────────────────────┘
```
- **Logging:** Error logged with stack trace
- **Window:** Stays open, user can retry or cancel

---

## Accessibility

- All controls have `AutomationProperties.Name` set
- Tab order is logical
- Keyboard shortcuts available
- High contrast mode supported (uses system colors)
- Screen reader compatible

---

## Related Documents

- `docs/iterations/iteration-06-settings.md` - Implementation plan
- `docs/specification/user-stories-gherkin.md` - US-050, US-051, US-052, US-053
- `docs/specification/functional-requirements.md` - FR-020
- `docs/ui/color-palette.md` - Color definitions

---

**Last updated:** 2025-11-18
