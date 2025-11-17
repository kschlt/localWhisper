# Functional Requirements

**Purpose:** Detailed, testable functional capabilities of the Dictate-to-Clipboard application
**Format:** Each requirement includes ID, description, and Fit Criteria (measurable verification)
**Status:** Stable (v0.1 baseline)
**Last Updated:** 2025-09-17

---

## Core Dictation Flow

### FR-010 — Hotkey (Hold-to-Talk)

**Description:**
The app registers a global hotkey that works system-wide (even when app is not in focus). Pressing and holding the hotkey starts recording; releasing it stops recording and triggers transcription.

**Fit Criteria:**
- ✓ Hotkey down event transitions app state from Idle to Recording (logged)
- ✓ Hotkey up event transitions app state from Recording to Processing (logged)
- ✓ Hotkey works when any other application has focus
- ✓ Hotkey conflict (already registered by another app) is detected and reported via dialog
- ✓ Default hotkey is `Ctrl+Shift+D` (user-configurable)

**Related:**
- UC-001
- US-001, US-002, US-003 (Iteration 1)
- ADR-0001 (Platform)

---

### FR-011 — Audio Recording

**Description:**
While the hotkey is held down, the app records audio from the default microphone using WASAPI. Output is a WAV file (16kHz, mono, 16-bit) saved to the `tmp/` directory.

**Fit Criteria:**
- ✓ WAV file is created in `<DATA_ROOT>/tmp/` with naming pattern `rec_{yyyyMMdd_HHmmssfff}.wav`
- ✓ WAV format is valid: 16kHz sample rate, mono channel, 16-bit PCM
- ✓ Recording duration > 1 second produces valid audio (verified with `soxi` or audio library)
- ✓ Microphone unavailable or denied: error dialog shown, app remains stable
- ✓ Recording is logged with timestamp and file path

**Related:**
- UC-001
- US-010, US-011, US-012 (Iteration 2)
- ADR-0001 (Platform: WASAPI via .NET)

---

### FR-012 — STT with Whisper (CLI)

**Description:**
After recording stops, the app invokes `whisper-cli.exe` (external CLI tool) with the WAV file, language setting, and model path. The CLI outputs a JSON file with transcription results, which the app parses.

**Fit Criteria:**
- ✓ For a 5-second audio sample, STT produces non-empty text output
- ✓ CLI is invoked with correct parameters: `--model <path> --language <lang> --output-format json <wav-file>`
- ✓ Output file `stt_result.json` is created and parsed into app's data model (text, lang, duration_sec, segments, meta)
- ✓ On error (missing model, CLI crash, timeout), app shows user-friendly dialog (not technical stack trace)
- ✓ Timeout (e.g., 60s) prevents indefinite hang
- ✓ Exit codes are mapped: 0=success, 2=model error, 3=device error, 4=timeout, 5=other
- ✓ STT operation is logged with duration, file size, and result status

**Related:**
- UC-001
- US-020, US-021, US-022, US-023 (Iteration 3)
- ADR-0002 (CLI subprocess approach)
- NFR-001 (Performance)
- NFR-006 (Observability)

**Updated:** 2025-09-18 — CLI subprocess approach per ADR-0002 (was: FFI in earlier draft)

---

### FR-013 — Clipboard Write

**Description:**
After successful transcription (and optional post-processing), the app writes the final text to the Windows system clipboard.

**Fit Criteria:**
- ✓ Immediately after clipboard write, pressing `Ctrl+V` in Notepad pastes the exact transcription text
- ✓ Unicode characters are preserved correctly
- ✓ Clipboard write operation is logged
- ✓ If clipboard write fails (rare Windows API error), error is logged and user is notified

**Related:**
- UC-001
- US-030 (Iteration 4)
- NFR-001 (Performance: part of E2E latency)

---

### FR-014 — History File

**Description:**
Simultaneously with clipboard write, the app saves the transcription as a Markdown file in the history directory. The file includes YAML front-matter with metadata.

**Fit Criteria:**
- ✓ File is created at path: `<DATA_ROOT>/history/YYYY/YYYY-MM/YYYY-MM-DD/YYYYMMDD_HHMMSS_<slug>.md`
- ✓ Front-matter includes fields: `created` (ISO-8601), `lang`, `stt_model`, `duration_sec`
- ✓ Markdown body includes a heading `# Diktat – {dd.MM.yyyy HH:mm}` followed by the transcription text
- ✓ File is UTF-8 encoded
- ✓ If history write fails (disk full, permissions), clipboard write still succeeds; error is logged and user is notified
- ✓ History write operation is logged with file path

