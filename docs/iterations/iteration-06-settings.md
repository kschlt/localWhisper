# Iteration 6: Settings UI

**Focus:** Post-setup configuration interface
**Effort:** 4-6 hours
**Status:** Ready for implementation
**Dependencies:** Iteration 5a (wizard), Iteration 5b (repair)

---

## Overview

Implement a Settings window accessible from the tray menu, allowing users to modify configuration after initial setup. All changes are persisted to `config.toml`.

**Key Deliverables:**
- Settings window UI (modal, single page)
- Tray menu integration (Settings, History, Exit)
- Hotkey reconfiguration with conflict detection
- Data root relocation (validation only, no auto-migration)
- Language and file format switching
- Model verification and replacement
- Save/Cancel with restart logic

---

## User Stories

| ID | Title | Priority | Scenarios |
|----|-------|----------|-----------|
| US-050 | Settings - Hotkey Change | Medium | 3 |
| US-051 | Settings - Data Root Change | Medium | 2 |
| US-052 | Settings - Language and Format | Low | 2 |
| US-053 | Settings - Model Check/Reload | Medium | 2 |
| US-054 | Settings - Access and Navigation | High | 3 |
| US-055 | Settings - Save and Cancel | High | 6 |
| US-056 | Settings - Validation and Error Handling | High | 3 |

**Total:** 7 user stories, 21 Gherkin scenarios

---

## Requirements Covered

- **FR-020:** Settings (Basis) - All settings available
- **FR-017:** Model validation (SHA-1 hash)
- **NFR-006:** Observability (log settings changes)

---

## Test-Driven Development (TDD) Workflow

### Phase 1: Write Tests FIRST (1-2h)

**Before any implementation, create these test files:**

#### 1. **SettingsWindowTests.cs**
Tests for window behavior, load/save, modal state.

**Test Scenarios:**
```csharp
// Window initialization
- OpenSettings_LoadsCurrentConfig_PopulatesFields()
- OpenSettings_NoChanges_SaveButtonDisabled()
- OpenSettings_WindowIsModal_BlocksAppInteraction()
- OpenSettings_ShowsVersionNumber_BottomLeft()

// Change detection
- ChangeAnyField_EnablesSaveButton()
- RevertChanges_DisablesSaveButton()
- ValidationError_DisablesSaveButton()
```

#### 2. **HotkeyChangeTests.cs**
Tests for hotkey modification and conflict detection.

**Test Scenarios:**
```csharp
- ChangeHotkey_Valid_UpdatesField()
- ChangeHotkey_Conflict_ShowsWarning()
- ChangeHotkey_NoModifier_ShowsError()
- SaveHotkeyChange_RequiresRestart()
```

#### 3. **DataRootChangeTests.cs**
Tests for data root validation and path updates.

**Test Scenarios:**
```csharp
- ChangeDataRoot_ValidPath_UpdatesField()
- ChangeDataRoot_InvalidStructure_ShowsError()
- ChangeDataRoot_NonExistent_ShowsError()
- ChangeDataRoot_ValidatesUsingDataRootValidator()
- SaveDataRootChange_RequiresRestart()
```

#### 4. **LanguageChangeTests.cs**
Tests for language switching.

**Test Scenarios:**
```csharp
- ChangeLanguage_GermanToEnglish_UpdatesConfig()
- SaveLanguageChange_RequiresRestart()
```

#### 5. **FileFormatChangeTests.cs**
Tests for file format switching.

**Test Scenarios:**
```csharp
- ChangeFileFormat_MarkdownToTxt_UpdatesConfig()
- SaveFileFormatChange_NoRestartRequired()
```

#### 6. **ModelVerificationTests.cs**
Tests for model SHA-1 verification and replacement.

**Test Scenarios:**
```csharp
- VerifyModel_ValidHash_ShowsSuccess()
- VerifyModel_InvalidHash_ShowsError()
- VerifyModel_ShowsProgressDialog()
- ChangeModel_ValidFile_UpdatesPath()
- ChangeModel_InvalidHash_ShowsError()
- SaveModelChange_NoRestartRequired()
```

