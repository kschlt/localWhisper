# Iteration 5 Research Findings & Decisions

**Date:** 2025-11-17
**Decision:** Split Iteration 5 into 5a (Wizard Core) and 5b (Download + Repair)

---

## Summary

Based on complexity analysis and the "simple but production-ready" philosophy, **Iteration 5 is split into two sub-iterations**:

- **Iteration 5a** (4-6h): Wizard core with file selection, SHA-1 validation, hotkey picker
- **Iteration 5b** (4-6h): HTTP download, progress tracking, repair flow

**Rationale:** Manual model download is acceptable for v0.1 (one-time setup). HTTP download is a UX enhancement, not a blocker. This split keeps each iteration manageable and reduces implementation risk.

---

## Research Questions & Answers

### Q1: Which models should we support?

**Answer:** base, small, medium, large-v3 (8 total with .en variants)

**Exclusions:**
- ❌ tiny/tiny.en - Not worth accuracy trade-off (minimal size savings)
- ❌ large-v1/v2 - Superseded by large-v3
- ❌ turbo - Not yet stable in whisper.cpp

**Justification:** Desktop has plenty of space. Even large-v3 (2.9 GB) is manageable. Offering all sizes gives users choice based on their hardware.

---

### Q2: Configurable URLs or hardcoded?

**Answer:** ✅ **Configurable via TOML**

**Format:**
```toml
[[whisper.available_models]]
name = "small"
download_url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"
sha1 = "55356645c2b361a969dfd0ef2c5a50d530afd8d5"
# ... other fields
```

**Justification:** URLs might change. Configuration-driven approach is future-proof and allows adding mirrors without code changes.

---

### Q3: What are the known-good hashes?

**Answer:** ✅ **SHA-1 hashes** (NOT SHA-256!)

**Discovery:** Whisper.cpp uses SHA-1 for model verification, not SHA-256.

**Complete Hash Table:**

| Model | SHA-1 Hash |
|-------|-----------|
| base | `465707469ff3a37a2b9b8d8f89f2f99de7299dac` |
| base.en | `137c40403d78fd54d454da0f9bd998f78703390c` |
| small | `55356645c2b361a969dfd0ef2c5a50d530afd8d5` |
| small.en | `db8a495a91d927739e50b3fc1cc4c6b8f6c2d022` |
| medium | `fd9727b6e1217c2f614f9b698455c4ffd82463b4` |
| medium.en | `8c30f0e44ce9560643ebd10bbe50cd20eafd3723` |
| large-v3 | `ad82bf6a9043ceed055076d0fd39f5f186ff8062` |

**Source:** https://github.com/ggml-org/whisper.cpp/blob/master/models/README.md

---

### Q4: Should we break down US-041 (Model Verification)?

**Answer:** ✅ **Yes - split into US-041a (file) + US-041b (download)**

**Complexity Analysis:**
- HTTP download requires: HttpClient, progress tracking, retry logic, cancellation
- File selection requires: OpenFileDialog, SHA-1 validation, file copy
- Total effort: 8-12h if combined

**Decision:**
- **US-041a** (Iteration 5a): File selection + SHA-1 validation
- **US-041b** (Iteration 5b): HTTP download + progress UI

---

### Q5: Multiple languages or just German?

**Answer:** ✅ **German + English** (2 languages for v0.1)

**Implementation:**
- Language ComboBox: "German / Deutsch", "English"
- German → shows: base, small, medium, large-v3
- English → shows: base.en, small.en, medium.en, large-v3

**Future:** More languages can be added via configuration without code changes.

---

### Q6: Show model tradeoffs (speed vs accuracy)?

**Answer:** ✅ **Yes - display in wizard**

**Data from OpenAI Research:**

| Model | Speed | VRAM | Description (German) |
|-------|-------|------|----------------------|
| base | 7x | ~1 GB | Schnell (142 MB) - Gut für Echtzeit |
| small | 4x | ~2 GB | **Empfohlen (466 MB) - Beste Balance ⭐** |
| medium | 2x | ~5 GB | Hohe Qualität (1.5 GB) - Langsamer |
| large-v3 | 1x | ~10 GB | Höchste Qualität (2.9 GB) - Am langsamsten |

**UI Presentation:** ListBox or DataGrid with Name, Size, Speed, Description columns. Highlight "small" as recommended.

---

### Q7: Repair trigger - when to check data root validity?

**Answer:** ✅ **On every app start**

**Validation Criteria:**
1. Data root directory exists
2. Required folders exist: config/, models/, history/, logs/, tmp/
3. config.toml exists and valid
4. Model file exists at configured path

**If invalid:** Show RepairDialog with options:
- "Neuen Ordner wählen" (re-link to moved folder)
- "Neu einrichten" (run wizard again)
- "Beenden" (exit app)

---

### Q8: Wizard midway close - what happens?

**Answer:** ✅ **Allow cancel, block app start until complete**

**Behavior:**
- User can cancel wizard at any step
- On cancel, confirm dialog: "Abbrechen? App kann ohne Konfiguration nicht gestartet werden."
- If confirmed cancel → app exits
- On next start → wizard shown again (no partial state saved)

**Rationale:** Simple, stateless. No complexity of resuming partial wizard state.

---

### Q9: Hotkey picker UX - how to capture input?

**Answer:** ✅ **Custom TextBox with PreviewKeyDown event**

