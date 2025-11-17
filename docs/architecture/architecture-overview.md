# Architecture Overview

**Purpose:** High-level system design and component structure
**Audience:** Developers, architects, AI agents
**Status:** Stable (v0.1 baseline)
**Last Updated:** 2025-09-17

---

## System Context

**Dictate-to-Clipboard** is a standalone Windows desktop application that orchestrates three main workflows:
1. **Audio capture** (microphone → WAV file)
2. **Speech-to-text transcription** (WAV → text via Whisper CLI)
3. **Output delivery** (text → clipboard + history file + user notification)

**External Dependencies:**
- Windows OS (WASAPI for audio, clipboard API, filesystem)
- Whisper CLI (external executable, invoked as subprocess)
- Optional: LLM CLI for post-processing (external executable)

**No Network Dependencies:**
- All processing is local (offline-first)
- Only network call: optional model download during setup

---

## Architecture Style

**Primary:** Layered Architecture with Service-Oriented Components

**Layers:**
1. **Presentation:** Tray UI, Wizard, Settings, Dialogs, Flyout
2. **Application Services:** Hotkey Manager, Audio Recorder, Clipboard Service, History Writer
3. **Adapters:** CLI adapters for Whisper and LLM (boundary to external tools)
4. **Core:** State Machine, Configuration Manager, Logger, Slug Generator
5. **Infrastructure:** Filesystem, Windows API interop, Process management

**Communication:**
- Synchronous method calls within layers
- Event-driven for state changes (e.g., HotkeyDown event → State transition)
- Process I/O (stdin/stdout) for CLI adapters

---

## Component Diagram (Conceptual)

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────────┐ │
│  │ Tray App │  │  Wizard  │  │ Settings │  │  Dialogs   │ │
│  └──────────┘  └──────────┘  └──────────┘  └────────────┘ │
│            ┌─────────────┐                                  │
│            │   Flyout    │                                  │
│            └─────────────┘                                  │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                  Application Services                       │
│  ┌────────────────┐  ┌──────────────┐  ┌────────────────┐ │
│  │ HotkeyManager  │  │ AudioRecorder│  │ ClipboardSvc   │ │
│  └────────────────┘  └──────────────┘  └────────────────┘ │
│  ┌────────────────┐  ┌──────────────┐  ┌────────────────┐ │
│  │ HistoryWriter  │  │ ModelManager │  │  ResetService  │ │
│  └────────────────┘  └──────────────┘  └────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                      Adapters                               │
│  ┌───────────────────┐       ┌───────────────────────────┐ │
│  │ WhisperCLIAdapter │       │ PostProcessorCLIAdapter   │ │
│  └───────────────────┘       └───────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                        Core                                 │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │ StateMachine│ │ ConfigManager│  │  AppLogger       │   │
│  └────────────┘  └──────────────┘  └──────────────────┘   │
│  ┌────────────┐                                            │
│  │SlugGenerator│                                           │
│  └────────────┘                                            │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure                            │
│  ┌────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │ Filesystem │  │ Win32 Interop│  │ Process Manager  │   │
│  └────────────┘  └──────────────┘  └──────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## Component Responsibilities

### Presentation Layer

**TrayApp** (Main Entry Point)
- Initializes app, shows tray icon
- Manages app lifecycle (startup, shutdown)
- Hosts tray context menu (Settings, History, Reset, Exit)
- Wires up all services and event handlers

**WizardWindow**
- First-run setup (3 steps)
- Repair flow (data root missing)
- Validates user inputs, creates folder structure

**SettingsWindow**
- Configuration UI (hotkey, data root, language, model, etc.)
- Persists changes to `config.toml`

**ErrorDialogs**
- Centralized error message display
- User-friendly messages (no stack traces)

**FlyoutNotification**
- Custom notification window (not Windows toast)
- Shows "Transkript im Clipboard" message
- Auto-dismisses after 3 seconds

---

### Application Services

**HotkeyManager**
- Registers global hotkey via Win32 API
- Fires events: `OnHotkeyDown`, `OnHotkeyUp`
- Detects and reports hotkey conflicts

**AudioRecorder**
- Records audio via WASAPI
- Outputs WAV file (16kHz, mono, 16-bit)
- Manages recording state (start/stop)

**ClipboardService**
- Writes text to Windows clipboard
- Handles clipboard API errors

**HistoryWriter**
- Creates Markdown or TXT file in date-based folder structure
- Generates slug for filename
- Writes front-matter metadata

**ModelManager**
- Validates model file (SHA-256 hash check)
- Provides "Check Model" functionality
- Handles model download (optional, future)

**ResetService**
- Deletes data root folder (after confirmation)
- Logs reset operation

---

### Adapters

**WhisperCLIAdapter**
- Invokes `whisper-cli.exe` as subprocess
- Passes WAV file, model path, language
- Parses JSON output (`stt_result.json`)
- Maps exit codes to errors
- Enforces timeout (60s default)

**PostProcessorCLIAdapter** (Optional)
- Invokes LLM CLI as subprocess
- Passes transcript text via stdin
- Receives formatted text via stdout
- Falls back to original text on error

---

### Core

**StateMachine**
- Manages app state: `Idle`, `Recording`, `Processing`
- Enforces valid transitions
- Fires state change events (for UI updates, logging)

**ConfigManager**
- Loads/saves `config.toml`
- Provides strongly-typed access to settings
- Validates configuration on load

**AppLogger**
- Structured logging to `app.log`
- Levels: DEBUG, INFO, WARN, ERROR
- Log rotation at 10 MB