#### 7. **SettingsPersistenceTests.cs**
Tests for saving to config.toml.

**Test Scenarios:**
```csharp
- Save_WritesToConfigToml()
- Save_MultipleChanges_AllPersisted()
- Save_WriteFailure_ShowsError()
- Cancel_NoChanges_ClosesImmediately()
- Cancel_WithChanges_ShowsConfirmation()
```

#### 8. **RestartLogicTests.cs**
Tests for restart dialog behavior.

**Test Scenarios:**
```csharp
- SaveHotkeyChange_ShowsRestartDialog()
- SaveLanguageChange_ShowsRestartDialog()
- SaveDataRootChange_ShowsRestartDialog()
- SaveFileFormatChange_NoRestartDialog()
- SaveMultipleChanges_OneRestartDialog()
- RestartDialog_Yes_RestartsApp()
- RestartDialog_No_SavesButNoRestart()
```

#### 9. **TrayMenuTests.cs**
Tests for tray menu integration.

**Test Scenarios:**
```csharp
- RightClickTray_ShowsMenu()
- ClickSettings_OpensSettingsWindow()
- ClickHistory_OpensExplorerToHistoryFolder()
- ClickExit_ClosesApp()
```

---

### Phase 2: Senior Dev Self-Review of Tests (0.5h)

**Switch to Senior Dev/Architect role and review tests:**

**Checklist:**
- ✅ Tests match Gherkin scenarios exactly
- ✅ Cover happy path AND edge cases
- ✅ Don't validate incorrect behavior
- ✅ Test isolation (no dependencies between tests)
- ✅ Performance considerations (no slow tests)
- ✅ Missing scenarios identified

**Document findings and fix tests before implementation.**

---

### Phase 3: Implementation (2-3h)

**Only AFTER tests are reviewed and fixed, implement components:**

#### Task 1: Create UI Components (1h)
1. **SettingsWindow.xaml**
   - Layout per `docs/ui/settings-window-specification.md`
   - Use Windows native colors from `docs/ui/color-palette.md`
   - Single scrollable page (500×600px)
   - Sections: Hotkey, Data Root, Language, File Format, Model
   - Save/Cancel buttons with proper styling

2. **SettingsWindow.xaml.cs**
   - Load current config on window open
   - Track changes (enable Save button)
   - Validation logic (inline errors)
   - Save to config.toml
   - Restart dialog logic

#### Task 2: Reuse Existing Components (0.5h)
- **HotkeyTextBox** (already implemented in wizard)
- **DataRootValidator** (already implemented in repair flow)
- **ModelValidator** (already implemented in wizard)
- **Folder browser** (Ookii.Dialogs.Wpf)

#### Task 3: Create Settings Manager (0.5h)
**SettingsManager.cs:**
```csharp
public class SettingsManager
{
    public void SaveSettings(AppConfig config, string configPath);
    public bool RequiresRestart(AppConfig oldConfig, AppConfig newConfig);
    public List<string> GetChangedSettings(AppConfig old, AppConfig new);
}
```

#### Task 4: Tray Menu Integration (0.5h)
**TrayIconManager.cs** (update existing):
- Add menu items: "Einstellungen", "History", "Beenden"
- Wire "Einstellungen" → Open SettingsWindow
- Wire "History" → Open Explorer to history folder
- Wire "Beenden" → Close app

#### Task 5: Restart Logic (0.5h)
**App.xaml.cs** (update):
- Detect restart requirement
- Show restart dialog
- Restart app if user confirms:
  ```csharp
  System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
  Application.Current.Shutdown();
  ```

---

### Phase 4: Senior Dev Self-Review of Code (0.5h)

**Switch to Senior Dev role again and review implementation:**