**Implementation:**
```csharp
private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
{
    e.Handled = true; // Prevent default TextBox behavior

    var key = e.Key == Key.System ? e.SystemKey : e.Key;
    var modifiers = Keyboard.Modifiers;

    // Require at least one modifier
    if (modifiers == ModifierKeys.None)
    {
        StatusText.Text = "Bitte drücken Sie eine Tastenkombination (z.B. Ctrl+Shift+D)";
        return;
    }

    // Update display
    HotkeyTextBox.Text = FormatHotkey(modifiers, key);
}
```

**Why PreviewKeyDown?**
- Captures keys before TextBox processes them
- Allows disabling default shortcuts (Ctrl+C, Ctrl+V)
- No external dependencies

**Alternative Considered:** NHotkey library
- ❌ Adds dependency
- ❌ More complex than needed
- ✅ PreviewKeyDown is simpler and sufficient

---

### Q10: Hotkey conflict detection - how to detect?

**Answer:** ✅ **Attempt RegisterHotKey, check for error code 1409**

**Implementation:**
```csharp
private bool ValidateHotkey(ModifierKeys modifiers, Key key)
{
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

**Why this approach?**
- Windows provides NO API to query registered hotkeys
- Only way to detect conflict: attempt registration and check failure
- Error code 1409 = `ERROR_HOTKEY_ALREADY_REGISTERED`

**Source:** https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey

---

### Q11: Folder browser - which dialog to use?

**Answer:** ✅ **Ookii.Dialogs.Wpf** (over WinForms.FolderBrowserDialog)

**Comparison:**

| Feature | Ookii.Dialogs.Wpf | WinForms.FolderBrowserDialog |
|---------|-------------------|------------------------------|
| UI | Modern Vista-style | Ancient tree view |
| Copy/paste paths | ✅ Yes | ❌ No |
| WPF native | ✅ Yes | ❌ Requires WinForms reference |
| Dependency | Small NuGet package | Built-in .NET |
| License | BSD 3-Clause | N/A |

**Decision:** Ookii.Dialogs.Wpf

**Justification:**
- ✅ Significantly better UX (can paste paths)
- ✅ Pure WPF (no cross-framework reference)
- ✅ Permissive license
- ✅ Small overhead (~50 KB DLL)
- ✅ Worth the dependency for user experience

**Installation:**
```bash
dotnet add package Ookii.Dialogs.Wpf
```

**Usage:**
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

## Final Decisions Summary

| Decision Point | Choice | Rationale |
|----------------|--------|-----------|
| **Models** | base, small, medium, large-v3 | Good range, desktop has space |
| **URLs** | Configurable (TOML) | Future-proof, easy to update |
| **Hashes** | SHA-1 (not SHA-256) | Matches whisper.cpp standard |
| **US-041 Split** | Yes (5a + 5b) | Keep iterations manageable |
| **Languages** | German + English | Sufficient for v0.1 |
| **Model Tradeoffs** | Display in wizard | Helps users choose |
| **Repair Trigger** | Every app start | Catch issues early |
| **Wizard Cancel** | Allow, block app | Simple, stateless |
| **Hotkey Picker** | PreviewKeyDown on TextBox | Simple, no dependencies |
| **Conflict Detection** | Attempt RegisterHotKey | Only method available |
| **Folder Browser** | Ookii.Dialogs.Wpf | Better UX, worth dependency |

---

## Iteration 5a Scope (Confirmed)

✅ **In Scope:**
- Data root selection with Ookii folder browser
- Model file selection (user provides downloaded .bin file)
- SHA-1 hash validation
- Model tradeoff display (size, speed, description)
- Language selection (German / English)
- Hotkey picker with PreviewKeyDown
- Hotkey conflict detection
- Wizard navigation (Next, Back, Cancel, Finish)
- Folder structure creation (config/, models/, history/, logs/, tmp/)
- Initial config.toml generation

❌ **Out of Scope (Deferred to 5b):**
- HTTP model download
- Progress bar for download
- Download retry logic
- Repair flow for missing data root
- Wizard completion time metrics

---

## Estimated Effort

**Iteration 5a:** 4-6 hours
- Wizard UI (3 steps): 2h
- HotkeyTextBox control: 0.5h
- ModelValidator (SHA-1): 0.5h
- WizardManager (folder creation, config generation): 1h
- Integration + testing: 1h

**Iteration 5b:** 4-6 hours
- ModelDownloader (HTTP + retry): 2h
- DownloadProgressDialog: 1h
- DataRootValidator: 0.5h
- RepairDialog: 1h
- Integration + testing: 1h

**Total:** 8-12 hours (as originally estimated, now split)

---

## Documentation Updates

✅ **Completed:**
- `docs/iterations/iteration-05a-wizard-core.md` - Detailed planning
- `docs/iterations/iteration-05b-download-repair.md` - Deferred scope
- `docs/reference/whisper-models.md` - Complete model reference
- `docs/specification/user-stories-gherkin.md` - Updated US-041a, US-041b
- `docs/decisions/iteration-5-research-findings.md` - This document

---

## Next Steps

1. ✅ Documentation complete
2. ⏭️ Begin Iteration 5a implementation
3. 🔄 Follow learning loop: Implement → Review → Fix
4. 📊 Update traceability matrix after implementation

---

**Approved By:** User
**Date:** 2025-11-17
**Status:** Ready for implementation
