# Iteration 1: Hotkey & Application Skeleton

**Duration:** 4-6 hours
**Status:** Planned
**Dependencies:** None (first iteration)
**Last Updated:** 2025-11-17

---

## Overview

**Goal:** Establish foundational application structure with global hotkey registration, state machine, tray icon, logging, and minimal data root setup.

**Value:** After this iteration, the app can launch to tray, respond to hotkey presses, and transition between states (Idle → Recording → Processing → Idle), providing the skeleton for all future features.

**Scope Additions (from planning session 2025-11-17):**
- ✅ Minimal data root creation (`%LOCALAPPDATA%\LocalWhisper\`)
- ✅ Basic config.toml (hotkey only)
- ✅ Logging infrastructure (full Serilog setup)
- ⚠️ Placeholder: Settings window link (resolved in Iter 6)
- ⚠️ Placeholder: Simulated processing (resolved in Iter 3)

---

## User Stories

### US-001: Hotkey Toggles State

**As a** user
**I want** to press and hold a hotkey to start recording
**So that** I can begin dictation hands-free

**Acceptance Criteria:**

**AC-1:** Hotkey down event transitions app state from Idle to Recording
- ✓ State machine enforces valid transition
- ✓ State change is logged with timestamp
- ✓ Log message: `"State transition: Idle -> Recording"`

**AC-2:** Hotkey up event transitions app state from Recording to Processing
- ✓ State machine enforces valid transition
- ✓ State change is logged
- ✓ Log message: `"State transition: Recording -> Processing"`

**AC-3:** Processing completes and returns to Idle
- ✓ Simulated processing delay (~500ms) for Iteration 1
- ✓ State transitions to Idle after simulation
- ✓ Log message: `"Simulated processing complete (no audio/STT yet)"`
- ✓ **TODO(PH-002, Iter-3):** Replace simulation with real STT processing

**AC-4:** Invalid transitions are rejected
- ✓ Direct transition from Idle to Processing throws `InvalidStateTransitionException`
- ✓ App does not crash, error is logged
- ✓ State remains unchanged

**AC-5:** Hotkey works when app is not in focus
- ✓ Hotkey is registered globally via `RegisterHotKey` Win32 API
- ✓ Works even when other apps have focus
- ✓ **Manual test:** T1-003 (see `docs/testing/manual-test-script-iter1.md`)

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 34-75

**Implementation Files:**
- `src/LocalWhisper/Core/StateMachine.cs`
- `src/LocalWhisper/Services/HotkeyManager.cs`
- `src/LocalWhisper/Models/AppState.cs` (enum)

---

### US-002: Tray Icon Shows Status

**As a** user
**I want** to see the app's current state in the tray icon
**So that** I know when recording is active

**Acceptance Criteria:**

**AC-1:** Tray icon displays Idle state
- ✓ Icon: Gray circle (Segoe MDL2: `\uE91F`)
- ✓ Tooltip: `"LocalWhisper: Bereit"` (German) or `"LocalWhisper: Ready"` (English)
- ✓ Icon is visible in system tray

**AC-2:** Tray icon displays Recording state
- ✓ Icon: Red solid circle (Segoe MDL2: `\uE7C8`)
- ✓ Tooltip: `"LocalWhisper: Aufnahme..."` (German) or `"LocalWhisper: Recording..."` (English)
- ✓ Icon update occurs **within 100ms** of state change (NFR-004 partial)

**AC-3:** Tray icon displays Processing state
- ✓ Icon: Blue spinner (Segoe MDL2: `\uEB9E`) with rotation animation
- ✓ Tooltip: `"LocalWhisper: Verarbeitung..."` (German) or `"LocalWhisper: Processing..."` (English)
- ✓ Animation: 360° rotation in 1.5 seconds (smooth, no stutter)

**AC-4:** Right-click context menu
- ✓ Menu items:
  - "Beenden" (Exit) → Exits app cleanly
  - *(Other menu items deferred to later iterations)*
- ✓ Clicking "Beenden" logs shutdown and terminates process

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 77-108

**Implementation Files:**
- `src/LocalWhisper/UI/TrayIcon/TrayIconManager.cs`
- `src/LocalWhisper/Utils/IconResources.cs`
- `src/LocalWhisper/UI/TrayIcon/TrayContextMenu.xaml`

**Design Reference:** `docs/ui/icon-style-guide.md`

---

### US-003: Hotkey Conflict Error Dialog

**As a** user
**I want** to be notified if my chosen hotkey is already in use
**So that** I can choose a different combination

**Acceptance Criteria:**

**AC-1:** Hotkey conflict is detected on startup
- ✓ If `RegisterHotKey` returns false (already registered), detect conflict
- ✓ Error is logged: `"Hotkey registration failed: Ctrl+Shift+D already in use"`
- ✓ Log level: WARNING

**AC-2:** Error dialog appears
- ✓ Dialog title: `"Hotkey nicht verfügbar"`
- ✓ Dialog message: `"Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination in den Einstellungen."`
- ✓ Dialog icon: Warning icon (yellow triangle, Segoe MDL2: `\uE7BA`)
- ✓ Buttons: `"Einstellungen öffnen"` | `"OK"`

**AC-3:** App remains running (no crash)
- ✓ After dialog is closed, app stays in tray
- ✓ App is in Idle state (hotkey is non-functional until fixed)
- ✓ Tray icon is gray

**AC-4:** Settings button (placeholder for Iteration 1)
- ✓ Clicking "Einstellungen öffnen" shows placeholder dialog:
  - Title: `"Not Yet Implemented"`
  - Message: `"Settings functionality coming in Iteration 6"`
- ✓ **TODO(PH-001, Iter-6):** Replace with real SettingsWindow

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 110-140

**Implementation Files:**
- `src/LocalWhisper/UI/Dialogs/ErrorDialog.xaml`
- `src/LocalWhisper/Services/HotkeyManager.cs` (conflict detection)

**Related Placeholder:** PH-001 (see `docs/meta/placeholders-tracker.md`)

---

## Technical Requirements

### Data Root Setup (New for Iteration 1)

**Rationale:** Logging and config need a folder structure. Instead of waiting for wizard (Iter 5), create minimal setup now.

**Implementation:**

**Location:** `%LOCALAPPDATA%\LocalWhisper\`

**Folder Structure (Iteration 1 minimal):**
```
LocalWhisper/
├── config/
│   └── config.toml  # Minimal: [hotkey] section only
└── logs/
    └── app.log      # Serilog output
```

**Created by:** `App.xaml.cs` on startup (before any other initialization)

**Code Sketch:**
```csharp
private static void EnsureDataRoot()
{
    var dataRoot = GetDataRoot(); // %LOCALAPPDATA%\LocalWhisper
    Directory.CreateDirectory(Path.Combine(dataRoot, "config"));
    Directory.CreateDirectory(Path.Combine(dataRoot, "logs"));

    logger.LogInformation("Data root initialized", new { DataRoot = dataRoot });
}

private static string GetDataRoot()
{
    // TODO(PH-004, Iter-5): User-chosen path from wizard
    var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return Path.Combine(appData, "LocalWhisper");
}
```

**Placeholder:** PH-004 (hardcoded path, wizard allows user choice in Iter 5)

---

### Config File (Minimal Schema)

**File:** `%LOCALAPPDATA%\LocalWhisper\config\config.toml`

**Schema (Iteration 1):**
```toml
[hotkey]
modifiers = ["Ctrl", "Shift"]
key = "D"
```

**Full schema deferred to Iteration 5** (see PH-003)

**Implementation:**
- `src/LocalWhisper/Core/ConfigManager.cs`
  - `Load()` method: Read TOML, return `AppConfig` object
  - `Save()` method: Write TOML from `AppConfig` object
  - Validation: Ensure modifiers array is not empty
- `src/LocalWhisper/Models/AppConfig.cs`
  - Properties: `HotkeyModifiers`, `HotkeyKey`
  - Defaults: `["Ctrl", "Shift"]`, `"D"`

**NuGet Dependency:** `Tomlyn` (v0.17.0)

---

### Logging Infrastructure

**Library:** Serilog (v3.1.1) + Serilog.Sinks.File (v5.0.0)

**Configuration:**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // Default: INFO
    .WriteTo.File(
        path: Path.Combine(GetDataRoot(), "logs", "app.log"),
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Infinite,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 5
    )
    .Enrich.FromLogContext()
    .CreateLogger();
```

**Logged Events (Iteration 1):**
- App start/stop
- Hotkey registration (success/failure)
- State transitions (Idle → Recording → Processing → Idle)
- Errors (exceptions, validation failures)

**Example Log Entries:**
```
[2025-11-17 14:30:15.123] [INF] [App] Application started {"Version":"0.1.0","OS":"Windows 10.0.22000","DataRoot":"C:\\Users\\...\\LocalWhisper"}
[2025-11-17 14:30:15.234] [INF] [HotkeyManager] Hotkey registered {"Modifiers":"Ctrl+Shift","Key":"D"}
[2025-11-17 14:30:22.456] [INF] [StateMachine] State transition {"From":"Idle","To":"Recording"}
[2025-11-17 14:30:27.789] [INF] [StateMachine] State transition {"From":"Recording","To":"Processing"}
[2025-11-17 14:30:28.289] [INF] [StateMachine] Simulated processing complete (no audio/STT yet)
[2025-11-17 14:30:28.290] [INF] [StateMachine] State transition {"From":"Processing","To":"Idle"}
```

**Implementation:** `src/LocalWhisper/Core/AppLogger.cs` (wrapper for Serilog)

---

### State Machine

**States (Enum):**
```csharp
public enum AppState
{
    Idle,
    Recording,
    Processing
}
```

**Valid Transitions:**
- Idle → Recording (hotkey down)
- Recording → Processing (hotkey up)
- Processing → Idle (processing complete)

**Invalid Transitions (throw `InvalidStateTransitionException`):**
- Idle → Processing (skip Recording)
- Recording → Idle (skip Processing)
- Processing → Recording (cannot restart during processing)

**Implementation:**
```csharp
public class StateMachine
{
    public AppState State { get; private set; } = AppState.Idle;
    public event EventHandler<StateChangedEventArgs> StateChanged;

    public void TransitionTo(AppState newState)
    {
        if (!IsValidTransition(State, newState))
        {
            throw new InvalidStateTransitionException($"Invalid transition: {State} -> {newState}");
        }

        var oldState = State;
        State = newState;

        logger.LogInformation("State transition", new { From = oldState, To = newState });
        StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState));
    }

    private bool IsValidTransition(AppState from, AppState to)
    {
        return (from, to) switch
        {
            (AppState.Idle, AppState.Recording) => true,
            (AppState.Recording, AppState.Processing) => true,
            (AppState.Processing, AppState.Idle) => true,
            _ => false
        };
    }
}
```

**File:** `src/LocalWhisper/Core/StateMachine.cs`

---

### Hotkey Manager

**Responsibilities:**
- Register global hotkey via Win32 `RegisterHotKey`
- Listen for `WM_HOTKEY` messages
- Fire events: `HotkeyDown`, `HotkeyUp`
- Detect conflicts (registration failure)

**Win32 Interop:**
```csharp
[DllImport("user32.dll")]
private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

[DllImport("user32.dll")]
private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

private const int WM_HOTKEY = 0x0312;
private const uint MOD_CONTROL = 0x0002;
private const uint MOD_SHIFT = 0x0004;
private const uint VK_D = 0x44;
```

**Implementation:**
```csharp
public class HotkeyManager : IDisposable
{
    private HwndSource _hwndSource;
    private const int HOTKEY_ID = 1;

    public event EventHandler HotkeyDown;
    public event EventHandler HotkeyUp;

    public bool RegisterHotkey(IntPtr windowHandle, uint modifiers, uint key)
    {
        bool success = RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, key);

        if (!success)
        {
            logger.LogWarning("Hotkey registration failed", new { Modifiers = modifiers, Key = key });
            return false;
        }

        logger.LogInformation("Hotkey registered", new { Modifiers = modifiers, Key = key });

        // Hook into WM_HOTKEY messages
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource.AddHook(WndProc);

        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            // Detect key down vs. up (simplified for Iteration 1: use key repeat detection)
            HotkeyDown?.Invoke(this, EventArgs.Empty);
            // TODO: Properly detect key up (may require low-level keyboard hook)
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID);
        _hwndSource?.RemoveHook(WndProc);
    }
}
```

**File:** `src/LocalWhisper/Services/HotkeyManager.cs`

**Note:** Win32 `RegisterHotKey` only fires on key down. For key up detection, we may need:
- **Option A:** Track key state manually (assume up after transition to Processing)
- **Option B:** Use low-level keyboard hook (more complex, defer to Iteration 2 if needed)
- **Iteration 1 Approach:** Simplify by auto-transitioning after hotkey fires (hold duration not enforced yet)

---

### Tray Icon Manager

**Library:** `H.NotifyIcon.Wpf` (v2.0.125)

**Responsibilities:**
- Show tray icon on app startup
- Update icon/tooltip based on state machine events
- Show context menu on right-click

**Implementation:**
```csharp
public class TrayIconManager
{
    private TaskbarIcon _trayIcon;
    private StateMachine _stateMachine;

    public TrayIconManager(StateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        _stateMachine.StateChanged += OnStateChanged;

        _trayIcon = new TaskbarIcon
        {
            IconSource = CreateIconSource(AppState.Idle),
            ToolTipText = IconResources.GetStateTooltip(AppState.Idle, "de"),
            ContextMenu = CreateContextMenu()
        };
    }

    private void OnStateChanged(object sender, StateChangedEventArgs e)
    {
        _trayIcon.IconSource = CreateIconSource(e.NewState);
        _trayIcon.ToolTipText = IconResources.GetStateTooltip(e.NewState, "de");

        // Start/stop animation for Processing state
        if (e.NewState == AppState.Processing)
        {
            StartSpinAnimation();
        }
        else
        {
            StopSpinAnimation();
        }
    }

    private ImageSource CreateIconSource(AppState state)
    {
        // Render Segoe MDL2 icon to ImageSource
        var icon = IconResources.GetStateIcon(state);
        var color = IconResources.GetStateColor(state);

        // Create DrawingVisual with TextBlock (Segoe MDL2 icon)
        // Convert to BitmapSource
        // Return as ImageSource
        // (See icon-style-guide.md for XAML example)
    }

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var exitItem = new MenuItem { Header = "Beenden" };
        exitItem.Click += (s, e) => Application.Current.Shutdown();

        menu.Items.Add(exitItem);
        return menu;
    }
}
```

**File:** `src/LocalWhisper/UI/TrayIcon/TrayIconManager.cs`

**Design Reference:** `docs/ui/icon-style-guide.md`

---

## Definition of Done

**Code:**
- [x] StateMachine class with Idle/Recording/Processing states
- [x] HotkeyManager with Win32 hotkey registration
- [x] TrayIconManager with state-based icon/tooltip
- [x] ErrorDialog for hotkey conflicts (with placeholder Settings button)
- [x] AppLogger wrapper for Serilog
- [x] ConfigManager for minimal config.toml (hotkey only)
- [x] Data root creation on startup (`config/`, `logs/`)
- [x] IconResources helper class

**Tests:**
- [x] Unit tests: StateMachine (valid/invalid transitions)
- [x] Unit tests: ConfigManager (read/write TOML)
- [x] Integration tests: HotkeyManager (mocked Win32 if possible, else manual test only)
- [x] All BDD scenarios for US-001, US-002, US-003 (xUnit with descriptive names)

**Logging:**
- [x] App start/stop logged
- [x] Hotkey registration (success/conflict) logged
- [x] All state transitions logged with structured data
- [x] Errors logged with exception details

**Documentation:**
- [x] Placeholders tracked in `placeholders-tracker.md` (PH-001, PH-002, PH-003, PH-004)
- [x] Traceability matrix updated (link code files to FR/US)
- [x] Changelog entry added

**Manual Testing:**
- [x] Manual test script executed (`docs/testing/manual-test-script-iter1.md`)
- [x] All 8 test cases pass
- [x] Build tested on clean Windows 10/11 VM (if possible)

**Performance:**
- [x] Tray icon updates within 100ms of state change (NFR-004 partial)
- [x] App startup time < 3 seconds (cold start)

---

## Implementation Order

**Recommended sequence (TDD approach):**

1. **Setup project structure**
   - Create `LocalWhisper.sln`, `LocalWhisper.csproj`, `LocalWhisper.Tests.csproj`
   - Add NuGet packages: Serilog, H.NotifyIcon.Wpf, Tomlyn, xUnit

2. **Implement logging & data root**
   - `Core/AppLogger.cs`: Serilog wrapper
   - `App.xaml.cs`: Data root creation on startup
   - Test: Verify log file is created

3. **Implement state machine (TDD)**
   - Write tests: `StateMachineTests.cs`
     - `Idle_to_Recording_IsValid()`
     - `Recording_to_Processing_IsValid()`
     - `Processing_to_Idle_IsValid()`
     - `Idle_to_Processing_ThrowsException()`
   - Implement: `Core/StateMachine.cs`
   - Run tests → GREEN

4. **Implement config manager (TDD)**
   - Write tests: `ConfigManagerTests.cs`
     - `Load_ValidToml_ReturnsConfig()`
     - `Save_WritesToml_FileExists()`
     - `Load_MissingFile_ReturnsDefaults()`
   - Implement: `Core/ConfigManager.cs`, `Models/AppConfig.cs`
   - Run tests → GREEN

5. **Implement icon resources**
   - `Utils/IconResources.cs`: Constants and helper methods
   - Test: Verify GetStateIcon() returns correct glyphs

6. **Implement hotkey manager**
   - `Utils/Win32Interop.cs`: P/Invoke declarations
   - `Services/HotkeyManager.cs`: Hotkey registration
   - Manual test: Run app, verify hotkey is registered

7. **Implement tray icon**
   - `UI/TrayIcon/TrayIconManager.cs`: Tray icon with state updates
   - Test: Verify icon changes on state machine events

8. **Implement error dialog**
   - `UI/Dialogs/ErrorDialog.xaml`: XAML dialog with buttons
   - Add placeholder for Settings button (PH-001)
   - Test: Manually trigger hotkey conflict

9. **Wire everything together in App.xaml.cs**
   - Initialize data root
   - Initialize logging
   - Load config
   - Create state machine
   - Create hotkey manager
   - Create tray icon manager
   - Register hotkey (handle conflicts)

10. **Manual testing**
    - Execute `docs/testing/manual-test-script-iter1.md`
    - Fix any issues

11. **Update documentation**
    - Update `traceability-matrix.md`
    - Add changelog entry
    - Commit with proper message

---

## Related Documents

- **User Stories (Gherkin):** `docs/specification/user-stories-gherkin.md` (lines 30-141)
- **Functional Requirements:** `docs/specification/functional-requirements.md` (FR-010, FR-021, FR-023)
- **Non-Functional Requirements:** `docs/specification/non-functional-requirements.md` (NFR-002, NFR-006)
- **Architecture Overview:** `docs/architecture/architecture-overview.md`
- **Project Structure:** `docs/architecture/project-structure.md`
- **Icon Style Guide:** `docs/ui/icon-style-guide.md`
- **Placeholders Tracker:** `docs/meta/placeholders-tracker.md`
- **Manual Test Script:** `docs/testing/manual-test-script-iter1.md`
- **ADR-0001:** Platform choice (.NET 8 + WPF)

---

## Risk Register (Iteration 1 Specific)

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Hotkey conflicts with common apps | Medium | Medium | Clear error dialog, logged for debugging |
| Win32 interop issues (P/Invoke) | Low | High | Use tested libraries (H.NotifyIcon wraps Win32) |
| Icon rendering issues at high DPI | Low | Low | Segoe MDL2 is vector-based (DPI-independent) |
| Hotkey up detection complexity | Medium | Low | Simplify for Iter 1 (auto-transition), improve in Iter 2 |

---

**Last updated:** 2025-11-17
**Version:** v0.2 (Revised with data root, config, placeholders)
**Next Iteration:** [iteration-02-audio-recording.md](iteration-02-audio-recording.md) (create after Iter 1 complete)
