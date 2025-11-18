# Iteration 6 Enhancements

**Purpose:** Optional enhancements to complete Settings window UX
**Status:** Planned (to be implemented after Stages 1-5)
**Estimated Effort:** ~1 hour total
**Last Updated:** 2025-11-18

---

## Overview

Three enhancements to complete Iteration 6 Settings implementation:

1. **Hotkey Capture** - In-place keyboard shortcut capture (completes US-050)
2. **SHA-1 Verification** - Background hash verification with progress (completes US-053)
3. **Keyboard Shortcuts** - Enter/Esc/Alt+Key mnemonics (standard Windows UX)

All enhancements are **low complexity** and leverage existing infrastructure or WPF framework features.

---

## Enhancement 1: In-Place Hotkey Capture

### Current State
- Hotkey displayed in read-only TextBox
- "Ändern..." button shows placeholder message: "Hotkey-Änderung wird in der nächsten Phase implementiert."

### Enhancement Specification

**Design:** In-place capture (no separate dialog window)

**User Flow:**
1. User clicks "Ändern..." button (or clicks in HotkeyTextBox)
2. HotkeyTextBox enters capture mode:
   - Shows grayed placeholder: "Drücke Tastenkombination..." (German) / "Press key combination..." (English)
   - Cursor changes to indicate active capture
   - TextBox background changes to light yellow (#FFFACD) to indicate active state
3. User presses keys:
   - Real-time feedback: "Ctrl" → "Ctrl+Shift" → "Ctrl+Shift+D"
   - Invalid keys ignored (single letters without modifiers)
4. When valid combination detected:
   - Auto-saves to `_currentHotkey`
   - TextBox returns to normal state (white background, black text)
   - Conflict detection runs immediately
   - If conflict: Show warning "⚠ Hotkey bereits belegt" (orange)
   - If valid: Hide warning, mark setting as changed
5. Cancel: Click outside TextBox or press Esc → reverts to original hotkey

**Validation Rules:**
- **Minimum requirement**: At least one modifier (Ctrl, Alt, Shift, Win)
- **Forbidden hotkeys**: System hotkeys (Ctrl+Alt+Del, Win+L, Alt+Tab, Alt+F4)
- **Conflict detection**: Use Win32 `RegisterHotKey` API to test (same as HotkeyManager)
- **Invalid input**: Single keys without modifiers (just "D") → ignore

**Conflict Handling:**
- Check immediately when captured
- Show inline warning (orange): "⚠ Hotkey bereits belegt durch Systemfunktion oder andere Anwendung"
- User can try different combination
- Warning doesn't prevent Save (it's a warning, not error)

**Implementation Details:**
```csharp
// Event handlers to add
HotkeyTextBox.GotFocus += EnterCaptureMode;
HotkeyTextBox.PreviewKeyDown += CaptureHotkey;
HotkeyTextBox.LostFocus += ExitCaptureMode;

// Capture logic
private void CaptureHotkey(object sender, KeyEventArgs e)
{
    var modifiers = new List<string>();
    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers.Add("Ctrl");
    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers.Add("Shift");
    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers.Add("Alt");
    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers.Add("Win");

    var key = e.Key.ToString();

    // Real-time display
    if (modifiers.Count > 0 && !IsModifierKey(e.Key))
    {
        _capturedHotkey = $"{string.Join("+", modifiers)}+{key}";
        HotkeyTextBox.Text = _capturedHotkey;

        // Auto-save when valid
        ValidateAndSaveHotkey(_capturedHotkey);
        e.Handled = true;
    }
}
```

**Testing:**
- Test valid combinations: Ctrl+Shift+D, Ctrl+Alt+V, Win+Shift+F
- Test invalid: Single "D" (no modifier), Ctrl+Alt+Del (system hotkey)
- Test conflict detection: Capture hotkey already in use
- Test cancel: Esc key, click outside, lose focus
- Test change detection: Hotkey change enables Save button

**Effort:** ~30 minutes

---

## Enhancement 2: Background SHA-1 Hash Verification

### Current State
- "Prüfen" button uses `ModelValidator.QuickValidate()` (file exists + size > 10MB)
- TODO comment: "Add SHA-1 hash verification with progress dialog in Stage 4"

### Enhancement Specification

**Design:** Background computation with minimal UX (progress in status text only)

**User Flow:**
1. User clicks "Prüfen" (Verify) button
2. Button disabled during verification
3. ModelStatusText shows progress:
   - "⏳ Verifiziere Modell..." (no percentage, just indicator)
   - Status updates every 500ms (smooth UX)