**Example file content:**
```markdown
---
created: 2025-09-17T14:30:22Z
lang: de
stt_model: whisper-small
duration_sec: 4.8
---
# Diktat – 17.09.2025 14:30

Let me check on that and get back to you tomorrow morning.
```

**Related:**
- UC-001
- US-031 (Iteration 4)
- FR-024 (Slug generation)
- ADR-0003 (Storage layout)

---

### FR-015 — Toast/Status (Custom Flyout)

**Description:**
After clipboard and history operations complete, the app displays a brief custom flyout notification near the system tray, confirming that the transcript is ready to paste. The tray icon also reflects the current state (Idle, Recording, Processing).

**Fit Criteria:**
- ✓ Flyout appears with message "Transkript im Clipboard" (German) or "Transcript in Clipboard" (English, based on app language)
- ✓ Flyout is visible for ~3 seconds, then auto-dismisses
- ✓ Flyout does not steal focus from the active window
- ✓ Tray icon changes appearance for each state: Idle (default icon), Recording (red/active icon), Processing (spinner/processing icon)
- ✓ Flyout appearance latency ≤ 0.5s after clipboard write (NFR-004)

**Related:**
- UC-001
- US-032, US-034 (Iteration 4)
- ADR-0005 (Custom flyout, not Windows toast)
- NFR-004 (Usability: flyout latency)

---

## Setup & Configuration

### FR-016 — First-Run Wizard (3 Steps)

**Description:**
On first launch (when no config file exists), the app opens a wizard to guide the user through initial setup. The wizard has 3 steps: Data Root selection, Model verification, and Hotkey/Autostart configuration.

**Fit Criteria:**

