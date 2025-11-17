# Placeholders Tracker

**Purpose:** Track all placeholder implementations that need to be replaced in future iterations
**Status:** Living document (updated as placeholders are added/resolved)
**Last Updated:** 2025-11-17

---

## What is a Placeholder?

A **placeholder** is a temporary implementation that satisfies immediate requirements but is **intentionally incomplete**. Placeholders are used when:

1. A feature is needed in an early iteration but its full implementation comes later
2. A dependency exists on a future iteration's work
3. We want to test the full workflow without blocking on unfinished components

**Important:** All placeholders MUST be tracked here and resolved before v0.1 release.

---

## Placeholder Status

| ID | Iteration Added | Component | Description | Resolution Iteration | Status |
|----|----------------|-----------|-------------|---------------------|--------|
| PH-001 | 1 | Settings Window Link | Error dialog shows "Settings coming in v0.2" instead of opening Settings | 6 | ⏳ Pending |
| PH-002 | 1 | Processing State | Simulated 500ms delay instead of actual STT processing | 3 | ⏳ Pending |
| PH-003 | 1 | Config Schema | Minimal config (hotkey only) instead of full schema | 5 | ⏳ Pending |
| PH-004 | 1 | Data Root | Hardcoded `%LOCALAPPDATA%\LocalWhisper\` instead of user choice | 5 | ⏳ Pending |

**Legend:**
- ⏳ **Pending** - Placeholder is active, waiting for resolution iteration
- ✅ **Resolved** - Placeholder has been replaced with full implementation
- ⚠️ **Blocked** - Placeholder cannot be resolved due to external dependency

---

## Detailed Placeholder Descriptions

### PH-001: Settings Window Link

**Added in:** Iteration 1
**Component:** `UI/Dialogs/ErrorDialog.xaml` (hotkey conflict dialog)
**Current Implementation:**
```csharp
// TODO(PH-001, Iter-6): Replace with real SettingsWindow
private void OnOpenSettingsClicked(object sender, RoutedEventArgs e)
{
    MessageBox.Show(
        "Settings functionality coming in Iteration 6",
        "Not Yet Implemented",
        MessageBoxButton.OK,
        MessageBoxImage.Information
    );
}
```

**Expected Final Implementation (Iteration 6):**
```csharp
private void OnOpenSettingsClicked(object sender, RoutedEventArgs e)
{
    var settingsWindow = new SettingsWindow();
    settingsWindow.ShowDialog();
}
```

**User Story Link:** US-003 (Hotkey Conflict Error Dialog) → US-050 (Settings - Hotkey Change)

**Verification:**
- [ ] Settings window opens when "Einstellungen öffnen" is clicked
- [ ] Hotkey configuration field is focused on open
- [ ] User can successfully change hotkey from settings

---

### PH-002: Processing State Simulation

**Added in:** Iteration 1
**Component:** `Core/StateMachine.cs` or `Services/HotkeyManager.cs`
**Current Implementation:**
```csharp
// TODO(PH-002, Iter-3): Replace with actual STT processing
private async Task SimulateProcessing()
{
    logger.LogInformation("Simulated processing started (no audio/STT yet)");
    await Task.Delay(500); // Simulated processing time
    logger.LogInformation("Simulated processing complete");
    stateMachine.TransitionTo(AppState.Idle);
}
```

**Expected Final Implementation (Iteration 3):**
```csharp
private async Task ProcessAudio()
{
    logger.LogInformation("STT processing started");
    var wavFile = audioRecorder.GetOutputPath();
    var result = await whisperAdapter.TranscribeAsync(wavFile);
    logger.LogInformation("STT processing complete, TranscriptLength={Length}", result.Text.Length);

    // Clipboard/History writes (Iteration 4)
    stateMachine.TransitionTo(AppState.Idle);
}
```

**User Story Link:** US-001 (Hotkey Toggles State) → US-020 (STT via Whisper CLI)

**Verification:**
- [ ] Real STT processing occurs (Whisper CLI invoked)
- [ ] Logging shows actual STT duration instead of "simulated"
- [ ] State transitions only after real processing completes

---

### PH-003: Minimal Config Schema

**Added in:** Iteration 1
**Component:** `Core/ConfigManager.cs`, `config/config.toml`
**Current Implementation:**
```toml
# Minimal config for Iteration 1
[hotkey]
modifiers = ["Ctrl", "Shift"]
key = "D"
```

**Expected Final Implementation (Iteration 5):**
```toml
[app]
version = "0.1.0"
language = "de"

