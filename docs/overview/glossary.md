# Glossary

**Purpose:** Define key terms and abbreviations used throughout the documentation
**Audience:** All readers (developers, AI agents, stakeholders)
**Status:** Living document

---

## Core Concepts

### Hold-to-Talk (HTT)
The primary interaction model: user presses and holds a hotkey to record audio, then releases the hotkey to stop recording and trigger transcription. This is distinct from "push-to-talk" (single press to toggle) or "voice activation" (no button required).

**Example:**
- User holds `Ctrl+Shift+D` → recording starts
- User speaks → audio captured
- User releases `Ctrl+Shift+D` → recording stops, STT begins

**Related:** FR-010, UC-001

---

### Hotkey
A keyboard combination registered globally in the operating system that triggers the app's recording functionality. Must be:
- Unique (not conflicting with other apps or system shortcuts)
- User-configurable
- Held down during recording (not toggled)

**Examples:**
- `Ctrl+Shift+D`
- `Alt+Space`
- `Ctrl+`

**Related:** FR-010, FR-020, US-001

---

### Wizard
The first-run setup assistant that guides users through initial configuration. Consists of 3 steps:
1. Choose data root directory
2. Verify/download Whisper model (with SHA-256 check)
3. Set hotkey

**Related:** FR-016, UC-002, Iteration 5

---

### Data Root
The base directory where all application data is stored. User-configurable, defaults to `%LOCALAPPDATA%/SpeechClipboardApp/`.

**Structure:**
```
<DATA_ROOT>/
  config/          ← Configuration files (config.toml)
  models/          ← Whisper model files (e.g., ggml-small-de.gguf)
  history/         ← Transcription history (organized by date)
  logs/            ← Application logs (app.log)
  tmp/             ← Temporary audio files (auto-cleaned)
```

**Related:** FR-016, FR-017, ADR-0003, Iteration 5

---

### History
The collection of saved transcriptions, stored as Markdown files in a date-based directory structure. Each file includes:
- Front-matter metadata (timestamp, language, model, duration)
- The transcription text
- A slug-based filename for easy identification

**Path structure:**
```
history/YYYY/YYYY-MM/YYYY-MM-DD/YYYYMMDD_HHMMSS_<slug>.md
```

**Example:**
```
history/2025/2025-09/2025-09-17/20250917_143022_let-me-check-on-that.md
```

**Related:** FR-014, FR-024, ADR-0003

---

### Slug
A normalized, URL-safe identifier derived from the first 6-10 words of a transcription. Used in history filenames for human readability.

**Generation rules:**
- Take first 6-10 words
- Convert to lowercase
- Replace special characters with hyphens (`-`)
- Compress multiple consecutive hyphens into one
- Trim leading/trailing hyphens

**Example:**
- Input: "Let me check on that and get back to you"
- Slug: `let-me-check-on-that-and-get`

**Related:** FR-024, US-036

---

### Post-Processing
An optional processing step that runs after STT to improve formatting. Uses a small local LLM via CLI. Configured to:
- Format lists and paragraphs
- Apply user-defined glossary (e.g., expand abbreviations)
- Fix punctuation
- **Must not change meaning** (constraint)

**Toggleable:** Can be enabled/disabled in settings.

**Related:** FR-022, Iteration 7

---

### Custom Flyout
A lightweight, custom-designed notification window that confirms transcript availability. Preferred over Windows toast notifications for reliability and control.

