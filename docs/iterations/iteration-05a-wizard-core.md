# Iteration 5a: Wizard Core (Simplified)

**Goal:** First-run wizard for initial setup (data root, model, hotkey)

**Status:** Planned (⚠️ Updated for Iteration 7 - See Note Below)
**Estimated Effort:** 4-6 hours (original) + 1.5h (Iteration 7 wizard step addition)
**Dependencies:** Iteration 1-4 complete

---

## ⚠️ IMPORTANT NOTE - Iteration 7 Wizard Extension

**Post-Processing wizard step added in Iteration 7:**

The wizard flow was extended to include post-processing setup. Original Iteration 5a implements Steps 1-3, but Step 3 is now numbered as Step 4.

**Updated Flow (after Iteration 7):**
```
Step 1: Data Root Selection (Iteration 5a)
Step 2: Whisper Model Selection (Iteration 5a)
Step 3: Post-Processing Setup (Iteration 7) ← NEW
        [ ] Enable Post-Processing (default: checked)
        Downloads: llama-cli.exe + Llama 3.2 3B model (~2GB)
Step 4: Hotkey Selection (Iteration 5a - renumbered from Step 3)
```

**See:** `docs/iterations/iteration-07-post-processing-DECISIONS.md` for Iteration 7 wizard step details.
**New User Story:** US-064 (Wizard - Post-Processing Setup)

---

## Overview

This iteration implements the **first-run wizard** to guide new users through initial setup. Users provide an existing Whisper model file (downloaded manually from HuggingFace or whisper.cpp). HTTP download functionality is deferred to Iteration 5b.

**Philosophy**: Keep it simple but production-ready. Manual model download is acceptable for v0.1 (one-time setup).

---

## User Stories

| ID | Title | Priority | Tags |
|----|-------|----------|------|
| US-040 | Wizard Step 1 - Data Root Selection | High | @FR-016 |
| US-041a | Wizard Step 2 - Model Verification (File Selection) | High | @FR-017 |
| US-042 | Wizard Step 3 - Hotkey Configuration | High | @FR-016 |
| US-045 | Wizard Logging | Medium | @NFR-006 |
| US-046 | Write-Protected Folder Error | Medium | @FR-021 |

**Total:** 5 user stories (US-041 split into 5a + 5b)

---

## Functional Requirements Covered

| FR ID | Title | Scope |
|-------|-------|-------|
| FR-016 | Configuration Management | ✅ Wizard creates valid config |
| FR-017 | Model Verification | ✅ SHA-1 hash validation |
| FR-021 | Error Handling | ✅ Write-protected folders, invalid models |
| FR-023 | Structured Logging | ✅ Wizard progress logging |

---

## Architecture Changes

### New Components

**1. UI/Wizard/WizardWindow.xaml + .cs**
- Multi-step wizard window (3 steps)
- Navigation: Next, Back, Cancel, Finish buttons
- Step indicators (1 → 2 → 3)
- Validation before advancing steps