**Checklist:**
- ✅ Code quality and patterns
- ✅ Error handling comprehensive
- ✅ Security (path validation)
- ✅ Logging (all settings changes logged)
- ✅ Performance (no blocking UI)
- ✅ Matches specifications exactly
- ✅ No hardcoded strings (use resources)

**Document issues and fix before completion.**

---

### Phase 5: Fix Issues and Refactor (0.5h)

Fix all issues identified in code review.

---

## Implementation Checklist

**Use this as step-by-step guide during implementation:**

### Prerequisites
- [ ] Read `docs/ui/settings-window-specification.md`
- [ ] Read `docs/ui/color-palette.md`
- [ ] Read US-050 through US-056 in `user-stories-gherkin.md`
- [ ] Verify Iteration 5a/5b components are available (HotkeyTextBox, DataRootValidator, ModelValidator)

### Tests (Write FIRST!)
- [ ] Create `SettingsWindowTests.cs` (window behavior)
- [ ] Create `HotkeyChangeTests.cs` (hotkey modification)
- [ ] Create `DataRootChangeTests.cs` (path validation)
- [ ] Create `LanguageChangeTests.cs` (language switch)
- [ ] Create `FileFormatChangeTests.cs` (format switch)
- [ ] Create `ModelVerificationTests.cs` (SHA-1 validation)
- [ ] Create `SettingsPersistenceTests.cs` (config save)
- [ ] Create `RestartLogicTests.cs` (restart dialog)
- [ ] Create `TrayMenuTests.cs` (menu integration)

### Senior Dev Review of Tests
- [ ] Switch to Senior Dev role
- [ ] Review all tests against Gherkin scenarios
- [ ] Check edge cases covered
- [ ] Fix any issues found
- [ ] Switch back to Implementation role

### Implementation
- [ ] Create `AppColors.cs` (color constants from color-palette.md)
- [ ] Create `SettingsWindow.xaml` (UI layout per specification)
- [ ] Create `SettingsWindow.xaml.cs` (code-behind)
- [ ] Create `SettingsManager.cs` (save/restart logic)
- [ ] Update `TrayIconManager.cs` (add menu items)
- [ ] Update `App.xaml.cs` (restart logic)
- [ ] Add logging for all settings changes

### Integration
- [ ] Wire tray menu → Settings window
- [ ] Wire tray menu → History folder
- [ ] Wire Save button → ConfigManager.Save()
- [ ] Wire Save button → Restart dialog (if needed)
- [ ] Wire Cancel button → Confirmation dialog (if changes)

### Senior Dev Review of Code
- [ ] Switch to Senior Dev role
- [ ] Review implementation quality
- [ ] Check error handling
- [ ] Check security (path validation)
- [ ] Check logging completeness
- [ ] Fix issues found
- [ ] Switch back to Implementation role

### Testing
- [ ] Run all tests (should pass!)
- [ ] Manual test: Open Settings from tray
- [ ] Manual test: Change each setting type
- [ ] Manual test: Restart required flow
- [ ] Manual test: Restart not required flow
- [ ] Manual test: Cancel with/without changes
- [ ] Manual test: Validation errors display correctly

### Documentation
- [ ] Update traceability matrix
- [ ] Add changelog entry
- [ ] Commit with message referencing US-050..056

### Definition of Done
- [ ] All 21 Gherkin scenarios pass
- [ ] All unit tests pass (9 test classes)
- [ ] No regressions (previous iteration tests pass)
- [ ] Logging added for settings changes
- [ ] Traceability matrix updated
- [ ] Changelog entry added

---

## Components to Create

### New Files
```
src/LocalWhisper/
├── UI/
│   ├── Settings/
│   │   ├── SettingsWindow.xaml
│   │   ├── SettingsWindow.xaml.cs
│   │   └── SettingsManager.cs
│   └── Styles/
│       └── AppColors.cs
tests/LocalWhisper.Tests/
└── Unit/
    ├── SettingsWindowTests.cs
    ├── HotkeyChangeTests.cs
    ├── DataRootChangeTests.cs
    ├── LanguageChangeTests.cs
    ├── FileFormatChangeTests.cs
    ├── ModelVerificationTests.cs
    ├── SettingsPersistenceTests.cs
    ├── RestartLogicTests.cs
    └── TrayMenuTests.cs
```