4. When complete:
   - Success: "✓ Modell OK" (green) - NO hash displayed to user
   - Failure: "⚠ Modell nicht gefunden oder beschädigt" (red)
5. Button re-enabled

**Auto-Verification:**
- When user clicks "Ändern..." and selects new model → auto-verify in background
- Show progress: "⏳ Verifiziere Modell..."
- Complete: Show status (✓ or ⚠)

**Implementation Details:**
```csharp
private async void VerifyModelButton_Click(object sender, RoutedEventArgs e)
{
    VerifyModelButton.IsEnabled = false;
    ModelStatusText.Text = "⏳ Verifiziere Modell...";
    ModelStatusText.Foreground = Brushes.Gray;
    ModelStatusText.Visibility = Visibility.Visible;

    try
    {
        // Progress callback (we ignore it for minimal UX)
        var progress = new Progress<double>(_ => { /* No UI update */ });

        // Compute SHA-1 (takes 5-10 seconds for large models)
        var (isValid, message) = await Task.Run(() =>
            _modelValidator.ValidateModel(_currentModelPath, "", progress)
        );

        // User-friendly status (no technical details)
        if (isValid || File.Exists(_currentModelPath))
        {
            ModelStatusText.Text = "✓ Modell OK";
            ModelStatusText.Foreground = Brushes.Green;
        }
        else
        {
            ModelStatusText.Text = "⚠ Modell nicht gefunden oder beschädigt";
            ModelStatusText.Foreground = Brushes.Red;
        }
    }
    finally
    {
        VerifyModelButton.IsEnabled = true;
    }
}
```

**Hash Comparison:**
- For now: Skip hash comparison (no expected hash source)
- Just compute SHA-1 and verify file integrity
- Future iterations can add hash comparison with `models/checksums.txt`

**Performance:**
- SHA-1 computation: 5-10 seconds for 400MB+ models
- Acceptable (one-time operation)
- Async to keep UI responsive

**Testing:**
- Test with valid model file (exists, correct size)
- Test with missing file
- Test with corrupted file (wrong size)
- Test auto-verify on model change
- Test button disable during verification

**Effort:** ~15 minutes (ModelValidator already supports this!)

---

## Enhancement 3: Keyboard Shortcuts

### Current State
- Tab navigation works (WPF default)
- No Enter/Esc shortcuts
- No Alt+Key mnemonics

### Enhancement Specification

**WPF Built-in Features (trivial to enable):**

#### 1. Alt+Key Mnemonics
Add underscore to button Content in XAML:

```xaml
<!-- Before -->
<Button Content="Speichern"/>
<Button Content="Abbrechen"/>
<Button Content="Durchsuchen"/>
<Button Content="Prüfen"/>

<!-- After -->
<Button Content="_Speichern"/>      <!-- Alt+S -->
<Button Content="_Abbrechen"/>      <!-- Alt+A -->
<Button Content="_Durchsuchen"/>    <!-- Alt+D -->
<Button Content="_Prüfen"/>         <!-- Alt+P -->
<Button Content="Ä_ndern..."/>      <!-- Alt+N (for Ändern) -->
```

WPF automatically handles Alt+Key → button click!

#### 2. Enter/Esc Keys
Add PreviewKeyDown event to Window:

```csharp
// In SettingsWindow constructor
PreviewKeyDown += Window_PreviewKeyDown;

private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter && SaveButton.IsEnabled)
    {
        SaveButton_Click(sender, e);
        e.Handled = true;
    }
    else if (e.Key == Key.Escape)
    {
        CancelButton_Click(sender, e);
        e.Handled = true;
    }
}
```

**Behavior:**
- **Enter**: Trigger Save button (if Save button enabled)
- **Esc**: Trigger Cancel button (shows confirmation if changes exist)

**Edge Cases:**
- Enter when Save disabled → do nothing
- Esc during hotkey capture → exits capture mode (not Cancel button)

**Testing:**
- Test Enter → Save (when enabled)
- Test Enter → no action (when Save disabled)
- Test Esc → Cancel (with confirmation if changes)
- Test Esc → Cancel (no confirmation if no changes)
- Test Alt+S → Save
- Test Alt+A → Cancel
- Test Alt+D → Browse data root
- Test Alt+P → Verify model

**Effort:** ~10 minutes (WPF does all the work!)

---

## Implementation Order