[hotkey]
modifiers = ["Ctrl", "Shift"]
key = "D"

[paths]
data_root = "C:\\Users\\...\\AppData\\Local\\LocalWhisper"
model_path = "${data_root}\\models\\ggml-small-de.gguf"
whisper_cli_path = "C:\\Tools\\whisper\\whisper-cli.exe"

[stt]
model_name = "small"
language = "de"
model_hash_sha256 = "a1b2c3d4..."

[history]
file_format = "md"
include_frontmatter = true

[postprocessing]
enabled = false

[logging]
level = "INFO"
max_file_size_mb = 10
```

**User Story Link:** US-001 (Hotkey) → US-040..042 (Wizard Steps)

**Verification:**
- [ ] ConfigManager can read/write full schema
- [ ] Wizard populates all config sections
- [ ] Validation rules are enforced (e.g., absolute paths, SHA-256 format)

---

### PH-004: Hardcoded Data Root

**Added in:** Iteration 1
**Component:** `Core/ConfigManager.cs`, application startup
**Current Implementation:**
```csharp
// TODO(PH-004, Iter-5): Replace with user-chosen path from wizard
private static string GetDataRoot()
{
    var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return Path.Combine(appData, "LocalWhisper");
}
```

**Expected Final Implementation (Iteration 5):**
```csharp
private static string GetDataRoot()
{
    var config = ConfigManager.Load();
    if (config?.Paths?.DataRoot != null && Directory.Exists(config.Paths.DataRoot))
    {
        return config.Paths.DataRoot;
    }

    // First run or missing data root → Launch wizard
    var wizard = new WizardWindow();
    if (wizard.ShowDialog() == true)
    {
        return wizard.SelectedDataRoot;
    }

    throw new InvalidOperationException("User cancelled wizard, cannot continue");
}
```

**User Story Link:** US-001 (App Initialization) → US-040 (Wizard Step 1 - Data Root Selection)

**Verification:**
- [ ] Wizard allows user to choose data root location
- [ ] Chosen path is validated (writable)
- [ ] Folder structure is created at chosen location
- [ ] Config stores user-chosen path

---

## Resolution Checklist Template

When resolving a placeholder, use this checklist:

```markdown
### Resolving PH-XXX: [Description]

**Date Resolved:** YYYY-MM-DD
**Iteration:** X
**Pull Request:** #XXX (if applicable)

**Changes Made:**
- [ ] Removed placeholder code
- [ ] Implemented full functionality
- [ ] Updated tests (placeholder tests → real tests)
- [ ] Updated documentation (if placeholder was documented)
- [ ] Verified acceptance criteria from linked user story
- [ ] Updated this tracker (status = ✅ Resolved)

**Verification:**
- [ ] All tests pass
- [ ] Manual testing confirms functionality
- [ ] No references to "TODO(PH-XXX)" remain in codebase
```

---

## Code Search for Placeholders

**Command to find all placeholders in code:**
```bash
grep -r "TODO(PH-" src/
```

**Expected output format:**
```
src/LocalWhisper/UI/Dialogs/ErrorDialog.xaml.cs:42: // TODO(PH-001, Iter-6): Replace with real SettingsWindow
src/LocalWhisper/Core/StateMachine.cs:78: // TODO(PH-002, Iter-3): Replace with actual STT processing
```

---

## Placeholder Naming Convention

**Format:** `PH-XXX` where XXX is a zero-padded 3-digit number

**In Code:**
```csharp
// TODO(PH-001, Iter-6): Brief description of what needs to be replaced
// See: docs/meta/placeholders-tracker.md
```

**In Commit Messages:**
```
feat(iter-1): US-003 Add hotkey conflict dialog

- Show error when hotkey registration fails
- Placeholder: Settings button (PH-001, resolved in Iter-6)
```

---

## Related Documents

- **Iteration Plan:** `docs/iterations/iteration-plan.md`
- **User Stories:** `docs/specification/user-stories-gherkin.md`
- **Traceability Matrix:** `docs/specification/traceability-matrix.md`

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| v0.1 | 2025-11-17 | Initial tracker created with 4 placeholders from Iteration 1 planning |

---

**Last updated:** 2025-11-17
**Tracked Placeholders:** 4 active, 0 resolved