**Behavior:**
- Appears near system tray
- Shows message: "Transkript im Clipboard"
- Auto-dismisses after ~3 seconds
- Non-intrusive (doesn't steal focus)

**Related:** FR-015, ADR-0005, NFR-004, Iteration 4

---

## Technical Terms

### STT (Speech-to-Text)
The process of converting spoken audio into written text. In this app, performed by Whisper via CLI.

**Related:** FR-012, ADR-0002

---

### Whisper
OpenAI's open-source speech recognition model. Used via `whisper-cli.exe` (CLI subprocess) in this project.

**Models used:**
- `small` (default for German/English)
- Others possible (e.g., `tiny`, `base`, `medium`, `large`)

**Related:** FR-012, FR-017, ADR-0002, Iteration 3

---

### WASAPI (Windows Audio Session API)
Microsoft's low-level audio API for Windows. Used for high-quality microphone input recording.

**Output format:** 16kHz mono, 16-bit WAV

**Related:** FR-011, Iteration 2

---

### CLI Adapter / CLI Subprocess
Design pattern where external tools (Whisper, LLM) are invoked as separate command-line processes rather than embedded via FFI (Foreign Function Interface).

**Benefits:**
- Easier debugging (can test CLI separately)
- Model versioning independence
- Clear error boundaries

**Related:** ADR-0002, FR-012, FR-022

---

### Front-Matter
Metadata block at the beginning of a Markdown file, enclosed in `---` delimiters. Uses YAML format.

**Example:**
```yaml
---
created: 2025-09-17T14:30:22Z
lang: de
stt_model: whisper-small
duration_sec: 4.8
---
```

**Related:** FR-014, Iteration 4

---

### SHA-256
Cryptographic hash algorithm used to verify model file integrity. Ensures downloaded or user-provided models are correct and untampered.

**Related:** FR-017, US-041, Iteration 5

---

### Portable (Application)
Software that runs without installation or admin rights. Characteristics:
- Self-contained executable (single EXE)
- Stores data in user-writable locations (no registry, no Program Files)
- Can be copied to USB drive or new machine and run immediately

**Related:** NFR-002, ADR-0001, UC-002

---

### Tray App / System Tray
Windows desktop application that runs minimized in the system tray (notification area) rather than as a window. Accessed via tray icon right-click menu.

**Related:** FR-015, ADR-0001, Iteration 1

---

## Requirements & Traceability Terms

### UC (Use Case)
High-level user-facing scenario describing what a user wants to accomplish.

**Format:** `UC-###` (e.g., `UC-001`)

**Examples:**
- UC-001: Quick dictation
- UC-002: First-time installation

**Related:** `specification/use-cases.md`

---

### FR (Functional Requirement)
Specific, testable capability the system must provide.

**Format:** `FR-###` (e.g., `FR-010`)

**Structure:**
- Description (what the system does)
- Fit Criteria (how to verify it works)

**Related:** `specification/functional-requirements.md`

---

### NFR (Non-Functional Requirement)
Quality attribute or constraint (performance, usability, security, etc.).

**Format:** `NFR-###` (e.g., `NFR-001`)

**Structure:**
- Stimulus (trigger condition)
- Environment (context)
- Response (system behavior)
- Measure (quantifiable target)

**Related:** `specification/non-functional-requirements.md`

---

### ADR (Architecture Decision Record)
Document capturing an important architectural or design decision, including context, options considered, and rationale.

**Format:** `ADR-####` (e.g., `ADR-0001`)

**Structure:**
- Status (Proposed, Accepted, Deprecated, Superseded)
- Context (why we need to decide)
- Options (alternatives considered)
- Decision (what we chose)
- Consequences (trade-offs)

**Related:** `adr/` directory

---

### US (User Story)
Implementation-level story describing a small, testable increment of work.

**Format:** `US-###` (e.g., `US-001`)

**Structure:**
- As a [role], I want [feature] so that [benefit]
- Acceptance Criteria (AC)

**Related:** `iterations/` files

---

### AC (Acceptance Criteria)
Testable conditions that must be met for a user story to be considered complete.

**Format:** Listed under "AC:" in user stories

**Example:**
```
AC:
- Given app is running, When hotkey pressed, Then state changes to Recording
- Log shows "State transition: Idle -> Recording"
```

---

### DoD (Definition of Done)
Checklist of requirements for an iteration to be considered complete.

**Typical items:**
- [ ] All user stories implemented
- [ ] All tests passing
- [ ] Logging added
- [ ] Documentation updated
- [ ] Metrics measured (if applicable)

**Related:** Each iteration file has a DoD section

---

### BDD (Behavior-Driven Development)
Testing approach using Gherkin syntax (Given/When/Then) to define executable specifications.

**Example:**
```gherkin
Scenario: Hotkey toggles state
  Given die App läuft
  When ich den Hotkey gedrückt halte
  Then der State ist Recording
```

**Related:** `testing/bdd-feature-seeds.md`

---

## Project-Specific Abbreviations

| Abbreviation | Full Term | Meaning |
|--------------|-----------|---------|
| HTT | Hold-to-Talk | Core interaction pattern (hold hotkey to record) |
| STT | Speech-to-Text | Audio transcription process |
| LLM | Large Language Model | AI model used for post-processing |
| CLI | Command-Line Interface | How external tools are invoked |
| FFI | Foreign Function Interface | Alternative to CLI (not used in this project) |
| TOML | Tom's Obvious Minimal Language | Config file format |
| WASAPI | Windows Audio Session API | Audio recording API |
| SHA | Secure Hash Algorithm | Cryptographic hash (specifically SHA-256) |
| p95 | 95th percentile | Performance metric (95% of operations faster than X) |
| DoD | Definition of Done | Completion checklist |
| AC | Acceptance Criteria | Story completion conditions |
| UC | Use Case | High-level user scenario |
| FR | Functional Requirement | System capability requirement |
| NFR | Non-Functional Requirement | Quality attribute requirement |
| ADR | Architecture Decision Record | Design decision documentation |
| US | User Story | Implementation-level work item |
| BDD | Behavior-Driven Development | Testing methodology |
| TDD | Test-Driven Development | Write tests before code |
| MVP | Minimum Viable Product | Smallest releasable version |
| v0.1 | Version 0.1 | Initial release version |

---

## File Formats & Extensions

| Extension | Purpose | Example |
|-----------|---------|---------|
| `.md` | Markdown file (history, docs) | `20250917_143022_let-me-check.md` |
| `.txt` | Plain text (alternative history format) | `20250917_143022_let-me-check.txt` |
| `.toml` | Configuration file | `config.toml` |
| `.wav` | Audio file (temporary) | `rec_20250917_143022.wav` |
| `.log` | Log file | `app.log` |
| `.gguf` | Whisper model file | `ggml-small-de.gguf` |
| `.lnk` | Windows shortcut (autostart) | `SpeechClipboardApp.lnk` |

---

## State Machine States

| State | Meaning | Next States |
|-------|---------|-------------|
| `Idle` | App is waiting for hotkey | → `Recording` (on hotkey down) |
| `Recording` | Audio is being captured | → `Processing` (on hotkey up) |
| `Processing` | STT/post-processing running | → `Idle` (on completion or error) |

**Related:** FR-010, US-001, Iteration 1

---

## Maintenance Instructions

**When adding a new term:**
1. Add definition in appropriate section (Core Concepts, Technical Terms, etc.)
2. Include format, examples, and related IDs
3. Update abbreviation table if applicable
4. Link to relevant documentation files

**When a term changes meaning:**
1. Update definition
2. Note change date and reason
3. Check if any other docs reference the old meaning

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial glossary)
