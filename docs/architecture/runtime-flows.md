# Runtime Flows

**Purpose:** Detailed sequence diagrams and flow descriptions for key scenarios
**Audience:** Developers implementing features
**Status:** Normative (implementation should match these flows)
**Last Updated:** 2025-09-17

---

## Flow 1: End-to-End Dictation (Happy Path)

**Scenario:** User successfully dictates a 5-second message

### Sequence Diagram (Text)

```
[User] → [TrayApp]: Press & hold hotkey (Ctrl+Shift+D)
[TrayApp] → [HotkeyManager]: OnHotkeyDown event
[HotkeyManager] → [StateMachine]: Transition(Idle → Recording)
[StateMachine] → [AppLogger]: Log state change
[StateMachine] → [TrayIcon]: Update icon (recording indicator)
[TrayApp] → [AudioRecorder]: StartRecording()
[AudioRecorder] → [WASAPI]: Begin capture
[AudioRecorder] → [AppLogger]: Log recording start

... User speaks for 5 seconds ...

[User] → [TrayApp]: Release hotkey
[TrayApp] → [HotkeyManager]: OnHotkeyUp event
[HotkeyManager] → [AudioRecorder]: StopRecording()
[AudioRecorder] → [WASAPI]: End capture
[AudioRecorder] → [Filesystem]: Write WAV file (tmp/rec_*.wav)
[AudioRecorder] → [TrayApp]: Return WAV path
[TrayApp] → [StateMachine]: Transition(Recording → Processing)
[StateMachine] → [AppLogger]: Log state change
[StateMachine] → [TrayIcon]: Update icon (processing indicator)
[TrayApp] → [WhisperCLIAdapter]: Transcribe(wavPath)
[WhisperCLIAdapter] → [ProcessManager]: Launch whisper-cli.exe
[ProcessManager] → [Whisper CLI]: Execute with args
[Whisper CLI] → [ProcessManager]: Exit 0, write stt_result.json
[ProcessManager] → [WhisperCLIAdapter]: Return exit code, stdout, stderr
[WhisperCLIAdapter] → [Filesystem]: Read stt_result.json
[WhisperCLIAdapter] → [WhisperCLIAdapter]: Parse JSON
[WhisperCLIAdapter] → [TrayApp]: Return transcript text
[TrayApp] → [ClipboardService]: Write(text)
[ClipboardService] → [Windows Clipboard API]: SetText(text)
[ClipboardService] → [AppLogger]: Log clipboard write
[TrayApp] → [HistoryWriter]: Write(text, metadata)
[HistoryWriter] → [SlugGenerator]: Generate slug
[SlugGenerator] → [HistoryWriter]: Return slug
[HistoryWriter] → [Filesystem]: Create history/YYYY/MM/DD/file.md
[HistoryWriter] → [AppLogger]: Log file path
[TrayApp] → [FlyoutNotification]: Show("Transkript im Clipboard")
[FlyoutNotification] → [Screen]: Display flyout near tray
[TrayApp] → [StateMachine]: Transition(Processing → Idle)
[StateMachine] → [AppLogger]: Log state change
[StateMachine] → [TrayIcon]: Update icon (idle)
[TrayApp] → [Filesystem]: Delete WAV file (tmp cleanup)
```

### Steps with Timing (p95 Target: ≤ 2.5s from hotkey up to clipboard)

| Step | Component | Action | Est. Time |
|------|-----------|--------|-----------|
| 1 | HotkeyManager | Detect key up | < 10ms |
| 2 | AudioRecorder | Stop capture, write WAV | 50-100ms |
| 3 | StateMachine | Transition to Processing | < 5ms |
| 4 | WhisperCLIAdapter | Launch CLI | 50-100ms |
| 5 | Whisper CLI | Transcribe (5s audio) | 800-1500ms |
| 6 | WhisperCLIAdapter | Parse JSON | 10-20ms |
| 7 | ClipboardService | Write to clipboard | 10-30ms |
| 8 | HistoryWriter | Write file | 30-50ms |
| 9 | FlyoutNotification | Show flyout | 100-300ms |
| **Total** | | **p95: 1.0-2.1s** | ✓ Within target |

---

## Flow 2: First-Run Wizard

**Scenario:** New user starts the app for the first time

### Step-by-Step Flow

**App Startup:**
1. `TrayApp.Main()` executes
2. Check if `config.toml` exists
3. If not found → Launch `WizardWindow`, block main app startup