### Modified Files
```
src/LocalWhisper/
├── App.xaml.cs (restart logic)
└── UI/TrayIcon/TrayIconManager.cs (menu items)
```

---

## Reusable Components (Already Implemented)

**From Iteration 5a:**
- `HotkeyTextBox.cs` - Hotkey capture control
- `ModelValidator.cs` - SHA-1 hash validation
- `ModelDefinition.cs` - Model metadata

**From Iteration 5b:**
- `DataRootValidator.cs` - Path structure validation

**From Core:**
- `ConfigManager.cs` - Load/save config.toml
- `AppLogger.cs` - Logging

---

## Settings That Require Restart

| Setting | Restart? | Reason |
|---------|----------|--------|
| Hotkey | ✅ Yes | Must unregister old, register new |
| Data Root | ✅ Yes | Must reinitialize paths |
| Language | ✅ Yes | Must reload UI strings |
| File Format | ❌ No | Applies to next dictation |
| Model Path | ❌ No | Adapter reloads on next use |

**Implementation:**
```csharp
private bool RequiresRestart(AppConfig old, AppConfig new)
{
    return old.Hotkey != new.Hotkey ||
           old.DataRoot != new.DataRoot ||
           old.Language != new.Language;
}
```

---

## Error Scenarios

| Error | Handling |
|-------|----------|
| Invalid data root path | Show inline red error, disable Save |
| Hotkey conflict | Show inline orange warning, allow Save |
| Model hash mismatch | Show red error, disable Save |
| Config save failure | Show error dialog, keep window open |
| Restart failure | Show error dialog, app remains running |

---

## Logging Requirements

**Log all settings changes:**
```csharp
AppLogger.LogInformation("Settings changed", new
{
    ChangedSettings = new[] { "Hotkey", "Language" },
    OldHotkey = "Ctrl+Shift+D",
    NewHotkey = "Ctrl+Alt+D",
    OldLanguage = "de",
    NewLanguage = "en"
});
```

**Log restart decisions:**
```csharp
AppLogger.LogInformation("User chose to restart now");
AppLogger.LogInformation("User deferred restart");
```

---

## Performance Considerations

- Model verification (SHA-1) can take 5-10 seconds for large files
  - Must show progress dialog (non-blocking)
- Data root validation should be instant
  - Use cached validator instance
- Config save should be instant
  - Use async file I/O if needed

---

## Accessibility

- All controls have `AutomationProperties.Name`
- Tab order is logical (top to bottom)
- Keyboard shortcuts (Enter = Save, Esc = Cancel)
- High contrast mode supported
- Screen reader compatible

---

## Related Documents

- **UI Spec:** `docs/ui/settings-window-specification.md`
- **Colors:** `docs/ui/color-palette.md`
- **User Stories:** `docs/specification/user-stories-gherkin.md` (US-050..056)
- **FR:** `docs/specification/functional-requirements.md` (FR-020)
- **Traceability:** `docs/specification/traceability-matrix.md`

---

## Estimated Effort Breakdown

| Phase | Effort | Description |
|-------|--------|-------------|
| Write tests | 1-2h | 9 test files, ~50 tests |
| Review tests | 0.5h | Senior dev self-review |
| UI implementation | 1h | XAML + code-behind |
| Settings manager | 0.5h | Save/restart logic |
| Tray integration | 0.5h | Menu items |
| Code review | 0.5h | Senior dev self-review |
| Fix issues | 0.5h | Refactor based on review |

**Total:** 4-6 hours

---

**Last updated:** 2025-11-18
**Status:** Ready for implementation