**2. UI/Wizard/DataRootStep.xaml + .cs** (Step 1)
- TextBox showing data root path
- Browse button (Ookii.Dialogs.Wpf folder picker)
- Default: `%LOCALAPPDATA%\SpeechClipboardApp\`
- Validation: Check write permissions

**3. UI/Wizard/ModelSelectionStep.xaml + .cs** (Step 2)
- ComboBox for language selection (German / English)
- ComboBox for model size (base, small, medium, large)
- "Modell-Datei auswählen..." button (OpenFileDialog for .bin files)
- Progress indicator during SHA-1 computation
- Status label: "Modell OK ✓" or error message
- Model tradeoff display (size, speed, quality)

**4. UI/Wizard/HotkeyStep.xaml + .cs** (Step 3)
- Custom HotkeyTextBox control
- Default: Ctrl+Shift+D
- "Ändern..." button to capture new hotkey
- Validation: Requires at least one modifier
- Conflict detection via RegisterHotKey attempt

**5. UI/Controls/HotkeyTextBox.cs**
- Custom TextBox control
- Captures hotkey via PreviewKeyDown
- Displays formatted hotkey string (e.g., "Ctrl+Shift+D")
- Prevents default TextBox behavior (copy/paste)

**6. Services/ModelValidator.cs**
- Computes SHA-1 hash of model files
- Compares against known-good hashes
- Validates model file integrity
- Logs validation results

**7. Models/ModelDefinition.cs**
- Data model for available models
- Properties: Name, SizeMB, SHA1, DownloadURL, SpeedFactor, Description
- Configuration-driven (loaded from TOML)

**8. Core/WizardManager.cs**
- Orchestrates wizard flow
- Creates data root folder structure
- Copies model file to models/ directory
- Generates initial config.toml
- Validates all wizard inputs

### Modified Components

**App.xaml.cs:**
- Check for config existence on startup
- If no config → show wizard (block app initialization)
- If config exists but data root invalid → show repair dialog (defer to 5b)
- After wizard completion → initialize app normally

**AppConfig.cs:**
- Add AvailableModels list (loaded from config)
- Add IsConfigured flag

---

## Implementation Tasks

### Task 1: Model Configuration Schema

**File:** `docs/configuration-schema.md` (update)

Add `[[whisper.available_models]]` section:

```toml
[[whisper.available_models]]
name = "base"
filename = "ggml-base.bin"
size_mb = 142
sha1 = "465707469ff3a37a2b9b8d8f89f2f99de7299dac"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin"
speed_factor = 7.0
vram_gb = 1
description = "Schnell (142 MB) - Gut für Echtzeit"

[[whisper.available_models]]
name = "small"
filename = "ggml-small.bin"
size_mb = 466
sha1 = "55356645c2b361a969dfd0ef2c5a50d530afd8d5"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"
speed_factor = 4.0
vram_gb = 2
description = "Empfohlen (466 MB) - Beste Balance ⭐"

[[whisper.available_models]]
name = "medium"
filename = "ggml-medium.bin"
size_mb = 1536
sha1 = "fd9727b6e1217c2f614f9b698455c4ffd82463b4"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin"
speed_factor = 2.0
vram_gb = 5
description = "Hohe Qualität (1.5 GB) - Langsamer"

