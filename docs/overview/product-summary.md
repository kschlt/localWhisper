# Product Summary

**Product Name:** Dictate-to-Clipboard
**Version:** v0.1 (Planned)
**Platform:** Windows Desktop (Portable)
**Status:** Documentation Complete, Implementation Pending

---

## One-Sentence Description

A portable Windows desktop app that transcribes voice dictation offline and places the result directly into your clipboard and history—activated by holding a hotkey.

---

## Core Value Proposition

**Problem:**
Knowledge workers need to capture ideas, respond in chats, or transcribe thoughts quickly during meetings—but typing is slow and switching to dictation tools is cumbersome.

**Solution:**
**Hold hotkey → speak → release → transcript is in clipboard** (ready to paste anywhere) and saved to a searchable history.

**Key Benefits:**
- **Zero friction:** No app switching, no UI interaction, just hold-talk-release-paste
- **Offline & private:** Runs entirely on your machine using Whisper STT
- **Portable:** No admin rights needed, runs from a single EXE
- **Searchable history:** Every dictation is automatically saved with timestamps

---

## Primary Users

**Persona: Knowledge Worker**
- Works on company or personal laptop (often without admin rights)
- Frequently in meetings, chats, or brainstorming sessions
- Values speed and privacy
- Needs offline transcription (no cloud dependencies)

**Use Cases:**
1. Capture meeting notes quickly
2. Dictate responses in chat apps
3. Transcribe ideas while thinking out loud
4. Speed up documentation writing

---

## Key Features (v0.1)

### Core Workflow
- **Hold-to-Talk (HTT):** Press and hold a customizable hotkey to record; release to transcribe
- **Instant Clipboard:** Transcript appears in system clipboard immediately after release
- **Auto-History:** Every dictation saved as Markdown file in date-organized folders
- **Custom Flyout:** Visual confirmation that transcript is ready to paste

### Setup & Configuration
- **First-Run Wizard:** 3-step setup (data location, model verification, hotkey selection)
- **Model Verification:** SHA-256 hash check ensures model integrity
- **Settings Panel:** Change hotkey, data location, language, file format
- **Portable by Design:** Self-contained executable, no registry/admin needed

### Advanced Features
- **Optional Post-Processing:** Format lists, apply glossary, fix punctuation (via local LLM)
- **Robust Error Handling:** Clear dialogs for microphone issues, model problems, or conflicts
- **Full Observability:** Structured logging for debugging and performance tracking
- **Reset/Uninstall:** Clean removal of data and configuration

---

## What's Out of Scope (v0.1)

- ❌ Auto-insertion at cursor position (clipboard-only for now)
- ❌ Auto-update mechanism
- ❌ GPU optimization (CPU-only initially)
- ❌ Code signing (SmartScreen warnings accepted for v0.1)
- ❌ Toggle hotkey (only hold-to-talk mode)
- ❌ Cloud sync or team features

---

## Technical Highlights

**Architecture:**
- Platform: .NET 8 + WPF (Windows Presentation Foundation)
- STT Engine: Whisper (via CLI subprocess)
- Audio: WASAPI (16kHz mono WAV)
- Storage: User-configurable data root (default: `%LOCALAPPDATA%`)

**Key Design Decisions (ADRs):**
- CLI subprocesses for STT/LLM (not FFI) → easier debugging and model swapping
- Custom flyout (not Windows toast) → more reliable, customizable
- Date-based history structure → easy backup and archival

**Performance Targets:**
- **p95 latency:** ≤ 2.5s from hotkey release to clipboard ready
- **Flyout delay:** ≤ 0.5s after clipboard write
- **Wizard completion:** < 2 minutes

---

## Implementation Status

| Iteration | Focus | Status |
|-----------|-------|--------|
| 1 | Hotkey & app skeleton | Planned |
| 2 | Audio recording | Planned |
| 3 | STT with Whisper | Planned |
| 4 | Clipboard + History + Flyout | Planned |
| 5 | First-run wizard + Model check | Planned |
| 6 | Settings UI | Planned |
| 7 | Optional post-processing | Planned |
| 8 | Stabilization + Reset | Planned |

**Expected Delivery:** 8 iterations (~40-60 hours total)

---

## Data & Privacy

**Privacy-First:**
- All processing happens offline on your machine
- No cloud services, no telemetry
- Only network access: optional model download during setup

**Data Storage:**
```
<DATA_ROOT>/
  config/          ← Settings (TOML)
  models/          ← Whisper model files
  history/         ← Transcripts organized by date
  logs/            ← Application logs
  tmp/             ← Temporary audio files (auto-cleaned)
```

**Backup & Portability:**
- Copy `<DATA_ROOT>` to backup all your dictations
- Move EXE + data root to new machine → works immediately

---

## User Workflow Example

```
1. User is in a chat app, wants to reply quickly
2. Holds down hotkey (e.g., Ctrl+Shift+D)
3. Speaks: "Let me check on that and get back to you tomorrow morning"
4. Releases hotkey
5. ~2 seconds later, custom flyout appears: "Transcript in clipboard"
6. Presses Ctrl+V in chat app
7. Message appears: "Let me check on that and get back to you tomorrow morning."
8. Transcript also saved to history as:
   history/2025/2025-09/2025-09-17/20250917_143022_let-me-check-on-that.md
```

---

## Success Criteria

**MVP is successful if:**
- ✓ Hotkey-driven dictation works reliably on Windows 10/11
- ✓ p95 latency ≤ 2.5s (measured in Iteration 4 & 8)
- ✓ Portable installation works without admin rights
- ✓ Error scenarios handled gracefully (no crashes)
- ✓ History is searchable and well-organized
- ✓ Wizard completes in < 2 minutes

---

## Future Enhancements (Post-v0.1)

- **Auto-insert at cursor:** Simulate paste at active window's text cursor
- **GPU acceleration:** Faster STT with CUDA/DirectML
- **Code signing:** Eliminate SmartScreen warnings
- **Auto-update:** In-app update mechanism
- **Multi-language models:** Easier switching between languages
- **History search UI:** In-app search and playback of past dictations

---

## Related Documents

- **Use Cases:** `specification/use-cases.md`
- **Requirements:** `specification/functional-requirements.md`, `specification/non-functional-requirements.md`
- **Architecture:** `architecture/architecture-overview.md`
- **ADRs:** `adr/0000-index.md`
- **Iterations:** `iterations/iteration-plan.md`

---

**Last updated:** 2025-09-17
**Owner:** Solo developer (BA/Architect/Developer)
**Feedback:** Document issues or questions in project issue tracker