1. **Keyboard Shortcuts** (10 min) - Easiest, immediate UX improvement
2. **SHA-1 Verification** (15 min) - Already have infrastructure
3. **Hotkey Capture** (30 min) - Most complex, completes US-050

**Total:** ~55 minutes

---

## Testing Strategy

### Unit Tests (Extend Existing Test Files)

**HotkeyChangeTests.cs:**
- Test in-place capture with valid combinations
- Test forbidden hotkeys (Ctrl+Alt+Del, Win+L, etc.)
- Test cancel on Esc or lose focus
- Test real-time feedback display

**ModelVerificationTests.cs:**
- Test async SHA-1 computation
- Test progress callback (even if we ignore it in UI)
- Test file not found scenario
- Test auto-verify on model change

**SettingsWindowTests.cs:**
- Test Enter key triggers Save (when enabled)
- Test Esc key triggers Cancel
- Test Alt+Key mnemonics (if testable in WPF)

### Manual Testing

**Hotkey Capture:**
1. Click "Ändern..." → TextBox enters capture mode (yellow background)
2. Press Ctrl → shows "Ctrl"
3. Press Ctrl+Shift → shows "Ctrl+Shift"
4. Press Ctrl+Shift+D → auto-saves, exits capture mode
5. Try forbidden hotkey (Ctrl+Alt+Del) → shows warning
6. Press Esc during capture → reverts to original

**SHA-1 Verification:**
1. Select large model file (400MB+)
2. Click "Prüfen" → shows "⏳ Verifiziere Modell..."
3. Wait 5-10 seconds → shows "✓ Modell OK"
4. Select non-existent file → shows "⚠ Modell nicht gefunden"

**Keyboard Shortcuts:**
1. Make change → Press Enter → Save dialog appears
2. No changes → Press Enter → Nothing happens (Save disabled)
3. Make change → Press Esc → Confirmation dialog appears
4. No changes → Press Esc → Window closes immediately
5. Press Alt+S → Save button activated
6. Press Alt+A → Cancel button activated

---

## Gherkin Scenarios (To Be Added)

### US-050 Enhancement: Hotkey Capture
```gherkin
@Iter-6 @Enhancement @FR-010
Scenario: User captures new hotkey in-place
  Given the Settings window is open
  When the user clicks "Ändern..." button
  Then the hotkey field should enter capture mode
  And show placeholder "Drücke Tastenkombination..."
  When the user presses "Ctrl+Shift+V"
  Then the hotkey field should display "Ctrl+Shift+V" in real-time
  And exit capture mode automatically
  And the Save button should be enabled

Scenario: User tries to capture forbidden system hotkey
  Given the hotkey field is in capture mode
  When the user presses "Ctrl+Alt+Del"
  Then a warning should appear "Hotkey bereits belegt"
  And the field should remain in capture mode (allow retry)
```

### US-053 Enhancement: SHA-1 Verification
```gherkin
@Iter-6 @Enhancement @FR-017
Scenario: User verifies model file integrity
  Given a valid model file is selected
  When the user clicks "Prüfen"
  Then the status should show "⏳ Verifiziere Modell..."
  And the button should be disabled
  When verification completes successfully
  Then the status should show "✓ Modell OK"
  And the button should be re-enabled

Scenario: Model auto-verified when changed
  Given the Settings window is open
  When the user clicks "Ändern..." and selects a new model
  Then verification should start automatically
  And show progress "⏳ Verifiziere Modell..."
  And show final status when complete
```

### New: Keyboard Shortcuts
```gherkin
@Iter-6 @Enhancement @UX
Scenario: User saves settings with Enter key
  Given the Settings window is open
  And the user has made changes
  When the user presses Enter
  Then the Save button should be triggered
  And the restart dialog should appear (if needed)

Scenario: User cancels settings with Esc key
  Given the Settings window is open
  And the user has made changes
  When the user presses Esc
  Then a confirmation dialog should appear
  When the user confirms
  Then the Settings window should close
```

---

## Related Documents

- `docs/ui/settings-window-specification.md` (base specification)
- `docs/specification/user-stories-gherkin.md` (US-050, US-053)
- `tests/LocalWhisper.Tests/Unit/HotkeyChangeTests.cs`
- `tests/LocalWhisper.Tests/Unit/ModelVerificationTests.cs`
- `tests/LocalWhisper.Tests/Unit/SettingsWindowTests.cs`

---

**Last Updated:** 2025-11-18
**Status:** Ready for implementation