**Step 1: Data Root**
- ✓ Wizard shows default path `%LOCALAPPDATA%\SpeechClipboardApp\` (resolved to actual path)
- ✓ User can browse and select a custom folder
- ✓ App verifies chosen folder is writable (attempts to create a test file)
- ✓ If not writable, error message is shown and user can choose another folder
- ✓ App creates subdirectories: `config/`, `models/`, `history/`, `logs/`, `tmp/`

**Step 2: Model Verification**
- ✓ Wizard shows two options: "Download model" or "I have the model already"
- ✓ For download: app downloads `ggml-small.bin` to `<DATA_ROOT>/models/`
- ✓ For existing model: user browses to select the file
- ✓ App computes SHA-256 hash of model file
- ✓ If hash matches known-good value: "Modell OK ✓"
- ✓ If hash mismatch: "Modell ungültig oder beschädigt" → user can retry or choose different file
- ✓ Model path and hash are saved to config

**Step 3: Hotkey Configuration**
- ✓ Wizard shows default hotkey `Ctrl+Shift+D`
- ✓ User can click a button to record a custom hotkey combination
- ✓ If hotkey is already registered (conflict), warning is shown
- ✓ Hotkey is saved to config
- ✓ ~~Checkbox: "Start with Windows" (autostart)~~ (REMOVED - out of scope for v0.1)

**Finalization**
- ✓ Wizard saves all settings to `<DATA_ROOT>/config/config.toml`
- ✓ Wizard closes automatically
- ✓ App starts normally (no wizard on subsequent launches unless data root is missing)

**Repair Flow (UC-003):**
- ✓ If data root is missing on startup, app shows repair dialog: "Datenordner nicht gefunden. Neu wählen oder neu einrichten?"
- ✓ User can re-link to moved folder OR restart wizard

**Related:**
- UC-002, UC-003
- US-040, US-041, US-042, US-043, US-044, US-046 (Iteration 5)
- FR-017 (Model management)
- ~~FR-018 (Autostart)~~ (REMOVED)
- NFR-004 (Wizard < 2 min)

---

### FR-017 — Model Management

**Description:**
The app stores the path to the Whisper model file and its SHA-256 hash in the config. A "Check Model" function verifies model integrity at any time.

**Fit Criteria:**
- ✓ Config file (`config.toml`) contains `model_path` and `model_hash_sha256` fields
- ✓ "Check Model" button in settings recomputes hash and compares to stored value
- ✓ If hash matches: "Modell OK ✓" message
- ✓ If hash mismatch: "Modell ungültig. Bitte laden Sie es erneut." message
- ✓ If model file is missing: "Modell nicht gefunden. Pfad: <path>" message
- ✓ Hash check operation is logged

**Related:**
- UC-002
- US-041, US-053 (Iteration 5, 6)
- ADR-0003 (Storage layout)

---

### ~~FR-018 — Autostart (without Admin)~~ (REMOVED)

**Status:** Out of scope for v0.1

**Original Description:**
~~Option to start the app automatically when Windows starts, implemented via a shortcut in the user's Startup folder (no registry or Task Scheduler required).~~

**Reason for removal:**
Feature deferred to post-v0.1 to reduce scope and complexity.

---

### FR-019 — Reset/Deinstallieren

**Description:**
The app provides a "Reset/Uninstall" function accessible via the tray menu. This removes all app data after confirmation. The EXE itself must be deleted manually.

**Fit Criteria:**
- ✓ Tray menu has option "Zurücksetzen/Deinstallieren..."
- ✓ Confirmation dialog shows: "Dies wird alle Einstellungen und Transkripte entfernen. Fortfahren?"
- ✓ Dialog buttons: "Abbrechen" | "Nur Einstellungen löschen" | "Alles löschen"
- ✓ "Alles löschen" → Entire data root folder is deleted recursively
- ✓ ~~Autostart shortcut is removed~~ (REMOVED - autostart out of scope)
- ✓ Final message: "Daten gelöscht. Bitte löschen Sie die EXE-Datei manuell."
- ✓ App exits after reset
- ✓ Reset operation is logged
- ✓ If deletion fails (files in use), error is shown with details

**Related:**
- UC-004
- US-070 (Iteration 8)
- ADR-0003 (Storage layout)

---

### FR-020 — Settings (Basis)

**Description:**
The app has a Settings window accessible via the tray menu, allowing users to change configuration after initial setup.

**Fit Criteria:**

**Settings available:**
- ✓ Hotkey: change global hotkey combination
- ✓ Data Root: change base folder (triggers migration/relink dialog)
- ✓ ~~Autostart: toggle on/off~~ (REMOVED - out of scope)
- ✓ File Format: choose `.md` or `.txt` for history files
- ✓ App Language: `de` or `en` (affects UI and default STT language)
- ✓ Model: show current model path, button to "Check Model" (FR-017), button to "Choose different model"

**Persistence:**
- ✓ All changes are saved to `config.toml` immediately (or on "Save" button click)
- ✓ Changes take effect after app restart (or immediately if feasible)

**Validation:**
- ✓ Hotkey conflicts are detected and reported
- ✓ Data root changes are validated (folder exists, writable)

**Related:**
- UC-002, UC-001
- US-050, US-051, US-052, US-053 (Iteration 6)
- NFR-006 (Observability: log settings changes)

---

## Error Handling & Robustness

### FR-021 — Fehlerfälle (Dialoge)

**Description:**
The app handles common error scenarios gracefully with user-friendly dialogs (not crashes or technical stack traces). All errors are logged.

**Fit Criteria:**

**Error scenarios covered:**

1. **Schreibschutz (Write-protected folder):**
   - ✓ Dialog: "Ordner ist schreibgeschützt. Bitte wählen Sie einen anderen Ordner."
   - ✓ App remains stable, allows user to choose different folder

2. **Mikro nicht verfügbar/gesperrt:**
   - ✓ Dialog: "Mikrofon nicht verfügbar. Bitte überprüfen Sie die Berechtigungen und Geräteverbindung."
   - ✓ App returns to Idle state, does not crash

3. **Hotkey belegt:**
   - ✓ Dialog: "Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination in den Einstellungen."
   - ✓ App continues running (falls back to no hotkey until user fixes it)

4. **Fehlendes CLI/Modell:**
   - ✓ Dialog: "Whisper-CLI nicht gefunden. Pfad: <expected-path>. Bitte installieren Sie es oder passen Sie die Konfiguration an."
   - ✓ App remains functional for other operations (settings, history view)

5. **Timeouts:**
   - ✓ If STT takes > 60s, operation is canceled
   - ✓ Dialog: "Transkription dauert zu lange und wurde abgebrochen. Probieren Sie eine kürzere Aufnahme."

6. **Disk full / History write failure:**
   - ✓ Clipboard write succeeds anyway (prioritize immediate user need)
   - ✓ Dialog: "History konnte nicht gespeichert werden. Speicherplatz prüfen."

**All error cases:**
- ✓ Produce specific, user-friendly dialog messages (not generic "Error occurred")
- ✓ Are logged with timestamp, error type, context (file paths, user action, etc.)
- ✓ Do not cause app to crash or freeze

**Related:**
- UC-001, UC-003
- US-003, US-012, US-022, US-046, US-071 (Iterations 1, 2, 3, 5, 8)
- NFR-003 (Reliability)
- NFR-006 (Observability)

---

## Optional Features

### FR-022 — Optionales Post-Processing

**Description:**
An optional processing step that runs after STT, using a small local LLM (via CLI) to improve formatting and apply user glossary. Can be toggled on/off in settings. **Must not change meaning** of the transcription.

**Fit Criteria:**
- ✓ Settings has toggle: "Post-Processing aktivieren" (default: off)
- ✓ When enabled: after STT, app invokes `llm-cli.exe` with transcript text (via stdin) and prompt: "Format lists, apply glossary, fix punctuation. Do not change meaning."
- ✓ LLM output replaces STT output for clipboard and history
- ✓ If post-processing fails (timeout, crash, invalid output): app falls back to original STT text, logs error, shows warning flyout
- ✓ Post-processing latency is measured and logged
- ✓ User can provide custom glossary file (e.g., `config/glossary.txt`) for abbreviation expansion

**Related:**
- UC-001 (optional enhancement)
- US-060, US-061, US-062 (Iteration 7)
- ADR-0002 (CLI subprocess approach)

---

## Observability

### FR-023 — Logging

**Description:**
The app writes structured logs to a file for debugging, diagnostics, and performance analysis.

**Fit Criteria:**
- ✓ Log file location: `<DATA_ROOT>/logs/app.log`
- ✓ Log format: `[YYYY-MM-DD HH:MM:SS] [LEVEL] [Component] Message {StructuredData}`
- ✓ Log levels: DEBUG, INFO, WARN, ERROR
- ✓ Logged events include:
  - App start/stop
  - Hotkey registration (success/conflict)
  - State transitions (Idle → Recording → Processing → Idle)
  - Audio recording start/stop (file path, duration)
  - STT invocation (command, duration, exit code, result length)
  - Clipboard write (success/failure)
  - History write (file path, success/failure)
  - Settings changes (old value → new value)
  - Errors (with full context: file paths, error codes, stack traces)
- ✓ Log rotation or size management (e.g., max 10 MB, then archive)
- ✓ Logs do not contain sensitive user data (transcript content may be logged at DEBUG level only, not in production)

**Related:**
- All UC, all iterations
- US-072 (Iteration 8)
- NFR-006 (Observability)

---

## Naming & Organization

### FR-024 — Slug & Dateinamen

**Description:**
History file names include a "slug" derived from the first 6-10 words of the transcription, making files easier to identify at a glance.

**Fit Criteria:**
- ✓ File naming pattern: `YYYYMMDD_HHMMSS_<slug>.md`
- ✓ Slug generation rules:
  1. Extract first 6-10 words from transcript (stop at first sentence break if shorter)
  2. Convert to lowercase
  3. Replace non-alphanumeric characters (except spaces) with hyphens `-`
  4. Replace spaces with hyphens
  5. Compress multiple consecutive hyphens into one
  6. Trim leading/trailing hyphens
  7. Limit slug length to 50 characters (truncate if longer)

**Example:**
- Input: "Let me check on that and get back to you tomorrow"
- Slug: `let-me-check-on-that-and-get`
- Full filename: `20250917_143022_let-me-check-on-that-and-get.md`

**Edge cases:**
- ✓ Empty transcript → slug = `empty`
- ✓ Transcript with only special characters → slug = `transcript`
- ✓ Duplicate slugs (same timestamp unlikely, but possible) → append counter `_2`, `_3`, etc.

**Related:**
- UC-001
- US-036 (Iteration 4)
- FR-014 (History file)
- ADR-0003 (Storage layout)

---

## Requirements Summary Table

| ID | Title | Iteration(s) | Status |
|----|-------|--------------|--------|
| FR-010 | Hotkey (Hold-to-Talk) | 1 | Planned |
| FR-011 | Audio Recording | 2 | Planned |
| FR-012 | STT with Whisper (CLI) | 3 | Planned |
| FR-013 | Clipboard Write | 4 | Planned |
| FR-014 | History File | 4 | Planned |
| FR-015 | Custom Flyout | 4 | Planned |
| FR-016 | First-Run Wizard | 5 | Planned |
| FR-017 | Model Management | 5, 6 | Planned |
| ~~FR-018~~ | ~~Autostart~~ | ~~6~~ | REMOVED (out of scope) |
| FR-019 | Reset/Uninstall | 8 | Planned |
| FR-020 | Settings | 6 | Planned |
| FR-021 | Error Dialogs | 1-8 | Planned |
| FR-022 | Post-Processing | 7 | Planned |
| FR-023 | Logging | 1-8 | Planned |
| FR-024 | Slug Generation | 4 | Planned |

---

## Related Documents

- **Use Cases:** `specification/use-cases.md`
- **Non-Functional Requirements:** `specification/non-functional-requirements.md`
- **Data Structures:** `specification/data-structures.md`
- **Architecture:** `architecture/architecture-overview.md`
- **ADRs:** `adr/0000-index.md`
- **Traceability:** `specification/traceability-matrix.md`

---

**Last updated:** 2025-09-17
**Version:** v0.1 (FR-018 removed; ADR-0002 and ADR-0005 integrated)
