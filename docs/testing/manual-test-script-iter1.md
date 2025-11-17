# Manual Test Script - Iteration 1

**Purpose:** Step-by-step manual testing instructions for Iteration 1 features
**Audience:** QA testers, product owners
**Iteration:** 1 (Hotkey & App Skeleton)
**Last Updated:** 2025-11-17

---

## Prerequisites

### Test Environment
- **OS:** Windows 10 (version 1903+) or Windows 11
- **User Account:** Standard user (no admin rights required)
- **Display:** Any resolution, test at 100% and 150% DPI if possible
- **Keyboard:** Standard US or German keyboard layout

### Test Build
- **Build Type:** Release (self-contained)
- **File:** `LocalWhisper.exe` (portable, single file)
- **Size:** ~150-200 MB
- **Location:** Can be placed anywhere (e.g., `Downloads`, `Desktop`, `C:\Tools\`)

### Before Testing
1. ✅ Close any applications that might use `Ctrl+Shift+D` hotkey
2. ✅ Ensure no previous installation of LocalWhisper exists
3. ✅ Have Notepad or similar text editor ready for clipboard testing (future iterations)
4. ✅ Have Task Manager open to verify process behavior

---

## Test Suite Overview

| Test ID | Feature | Priority | Expected Duration |
|---------|---------|----------|-------------------|
| T1-001 | First Launch & Tray Icon | High | 2 min |
| T1-002 | Hotkey Registration (Default) | High | 3 min |
| T1-003 | Hotkey State Transitions | High | 5 min |
| T1-004 | Tray Icon State Indicators | High | 3 min |
| T1-005 | Hotkey Conflict Detection | Medium | 5 min |
| T1-006 | Error Dialog Appearance | Medium | 2 min |
| T1-007 | Logging Functionality | Low | 3 min |
| T1-008 | App Shutdown & Restart | High | 2 min |

**Total Duration:** ~25 minutes

---

## Test Cases

### T1-001: First Launch & Tray Icon

**User Story:** US-002 (Tray Icon Shows Status)

**Objective:** Verify app launches successfully and tray icon appears.

**Steps:**
1. Double-click `LocalWhisper.exe`
2. Observe system tray (bottom-right corner of taskbar)
3. Locate the LocalWhisper icon

**Expected Results:**
- ✅ App launches without errors (no crash, no UAC prompt)
- ✅ Tray icon appears within 3 seconds
- ✅ Icon is **gray circle** (Idle state)
- ✅ Hover tooltip shows: `"LocalWhisper: Bereit"` (German) or `"LocalWhisper: Ready"` (English)
- ✅ No main window appears (app is tray-only)

**Pass/Fail:** ___________

**Notes:**
_____________________________________________

---

### T1-002: Hotkey Registration (Default)

**User Story:** US-001 (Hotkey Toggles State)

**Objective:** Verify default hotkey `Ctrl+Shift+D` is registered successfully.

**Steps:**
1. Ensure app is running (tray icon visible)
2. Open Task Manager → Details tab
3. Verify `LocalWhisper.exe` process is running
4. Check tray icon tooltip

**Expected Results:**
- ✅ App is running in background
- ✅ No error dialogs appear on startup
- ✅ Tray icon tooltip indicates "Ready" state

**Pass/Fail:** ___________

**Notes:**
_____________________________________________

---

### T1-003: Hotkey State Transitions

**User Story:** US-001 (Hotkey Toggles State)

**Objective:** Verify hotkey press/release triggers state transitions.

**Steps:**
1. **Press and hold** `Ctrl+Shift+D`
2. Observe tray icon (should change immediately)
3. **Hold for 2 seconds** while watching icon
4. **Release** `Ctrl+Shift+D`
5. Observe tray icon (should change again)
6. Wait 1 second
7. Observe tray icon (should return to Idle)

**Expected Results:**

| Action | Expected Tray Icon | Expected Tooltip |
|--------|-------------------|------------------|
| Initial state | Gray circle ○ | "LocalWhisper: Bereit" |
| Hotkey DOWN (held) | Red solid circle ● | "LocalWhisper: Aufnahme..." |
| Hotkey UP (released) | Blue spinner ⟳ (rotating) | "LocalWhisper: Verarbeitung..." |
| After ~500ms | Gray circle ○ | "LocalWhisper: Bereit" |

**Animation Check:**
- ✅ Recording icon (red) may pulse slightly (optional)
- ✅ Processing icon (blue) rotates smoothly (~1.5s per revolution)

**Pass/Fail:** ___________

**Notes:**
_____________________________________________

---

### T1-004: Tray Icon State Indicators

**User Story:** US-002 (Tray Icon Shows Status)

**Objective:** Verify tray icon updates within 100ms of state changes.

**Steps:**
1. Press `Ctrl+Shift+D` (hold)
2. **Immediately** observe tray icon (should turn red within 100ms)
3. Release `Ctrl+Shift+D`
4. **Immediately** observe tray icon (should turn blue within 100ms)

**Expected Results:**
- ✅ Icon changes to **red** within 100ms of pressing hotkey
- ✅ Icon changes to **blue** within 100ms of releasing hotkey
- ✅ No lag or delay in visual feedback

**Pass/Fail:** ___________

**Notes:**
_____________________________________________

---

### T1-005: Hotkey Conflict Detection

**User Story:** US-003 (Hotkey Conflict Error Dialog)

**Objective:** Verify app detects and reports hotkey conflicts.

**Prerequisite Setup:**
1. Close LocalWhisper (right-click tray icon → "Beenden")
2. Install **AutoHotkey** or use PowerShell to register `Ctrl+Shift+D` globally:
   ```powershell
   # PowerShell script (run as admin):
   Add-Type @"
       using System;
       using System.Runtime.InteropServices;
       public class Hotkey {
           [DllImport("user32.dll")]
           public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
       }
   "@
   [Hotkey]::RegisterHotKey([IntPtr]::Zero, 1, 0x0006, 0x44) # Ctrl+Shift+D
   Read-Host "Press Enter to unregister"
   ```
   **Note:** If PowerShell is too complex, manually simulate by opening another instance of LocalWhisper first.

**Steps:**
1. With conflicting hotkey registered, launch `LocalWhisper.exe`
2. Observe startup behavior

**Expected Results:**
- ✅ App launches (does not crash)
- ✅ Error dialog appears with:
  - **Title:** `"Hotkey nicht verfügbar"`
  - **Message:** `"Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination in den Einstellungen."`
  - **Icon:** Warning icon (yellow triangle)
  - **Buttons:** `"Einstellungen öffnen"` | `"OK"`
- ✅ App remains running in tray (does not exit)
- ✅ Tray icon is gray (Idle state)

**Step 2a: Test Settings Placeholder**
1. Click `"Einstellungen öffnen"` button

**Expected Results:**
- ✅ Placeholder dialog appears: `"Settings functionality coming in Iteration 6"`
- ✅ Clicking OK closes placeholder dialog

**Cleanup:**
1. Unregister conflicting hotkey (close AutoHotkey or PowerShell script)

**Pass/Fail:** ___________

**Notes:**
_____________________________________________

---

### T1-006: Error Dialog Appearance

**User Story:** US-003 (Hotkey Conflict Error Dialog), FR-021 (Error Dialogs)

**Objective:** Verify error dialogs are user-friendly and properly formatted.

**Steps:**
1. (Use same setup as T1-005: trigger hotkey conflict)
2. Inspect error dialog appearance

**Expected Results:**
- ✅ Dialog is **centered on screen** (or above tray icon)
- ✅ Dialog has **window title bar** with app name
- ✅ Dialog is **modal** (blocks interaction with other windows until closed)
- ✅ Text is **readable** (sufficient contrast, no truncation)
- ✅ Buttons are **clearly labeled** in German (or English if app language is EN)
- ✅ **No technical jargon** (no stack traces, no "Exception", no debug info)

**Pass/Fail:** ___________

**Notes:**
_____________________________________________

---

### T1-007: Logging Functionality

**User Story:** FR-023 (Logging), NFR-006 (Observability)

**Objective:** Verify app creates log file with structured entries.

**Steps:**
1. Launch LocalWhisper
2. Perform hotkey press/release cycle 3 times
3. Right-click tray icon → "Beenden" (Exit)
4. Navigate to `%LOCALAPPDATA%\LocalWhisper\logs\`
5. Open `app.log` in Notepad

**Expected Results:**
- ✅ File `app.log` exists
- ✅ Log file is **UTF-8 plain text** (readable)
- ✅ Log entries include:
  - `[YYYY-MM-DD HH:MM:SS.fff] [INFO] [App] Application started {Version=0.1.0, ...}`
  - `[...] [INFO] [HotkeyManager] Hotkey registered {Modifiers=Ctrl+Shift, Key=D}`
  - `[...] [INFO] [StateMachine] State transition {From=Idle, To=Recording}`
  - `[...] [INFO] [StateMachine] State transition {From=Recording, To=Processing}`
  - `[...] [INFO] [StateMachine] Simulated processing complete`
  - `[...] [INFO] [StateMachine] State transition {From=Processing, To=Idle}`
  - `[...] [INFO] [App] Application stopped`

**Verification:**
- ✅ At least 3 `Idle → Recording` transitions logged
- ✅ At least 3 `Recording → Processing` transitions logged
- ✅ At least 3 `Processing → Idle` transitions logged
- ✅ No ERROR or WARN entries (unless hotkey conflict was tested)

**Pass/Fail:** ___________

**Notes:**
_____________________________________________

---

### T1-008: App Shutdown & Restart

**User Story:** General stability (NFR-003)

**Objective:** Verify clean shutdown and restart without errors.

**Steps:**
1. With app running, right-click tray icon
2. Select `"Beenden"` (Exit)
3. Observe tray icon disappears
4. Check Task Manager → LocalWhisper process is gone
5. **Wait 5 seconds**
6. Launch `LocalWhisper.exe` again
7. Verify tray icon reappears

**Expected Results:**
- ✅ App exits immediately (no hang, < 2 seconds)
- ✅ Tray icon disappears
- ✅ Process terminates cleanly (not visible in Task Manager)
- ✅ **No error dialogs** on exit
- ✅ **No error dialogs** on restart
- ✅ App restarts successfully with same behavior as first launch

**Pass/Fail:** ___________

**Notes:**
_____________________________________________

---

## Test Summary

**Tester Name:** _________________________

**Test Date:** _________________________

**Build Version:** _________________________

**Test Results:**

| Test ID | Pass | Fail | Notes |
|---------|------|------|-------|
| T1-001 | ☐ | ☐ | |
| T1-002 | ☐ | ☐ | |
| T1-003 | ☐ | ☐ | |
| T1-004 | ☐ | ☐ | |
| T1-005 | ☐ | ☐ | |
| T1-006 | ☐ | ☐ | |
| T1-007 | ☐ | ☐ | |
| T1-008 | ☐ | ☐ | |

**Overall Pass Rate:** _____ / 8 tests

**Critical Issues Found:**
_____________________________________________
_____________________________________________

**Non-Critical Issues Found:**
_____________________________________________
_____________________________________________

**Recommendation:**
- ☐ **PASS** - Ready for next iteration
- ☐ **PASS WITH MINOR ISSUES** - Proceed with noted issues
- ☐ **FAIL** - Requires fixes before proceeding

---

## Environment Information

**Windows Version:** _________________________

**DPI Scaling:** _________________________

**Keyboard Layout:** _________________________

**Other Running Apps:** _________________________

---

## Appendix: Troubleshooting

### Issue: Tray icon doesn't appear

**Possible Causes:**
- Windows Explorer not running
- System tray overflow (icon hidden)

**Resolution:**
1. Press `Win+B` to focus system tray
2. Click arrow to show hidden icons
3. Look for LocalWhisper icon

---

### Issue: Hotkey doesn't work

**Possible Causes:**
- Another app registered the same hotkey
- Keyboard layout conflict

**Resolution:**
1. Check app.log for "Hotkey registration failed"
2. Try closing other apps (AutoHotkey, macro tools)
3. Restart LocalWhisper

---

### Issue: App crashes on launch

**Possible Causes:**
- Missing .NET runtime (should not happen with self-contained build)
- Corrupted download

**Resolution:**
1. Check Event Viewer → Windows Logs → Application
2. Re-download LocalWhisper.exe
3. Verify file size is ~150-200 MB

---

**Last updated:** 2025-11-17
**Version:** v1.0 (Iteration 1 manual test script)