[[whisper.available_models]]
name = "large-v3"
filename = "ggml-large-v3.bin"
size_mb = 2960
sha1 = "ad82bf6a9043ceed055076d0fd39f5f186ff8062"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin"
speed_factor = 1.0
vram_gb = 10
description = "Höchste Qualität (2.9 GB) - Am langsamsten"
```

**Also add English-only variants**:
- base.en, small.en, medium.en (with respective SHA-1 hashes)

### Task 2: ModelDefinition + ModelValidator

**File:** `src/LocalWhisper/Models/ModelDefinition.cs`

```csharp
public class ModelDefinition
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int SizeMB { get; set; }
    public string SHA1 { get; set; } = string.Empty;
    public string DownloadURL { get; set; } = string.Empty;
    public double SpeedFactor { get; set; }
    public int VramGB { get; set; }
    public string Description { get; set; } = string.Empty;

    public bool IsEnglishOnly => Name.EndsWith(".en");
}
```

**File:** `src/LocalWhisper/Services/ModelValidator.cs`

```csharp
public class ModelValidator
{
    public async Task<bool> ValidateAsync(string filePath, string expectedSHA1)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await sha1.ComputeHashAsync(stream);
        var computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return computedHash.Equals(expectedSHA1, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Task 3: HotkeyTextBox Control

**File:** `src/LocalWhisper/UI/Controls/HotkeyTextBox.cs`

**Implementation**:
- Inherit from TextBox
- Override PreviewKeyDown:
  - Set `e.Handled = true`
  - Capture key + modifiers
  - Require at least one modifier (Ctrl/Shift/Alt/Win)
  - Format as "Ctrl+Shift+D"
  - Update Text property

**Properties**:
- `Hotkey` (ModifierKeys + Key tuple)
- `HotkeyString` (formatted display)

### Task 4: Wizard Window (3 Steps)

**File:** `src/LocalWhisper/UI/Wizard/WizardWindow.xaml`

**Layout**:
- Title bar: "LocalWhisper - Ersteinrichtung"
- Step indicator: ● ○ ○ (filled for current step)
- ContentControl for step content
- Button bar: [Abbrechen] [< Zurück] [Weiter >] [Fertig]

**Navigation Logic**:
- Step 1 → Step 2: Validate data root writability
- Step 2 → Step 3: Validate model file SHA-1
- Step 3 → Finish: Validate hotkey, attempt registration
- Cancel: Confirm exit (app cannot start without config)

### Task 5: Step 1 - Data Root Selection

**File:** `src/LocalWhisper/UI/Wizard/DataRootStep.xaml`

**UI Elements**:
- Label: "Wählen Sie den Speicherort für Ihre Daten:"
- TextBox: Read-only, displays path
- Browse button: Opens Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
- Default path: `Environment.GetFolderPath(SpecialFolder.LocalApplicationData) + "\SpeechClipboardApp"`

**Validation**:
```csharp
public bool ValidateDataRoot(string path)
{
    try
    {
        var testFile = Path.Combine(path, ".write_test");
        Directory.CreateDirectory(path);
        File.WriteAllText(testFile, "test");
        File.Delete(testFile);
        return true;
    }
    catch (UnauthorizedAccessException)
    {
        ShowError("Keine Schreibrechte für diesen Ordner.");
        return false;
    }
    catch (IOException ex)
    {
        ShowError($"Fehler beim Zugriff auf Ordner: {ex.Message}");
        return false;
    }
}
```

### Task 6: Step 2 - Model Selection

**File:** `src/LocalWhisper/UI/Wizard/ModelSelectionStep.xaml`

**UI Elements**:
- ComboBox: Language (German / English)
  - German → shows base, small, medium, large-v3
  - English → shows base.en, small.en, medium.en
- DataGrid or ListBox: Model selection with tradeoff display
  - Columns: Name, Size, Speed, Description
  - Highlight "small" as recommended
- Button: "Modell-Datei auswählen..."
- ProgressBar: Shown during SHA-1 computation
- TextBlock: Status ("Berechne Hash..." → "Modell OK ✓" or error)

**File Picker**:
```csharp
var dialog = new OpenFileDialog
{
    Filter = "Whisper Models (*.bin)|*.bin|All Files (*.*)|*.*",
    Title = "Whisper-Modell auswählen"
};

if (dialog.ShowDialog() == true)
{
    await ValidateAndCopyModel(dialog.FileName);
}
```

**Model Copy Logic**:
```csharp
private async Task ValidateAndCopyModel(string sourcePath)
{
    // 1. Get expected SHA-1 for selected model
    var selectedModel = GetSelectedModel();

    // 2. Compute SHA-1 (show progress)
    var isValid = await _modelValidator.ValidateAsync(sourcePath, selectedModel.SHA1);

    // 3. If valid, copy to data root
    if (isValid)
    {
        var destPath = Path.Combine(_dataRoot, "models", selectedModel.FileName);
        File.Copy(sourcePath, destPath, overwrite: true);
        StatusLabel.Text = "Modell OK ✓";
        StatusLabel.Foreground = Brushes.Green;
    }
    else
    {
        ShowError("Modell-Datei ist beschädigt oder ungültig.");
    }
}
```

### Task 7: Step 3 - Hotkey Configuration

**File:** `src/LocalWhisper/UI/Wizard/HotkeyStep.xaml`

**UI Elements**:
- Label: "Wählen Sie Ihre Diktiertaste:"
- HotkeyTextBox: Displays "Ctrl+Shift+D"
- Button: "Ändern..." → focuses HotkeyTextBox for input
- Label: "Halten Sie diese Tastenkombination während des Diktierens"

**Conflict Detection**:
```csharp
private bool ValidateHotkey(ModifierKeys modifiers, Key key)
{
    // Attempt registration
    var hwnd = new WindowInteropHelper(this).Handle;
    var registered = HotkeyManager.TryRegister(hwnd, 9999, modifiers, key);

    if (!registered)
    {
        var error = Marshal.GetLastWin32Error();
        if (error == 1409) // ERROR_HOTKEY_ALREADY_REGISTERED
        {
            ShowError("Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination.");
            return false;
        }
    }

    // Unregister test hotkey
    HotkeyManager.Unregister(hwnd, 9999);
    return true;
}
```

### Task 8: WizardManager - Folder Structure Creation

**File:** `src/LocalWhisper/Core/WizardManager.cs`

```csharp
public class WizardManager
{
    public void CreateDataRootStructure(string dataRoot)
    {
        var folders = new[] { "config", "models", "history", "logs", "tmp", "tmp/failed" };

        foreach (var folder in folders)
        {
            var path = Path.Combine(dataRoot, folder);
            Directory.CreateDirectory(path);
            AppLogger.LogInformation($"Created folder: {folder}");
        }
    }

    public void GenerateInitialConfig(string dataRoot, WizardResult result)
    {
        var config = new AppConfig
        {
            Hotkey = new HotkeyConfig
            {
                Modifiers = result.HotkeyModifiers,
                Key = result.HotkeyKey
            },
            Whisper = new WhisperConfig
            {
                CLIPath = "whisper-cli",
                ModelPath = Path.Combine(dataRoot, "models", result.ModelFileName),
                Language = result.SelectedLanguage,
                TimeoutSeconds = 60
            }
        };

        var configPath = PathHelpers.GetConfigPath(dataRoot);
        ConfigManager.Save(configPath, config);

        AppLogger.LogInformation("Initial config created", new { ConfigPath = configPath });
    }
}
```

### Task 9: App Startup Integration

**File:** `src/LocalWhisper/App.xaml.cs`

**Changes in OnStartup**:

```csharp
private void OnStartup(object sender, StartupEventArgs e)
{
    AppLogger.LogInformation("Application starting");

    // 1. Determine data root
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var defaultDataRoot = Path.Combine(appDataPath, "SpeechClipboardApp");

    // 2. Check if config exists
    var configPath = Path.Combine(defaultDataRoot, "config", "config.toml");

    if (!File.Exists(configPath))
    {
        AppLogger.LogInformation("No config found - launching wizard");

        // Show wizard (blocks until completion)
        var wizard = new WizardWindow();
        var result = wizard.ShowDialog();

        if (result != true)
        {
            AppLogger.LogInformation("Wizard cancelled - exiting application");
            Shutdown();
            return;
        }

        // Wizard completed - reload config
        _dataRoot = wizard.DataRoot;
        configPath = PathHelpers.GetConfigPath(_dataRoot);
    }
    else
    {
        _dataRoot = defaultDataRoot; // TODO(Iter-5b): Validate data root, show repair if missing
    }

    // 3. Continue with normal initialization
    _config = ConfigManager.Load(configPath);
    // ... rest of initialization ...
}
```

---

## External Dependencies

### NuGet Packages

**Ookii.Dialogs.Wpf** - Modern folder browser dialog
- Version: 5.0.1 (or latest)
- License: BSD 3-Clause
- Purpose: Vista-style folder picker (better UX than WinForms)

**Installation**:
```bash
dotnet add package Ookii.Dialogs.Wpf
```

**Usage**:
```csharp
using Ookii.Dialogs.Wpf;

var dialog = new VistaFolderBrowserDialog
{
    Description = "Wählen Sie den Datenordner:",
    UseDescriptionForTitle = true,
    SelectedPath = defaultPath
};

if (dialog.ShowDialog() == true)
{
    DataRootPath = dialog.SelectedPath;
}
```

---

## Research Findings

### Model SHA-1 Hashes (from whisper.cpp repository)

**Important**: Whisper.cpp uses **SHA-1**, not SHA-256!

| Model | Size | SHA-1 Hash | Source |
|-------|------|-----------|--------|
| base | 142 MiB | `465707469ff3a37a2b9b8d8f89f2f99de7299dac` | HuggingFace |
| base.en | 142 MiB | `137c40403d78fd54d454da0f9bd998f78703390c` | HuggingFace |
| small | 466 MiB | `55356645c2b361a969dfd0ef2c5a50d530afd8d5` | HuggingFace |
| small.en | 466 MiB | `db8a495a91d927739e50b3fc1cc4c6b8f6c2d022` | HuggingFace |
| medium | 1.5 GiB | `fd9727b6e1217c2f614f9b698455c4ffd82463b4` | HuggingFace |
| medium.en | 1.5 GiB | `8c30f0e44ce9560643ebd10bbe50cd20eafd3723` | HuggingFace |
| large-v3 | 2.9 GiB | `ad82bf6a9043ceed055076d0fd39f5f186ff8062` | HuggingFace |

**Download URLs**:
- Base URL: `https://huggingface.co/ggerganov/whisper.cpp/resolve/main/`
- Example: `https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin`

### Model Tradeoffs (from OpenAI research)

| Model | Parameters | Speed | VRAM | Recommendation |
|-------|-----------|-------|------|----------------|
| base | 74M | ~7x faster | ~1 GB | Quick dictation |
| small | 244M | ~4x faster | ~2 GB | **Best balance** (recommended) |
| medium | 769M | ~2x faster | ~5 GB | Higher quality, slower |
| large | 1550M | 1x (baseline) | ~10 GB | Highest quality, slowest |

**For UI Display**:
- base: "Schnell (142 MB) - Gut für Echtzeit"
- small: "Empfohlen (466 MB) - Beste Balance ⭐"
- medium: "Hohe Qualität (1.5 GB) - Langsamer"
- large-v3: "Höchste Qualität (2.9 GB) - Am langsamsten"

### Hotkey Implementation Decisions

**Picker Approach**: Custom TextBox with `PreviewKeyDown` event
- Simple to implement (no external dependencies)
- Captures keys before TextBox processes them
- Set `e.Handled = true` to prevent default behavior
- Requires at least one modifier (Ctrl/Shift/Alt/Win)

**Conflict Detection**: Attempt `RegisterHotKey`, check error code
- `RegisterHotKey` returns false if already registered
- Call `Marshal.GetLastWin32Error()` → returns `1409` (ERROR_HOTKEY_ALREADY_REGISTERED)
- No API to check beforehand - must attempt registration
- Show error: "Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination."

### Folder Browser Decision

**Choice**: Ookii.Dialogs.Wpf (over WinForms.FolderBrowserDialog)

**Advantages**:
- Modern Vista-style UI (vs. ancient tree view)
- Copy/paste paths directly
- No WinForms reference needed (pure WPF)
- BSD license (permissive)
- Active maintenance

**Trade-off**: Small external dependency (~50 KB DLL)

---

## Out of Scope (Deferred to 5b)

- HTTP model download with progress tracking
- Download resume/retry logic
- Progress bar UI
- Repair flow for missing data root
- Wizard completion time metrics (NFR-004)

---

## Testing Strategy

### Unit Tests

**ModelValidator**:
- Test SHA-1 computation with known file
- Test hash mismatch detection
- Test file not found exception

**HotkeyTextBox**:
- Test key capture (Ctrl+Shift+D)
- Test modifier requirement validation
- Test formatted string output

**WizardManager**:
- Test folder structure creation
- Test config generation
- Test validation logic

### Integration Tests

**Wizard Flow**:
- Complete wizard end-to-end
- Verify folder structure created
- Verify config.toml exists and valid
- Verify model copied to models/ directory

### Manual Tests

**Wizard UX**:
- Step navigation (Next, Back)
- Cancel confirmation
- Browse folder dialog
- Browse file dialog
- Hotkey picker capture
- Conflict detection (register Ctrl+Shift+D in another app, then try wizard)
- Write-protected folder error

---

## Definition of Done

- [ ] All 5 user stories (US-040, US-041a, US-042, US-045, US-046) implemented
- [ ] Wizard creates valid config.toml
- [ ] Folder structure created (config/, models/, history/, logs/, tmp/)
- [ ] Model SHA-1 validation works
- [ ] Model file copied to models/ directory
- [ ] Hotkey picker captures keys correctly
- [ ] Hotkey conflict detection works
- [ ] App starts normally after wizard completion
- [ ] App blocks startup without valid config
- [ ] Ookii.Dialogs.Wpf integrated
- [ ] Unit tests for ModelValidator, HotkeyTextBox
- [ ] Manual wizard flow tested
- [ ] Logging for wizard progress
- [ ] Traceability matrix updated
- [ ] Changelog entry added

---

## Known Limitations (Acceptable for v0.1)

- User must download model manually from HuggingFace (one-time setup)
- No download progress tracking (deferred to 5b)
- No repair flow for missing data root (deferred to 5b)
- Only German and English languages (extensible via config later)

---

**Iteration Start:** TBD
**Target Completion:** Wizard functional, app setup complete