**Wizard Step 1: Data Root Selection**
4. Show default path: `%LOCALAPPDATA%\SpeechClipboardApp\`
5. User accepts default OR clicks "Browse" and selects folder
6. Wizard validates folder is writable (attempt to create test file)
7. If success: create subfolders (`config/`, `models/`, `history/`, `logs/`, `tmp/`)
8. If failure: show error, allow retry
9. Click "Next"

**Wizard Step 2: Model Setup**
10. Show two options:
    - Option A: "Download model 'small' for German/English (recommended)"
    - Option B: "I have already downloaded the model" (file picker)
11. If Option A selected:
    - Show download progress bar
    - Download model to `<DATA_ROOT>/models/ggml-small-de.gguf`
    - On completion: proceed to hash check
12. If Option B selected:
    - User browses to model file
    - Copy to `<DATA_ROOT>/models/` (or use in place if already there)
13. Compute SHA-256 hash of model file
14. Compare to known-good hash (hardcoded or from config)
15. If match: show "Modell OK ✓", enable "Next" button
16. If mismatch: show "Modell ungültig oder beschädigt", allow retry
17. Click "Next"

**Wizard Step 3: Hotkey Configuration**
18. Show default hotkey: `Ctrl+Shift+D`
19. User accepts default OR clicks "Change" and presses new combo
20. Wizard attempts to register hotkey (test registration)
21. If conflict: show warning "Hotkey bereits belegt", allow retry
22. If success: save hotkey to config
23. ~~Show checkbox: "Start with Windows"~~ (REMOVED for v0.1)
24. Click "Finish"

**Finalization:**
25. Write `config.toml` with all settings
26. Log wizard completion
27. Close wizard window
28. Resume `TrayApp` startup (show tray icon, register hotkey)

**Error Paths:**
- If user closes wizard before finishing: exit app (no partial config)
- If any step fails critically: show error, offer "Start Over" or "Exit"

---

## Flow 3: Error Handling (Microphone Denied)

**Scenario:** User presses hotkey, but microphone permissions are denied

### Flow

```
[User] → [TrayApp]: Press hotkey
[TrayApp] → [HotkeyManager]: OnHotkeyDown event
[HotkeyManager] → [StateMachine]: Transition(Idle → Recording)
[TrayApp] → [AudioRecorder]: StartRecording()
[AudioRecorder] → [WASAPI]: Begin capture
[WASAPI] → [AudioRecorder]: Throw COMException (access denied)
[AudioRecorder] → [TrayApp]: Catch exception, return error
[TrayApp] → [ErrorDialogs]: Show("Mikrofon nicht verfügbar. Bitte überprüfen Sie...")
[TrayApp] → [AppLogger]: Log error with exception details
[TrayApp] → [StateMachine]: Transition(Recording → Idle)
[StateMachine] → [TrayIcon]: Update icon (idle, not crashed)
```

**Key Points:**
- App does **not** crash
- User sees actionable error message
- App returns to Idle state, ready for retry
- Error is logged with full context

---

## Flow 4: Post-Processing (Optional)

**Scenario:** User has enabled post-processing; dictation is processed through LLM

### Modified Flow (from Flow 1)

**After Whisper returns transcript:**

```
[WhisperCLIAdapter] → [TrayApp]: Return transcript text (original)
[TrayApp] → [ConfigManager]: Check if post-processing enabled
[ConfigManager] → [TrayApp]: Return true
[TrayApp] → [PostProcessorAdapter]: Process(text)
[PostProcessorAdapter] → [ProcessManager]: Launch llm-cli.exe
[ProcessManager] → [LLM CLI]: Execute with prompt + text (via stdin)
[LLM CLI] → [ProcessManager]: Exit 0, return formatted text (via stdout)
[ProcessManager] → [PostProcessorAdapter]: Return text
[PostProcessorAdapter] → [TrayApp]: Return formatted text
[TrayApp] → [ClipboardService]: Write(formatted_text)
... (rest of flow continues with formatted text)
```

**Error Path (LLM fails):**

```
[LLM CLI] → [ProcessManager]: Exit 1 (error) OR timeout
[PostProcessorAdapter] → [AppLogger]: Log error
[PostProcessorAdapter] → [TrayApp]: Return original text (fallback)
[TrayApp] → [FlyoutNotification]: Show("Post-Processing fehlgeschlagen (Original-Text verwendet)")
[TrayApp] → [ClipboardService]: Write(original_text)
... (rest of flow with original text)
```

**Key Points:**
- Fallback to original text ensures clipboard always gets content
- User is notified of fallback via flyout
- Latency target still applies (post-processing adds max 1s)

---

## Flow 5: Reset/Uninstall

**Scenario:** User wants to completely remove the app

### Flow

```
[User] → [TrayIcon]: Right-click → "Zurücksetzen/Deinstallieren..."
[TrayApp] → [ErrorDialogs]: Show confirmation dialog with 3 options
[User] → [ErrorDialogs]: Click "Alles löschen"
[ErrorDialogs] → [TrayApp]: Return confirmation
[TrayApp] → [ResetService]: DeleteAll()
[ResetService] → [Filesystem]: Recursively delete <DATA_ROOT>
[ResetService] → [AppLogger]: Log deletion (files removed)
[ResetService] → [TrayApp]: Return success
[TrayApp] → [ErrorDialogs]: Show "Daten gelöscht. Bitte löschen Sie die EXE-Datei manuell."
[TrayApp] → [TrayApp]: Exit application
```

**Error Path (deletion fails):**

```
[Filesystem] → [ResetService]: Throw IOException (file in use)
[ResetService] → [AppLogger]: Log error with file paths
[ResetService] → [TrayApp]: Return error
[TrayApp] → [ErrorDialogs]: Show "Einige Dateien konnten nicht gelöscht werden..."
[TrayApp] → [TrayApp]: Exit application (partial cleanup)
```

---

## Flow 6: Data Root Missing (Repair)

**Scenario:** App starts, but data root has been moved or deleted

### Flow

```
[User] → [Explorer]: Double-click app EXE
[TrayApp.Main()] → [ConfigManager]: Load config
[ConfigManager] → [Filesystem]: Check if config.toml exists at expected path
[Filesystem] → [ConfigManager]: File not found
[ConfigManager] → [TrayApp]: Return error (config missing)
[TrayApp] → [WizardWindow]: Show repair dialog (not full wizard)
[RepairDialog] → [User]: "Datenordner nicht gefunden. Neuen Ordner wählen oder neu einrichten?"
[User] → [RepairDialog]: Click "Neuen Ordner wählen"
[RepairDialog] → [Filesystem]: Open folder picker
[User] → [Filesystem]: Select moved folder (e.g., D:\Backup\SpeechClipboardApp\)
[RepairDialog] → [ConfigManager]: Validate folder structure
[ConfigManager] → [Filesystem]: Check for config/, models/, history/ subfolders
[Filesystem] → [ConfigManager]: Return validation result
[ConfigManager] → [RepairDialog]: Folder is valid
[RepairDialog] → [ConfigManager]: Update config path, reload config
[RepairDialog] → [TrayApp]: Resume startup
[TrayApp] → [TrayIcon]: Show tray icon (app is functional)
```

**Alternative: User chooses "Neu einrichten":**
- Launch full first-run wizard (Flow 2)
- Old data is orphaned (user can manually recover if needed)

---

## State Machine Diagram

```
        ┌──────────┐
        │   Idle   │ ← (Initial state, hotkey released)
        └──────────┘
             │
             │ HotkeyDown
             ▼
        ┌──────────┐
        │Recording │ ← (Audio capture active)
        └──────────┘
             │
             │ HotkeyUp
             ▼
        ┌──────────┐
        │Processing│ ← (STT + Post-Proc + Write operations)
        └──────────┘
             │
             │ OperationComplete OR Error
             ▼
        ┌──────────┐
        │   Idle   │ ← (Ready for next dictation)
        └──────────┘
```

**Transition Rules:**
- `Idle → Recording`: Only on HotkeyDown
- `Recording → Processing`: Only on HotkeyUp
- `Processing → Idle`: After all operations complete (success or error)
- `Recording → Idle`: If error during recording (e.g., mic denied)
- **No other transitions allowed**

---

## Concurrency & Threading

**UI Thread (WPF Dispatcher):**
- All UI updates (tray icon, dialogs, flyout)
- Event handlers (hotkey events)

**Background Threads:**
- Audio recording (WASAPI runs on audio thread)
- CLI subprocess execution (async)
- File I/O (history write, log write)

**Synchronization:**
- State machine changes are serialized (lock on state object)
- Clipboard write must be on STA thread (WPF dispatcher)
- File writes are async with error handling

**No Concurrent Dictations:**
- If user presses hotkey while in Processing state → ignore (or show flyout "Bitte warten...")
- Queue future dictations? Not in v0.1 (defer to later version)

---

## Related Documents

- **Architecture Overview:** `architecture/architecture-overview.md`
- **Interface Contracts:** `architecture/interface-contracts.md`
- **Use Cases:** `specification/use-cases.md`
- **ADRs:** `adr/0000-index.md`

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial runtime flows)