**SlugGenerator**
- Generates URL-safe slug from transcript text
- Implements normalization rules (see FR-024)

---

### Infrastructure

**Filesystem**
- Abstraction for file I/O operations
- Used by HistoryWriter, ConfigManager, Logger

**Win32Interop**
- P/Invoke wrappers for Win32 APIs
- Hotkey registration, clipboard, audio

**ProcessManager**
- Launches and manages external processes (Whisper, LLM)
- Captures stdout/stderr
- Enforces timeouts

---

## Data Flow: End-to-End Dictation

**Sequence:**

```
User                      TrayApp            HotkeyMgr    AudioRecorder   StateMachine   WhisperAdapter   ClipboardSvc   HistoryWriter   Flyout
 |                          |                  |               |               |                |                |               |            |
 |-- Hold Hotkey ---------->|                  |               |               |                |                |               |            |
 |                          |-- OnHotkeyDown ->|               |               |                |                |               |            |
 |                          |                  |-- Start ----->|               |                |                |               |            |
 |                          |                  |               |-- Transition ->| (Idle→Recording)              |               |            |
 |                          |                  |               |<- Recording -->|                |                |               |            |
 |-- Release Hotkey ------->|                  |               |               |                |                |               |            |
 |                          |-- OnHotkeyUp --->|               |               |                |                |               |            |
 |                          |                  |-- Stop ------>|               |                |                |               |            |
 |                          |                  |               |-- WAV Path -->|                |                |               |            |
 |                          |                  |               |               |-- Transition ->| (Recording→Processing)         |            |
 |                          |                  |               |               |-- Invoke ----->|                |               |            |
 |                          |                  |               |               |                |-- CLI Call --->|               |            |
 |                          |                  |               |               |                |<- JSON Result -|               |            |
 |                          |                  |               |               |<- Text --------|                |               |            |
 |                          |                  |               |               |-- Write ------>|                |               |            |
 |                          |                  |               |               |                                 |-- Write ----->|            |
 |                          |                  |               |               |                                 |<- Path -------|            |
 |                          |                  |               |               |-- Show ------>|                |               |            |
 |                          |                  |               |               |               |                |               |-- Display ->|
 |                          |                  |               |               |-- Transition ->| (Processing→Idle)              |            |
 |<- Ready to Paste --------|                  |               |               |                |                |               |            |
```

**Key Points:**
- State transitions are explicit and logged
- WAV file is created in `tmp/`, passed to Whisper, then deleted
- Clipboard write and history write happen in parallel (or sequential with clipboard priority)
- Flyout appears after both writes complete

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Platform | .NET 8 (C#) |
| UI Framework | WPF (Windows Presentation Foundation) |
| Audio | WASAPI (via NAudio library or P/Invoke) |
| STT | Whisper (CLI subprocess, external tool) |
| Post-Processing | LLM CLI (optional, user-provided) |
| Configuration | TOML (via Tomlyn library) |
| Logging | Serilog or NLog |
| Build/Packaging | .NET CLI, Self-Contained Publish |

---

## Design Principles

### 1. Separation of Concerns
- UI layer does not contain business logic
- Adapters encapsulate external tool invocation
- Core components are testable in isolation

### 2. Fail-Safe Defaults
- Critical path (clipboard write) succeeds even if secondary operations (history write, flyout) fail
- Errors are logged and reported, but don't block user workflow

### 3. Explicit State Management
- State machine enforces valid transitions
- All state changes are logged
- UI reflects current state (tray icon, flyout)

### 4. Testability
- Services use interfaces (mockable for tests)
- CLI adapters can be replaced with test doubles
- BDD scenarios drive acceptance testing

### 5. Observability First
- Every significant event is logged
- Structured logging with context (key-value pairs)
- Performance metrics instrumented (latency, file sizes, durations)

---

## Security Considerations

### Data Privacy
- All processing is local (no cloud services)
- Transcripts stored in user-controlled folder
- Logs do not include transcript content by default

### Trust & Code Signing
- **v0.1:** No code signing (SmartScreen warnings accepted)
- **Future:** Code signing certificate to eliminate warnings

### Permissions
- No admin rights required
- Microphone access requested via standard Windows permissions
- User-writable folders only (no system folders)

---

## Performance Targets

| Metric | Target | Verification |
|--------|--------|--------------|
| E2E Latency (hotkey up → clipboard) | p95 ≤ 2.5s | Iteration 4, 8 |
| Flyout Display Latency | ≤ 0.5s after clipboard | Iteration 4 |
| Wizard Completion Time | < 2 min (default path) | Iteration 5 |
| App Startup Time | < 3s (cold start) | Iteration 1 |
| Memory Footprint | < 150 MB (idle) | Iteration 8 |

---

## Scalability & Limits

**Current Design Limits (v0.1):**
- Single concurrent dictation (no queueing)
- Audio files limited to ~10 minutes (Whisper timeout)
- History: no built-in search UI (use filesystem search)
- Models: user must manage downloads/updates manually

**Future Enhancements:**
- Queue for overlapping dictations
- In-app history search UI
- Auto-update mechanism for app and models

---

## Related Documents

- **Component Details:** `architecture/component-descriptions.md`
- **Runtime Flows:** `architecture/runtime-flows.md`
- **Interface Contracts:** `architecture/interface-contracts.md`
- **Risk Register:** `architecture/risk-register.md`
- **ADRs:** `adr/0000-index.md`

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial architecture overview)
