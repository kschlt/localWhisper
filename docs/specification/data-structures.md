# Data Structures & File Formats

**Purpose:** Define all data file formats, directory structures, and configuration schemas
**Audience:** Developers, AI agents
**Status:** Normative (implementation must match these specifications exactly)
**Last Updated:** 2025-09-17

---

## Directory Structure

### Data Root Layout

**Default location:** `%LOCALAPPDATA%\SpeechClipboardApp\`
**User-configurable:** Yes (selected in wizard or settings)

```
<DATA_ROOT>/
  ├── config/
  │   ├── config.toml           # Main configuration file
  │   └── glossary.txt          # Optional: user-defined glossary for post-processing
  │
  ├── models/
  │   └── ggml-small-de.gguf    # Whisper model file (or other variants)
  │
  ├── history/
  │   └── YYYY/                 # Year (e.g., 2025)
  │       └── YYYY-MM/          # Year-Month (e.g., 2025-09)
  │           └── YYYY-MM-DD/   # Year-Month-Day (e.g., 2025-09-17)
  │               ├── YYYYMMDD_HHMMSS_<slug>.md
  │               └── YYYYMMDD_HHMMSS_<slug>.md
  │
  ├── logs/
  │   └── app.log               # Application log (rotates at 10 MB)
  │
  └── tmp/
      └── rec_20250917_143022.wav  # Temporary audio files (cleaned after processing)
```

**Notes:**
- All paths are relative to `<DATA_ROOT>`
- Subfolders are created automatically if missing
- `tmp/` is cleaned periodically (delete files older than 24 hours or after successful processing)

---

## Configuration File

### `config/config.toml`

**Format:** TOML (Tom's Obvious Minimal Language)
**Encoding:** UTF-8

**Schema:**

```toml
[app]
version = "0.1.0"
language = "de"  # "de" | "en" (UI language)

[hotkey]
modifiers = ["Ctrl", "Shift"]  # Array: "Ctrl", "Alt", "Shift", "Win"
key = "D"                      # Single key: "A"-"Z", "0"-"9", "F1"-"F12", "Space", etc.

[paths]
data_root = "C:\\Users\\JohnDoe\\AppData\\Local\\SpeechClipboardApp"  # Absolute path (escaped backslashes)
model_path = "${data_root}\\models\\ggml-small-de.gguf"  # Supports ${data_root} variable
whisper_cli_path = "C:\\Tools\\whisper\\whisper-cli.exe"  # Path to CLI tool

[stt]
model_name = "small"
language = "de"  # "de" | "en" | "auto" (auto-detect)
model_hash_sha256 = "a1b2c3d4..."  # SHA-256 hash of model file for verification

[history]
file_format = "md"  # "md" | "txt"
include_frontmatter = true  # Only applies to .md files

[postprocessing]
enabled = false  # true | false
llm_cli_path = "C:\\Program Files\\LocalWhisper\\llama\\llama-cli.exe"  # Path to llama-cli.exe (auto-set during wizard)
llm_model_path = "${data_root}\\models\\llama-3.2-3b-q4.gguf"  # Path to GGUF model file
llm_model_hash_sha256 = "6c1a2b41161032677be168d354123594c0e6e67d2b9227c84f296ad037c728ff"  # Expected SHA-256 hash for verification
timeout_ms = 5000  # Timeout for LLM invocation in milliseconds (5 seconds default)
use_gpu = true  # Auto-detected during wizard; set to false to force CPU-only mode

[autostart]
enabled = false  # REMOVED for v0.1; kept in schema for future compatibility

[logging]
level = "INFO"  # "DEBUG" | "INFO" | "WARN" | "ERROR"
max_file_size_mb = 10
```

**Validation Rules:**
- `hotkey.modifiers` must contain at least one modifier (no single-key hotkeys)
- `paths.data_root` must be absolute path
- `stt.model_hash_sha256` must be 64-character hex string
- `logging.level` must be one of the specified values

**Default Values (if key is missing):**
- `app.language` → `"de"`
- `hotkey.modifiers` → `["Ctrl", "Shift"]`
- `hotkey.key` → `"D"`
- `stt.language` → `"de"`
- `history.file_format` → `"md"`
- `history.include_frontmatter` → `true`
- `postprocessing.enabled` → `false`
- `postprocessing.timeout_ms` → `5000`
- `postprocessing.use_gpu` → `true` (auto-detected)
- `logging.level` → `"INFO"`
- `logging.max_file_size_mb` → `10`

---

## History Files

### Markdown Format (`.md`)

**Filename Pattern:** `YYYYMMDD_HHMMSS_<slug>.md`
**Encoding:** UTF-8 with BOM (for Windows compatibility)

**Structure:**

```markdown
---
created: 2025-09-17T14:30:22Z
lang: de
stt_model: whisper-small
duration_sec: 4.8
post_processed: false
---
# Diktat – 17.09.2025 14:30

Let me check on that and get back to you tomorrow morning.
```

**Front-matter Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `created` | ISO-8601 DateTime | Yes | UTC timestamp of transcription completion |
| `lang` | String | Yes | Detected or configured language ("de", "en", etc.) |
| `stt_model` | String | Yes | Model identifier (e.g., "whisper-small", "whisper-large") |
| `duration_sec` | Float | Yes | Audio duration in seconds |
| `post_processed` | Boolean | Yes | Whether post-processing was applied |
| `confidence` | Float | Optional | Average confidence score (0.0-1.0) if available from STT |

**Body:**
- Heading: `# Diktat – {dd.MM.yyyy HH:mm}` (localized date format)
- Transcript text (one or more paragraphs)

---

### Plain Text Format (`.txt`)

**Filename Pattern:** `YYYYMMDD_HHMMSS_<slug>.txt`
**Encoding:** UTF-8 with BOM

**Structure:**

```
[Diktat – 17.09.2025 14:30]
Sprache: de | Modell: whisper-small | Dauer: 4.8s

Let me check on that and get back to you tomorrow morning.
```

**Header Lines:**
- Line 1: `[Diktat – {dd.MM.yyyy HH:mm}]`
- Line 2: `Sprache: {lang} | Modell: {model} | Dauer: {duration}s`
- Line 3: (empty)
- Line 4+: Transcript text

---

### Slug Generation Rules

**Input:** First 6-10 words of transcript (or entire transcript if shorter)

**Algorithm:**
1. Extract first 6-10 words (split on whitespace)
2. Join into single string
3. Convert to lowercase
4. Replace all non-alphanumeric characters (except spaces) with `-`
5. Replace spaces with `-`
6. Replace multiple consecutive `-` with single `-`
7. Trim leading and trailing `-`
8. Truncate to 50 characters maximum
9. If empty result: use `"transcript"`
10. If duplicate filename exists: append `_2`, `_3`, etc.

**Examples:**

| Transcript | Slug |
|------------|------|
| "Let me check on that and get back to you tomorrow" | `let-me-check-on-that-and-get` |
| "Meeting at 3:00 PM" | `meeting-at-3-00-pm` |
| "Re: Project Alpha — status update" | `re-project-alpha-status-update` |
| "" (empty) | `transcript` |
| "Äpfel, Öl & Übung" | `apfel-ol-ubung` (or keep umlauts if desired) |

---

## Audio Files (Temporary)

### WAV Format

**Location:** `<DATA_ROOT>/tmp/`
**Filename Pattern:** `rec_{yyyyMMdd_HHmmssfff}.wav`
**Format Specification:**
- **Sample Rate:** 16,000 Hz (16 kHz)
- **Channels:** 1 (mono)
- **Bit Depth:** 16-bit PCM
- **Byte Order:** Little-endian (standard WAV)

**Lifecycle:**
- Created when hotkey is pressed (recording starts)
- Passed to STT CLI after recording stops
- Deleted after successful STT processing OR after 24 hours (whichever comes first)
- If STT fails, file is preserved for debugging (logged path)

**Example File:**
```
Filename: rec_20250917_143022456.wav
Size: ~160 KB (for 5-second recording)
Path: C:\Users\JohnDoe\AppData\Local\SpeechClipboardApp\tmp\rec_20250917_143022456.wav
```

---

## STT Output (JSON)

### `stt_result.json`

**Location:** `<DATA_ROOT>/tmp/` (temporary, same lifecycle as WAV)
**Format:** JSON (UTF-8)

**Schema (Whisper CLI v1 Contract):**

```json
{
  "text": "Let me check on that and get back to you tomorrow morning.",
  "language": "de",
  "duration_sec": 4.8,
  "segments": [
    {
      "start": 0.0,
      "end": 2.3,
      "text": "Let me check on that"
    },
    {
      "start": 2.3,
      "end": 4.8,
      "text": "and get back to you tomorrow morning."
    }
  ],
  "meta": {
    "model": "whisper-small",
    "processing_time_sec": 1.2,
    "confidence": 0.86
  }
}
```

**Field Descriptions:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | String | Yes | Full transcription (all segments joined) |
| `language` | String | Yes | Detected language code (ISO 639-1) |
| `duration_sec` | Float | Yes | Audio duration in seconds |
| `segments` | Array | No | Word-level or sentence-level segments with timestamps |
| `segments[].start` | Float | If segments | Segment start time (seconds) |
| `segments[].end` | Float | If segments | Segment end time (seconds) |
| `segments[].text` | String | If segments | Segment text |
| `meta` | Object | No | Additional metadata |
| `meta.model` | String | No | Model identifier |
| `meta.processing_time_sec` | Float | No | STT processing duration |
| `meta.confidence` | Float | No | Average confidence (0.0-1.0) |

**Error Handling:**
- If `text` is missing or empty → treat as transcription failure
- If JSON is malformed → log error, show dialog, do not crash

---

## Log File

### `logs/app.log`

**Format:** Structured plain text (semi-JSON for structured data)
**Encoding:** UTF-8

**Line Format:**
```
[YYYY-MM-DD HH:MM:SS.fff] [LEVEL] [Component] Message {Key1=Value1, Key2=Value2, ...}
```

**Example Entries:**

```
[2025-09-17 14:30:15.123] [INFO] [App] Application started {Version=0.1.0, OS=Windows 10.0.19045, DataRoot=C:\Users\JohnDoe\AppData\Local\SpeechClipboardApp}
[2025-09-17 14:30:15.234] [INFO] [HotkeyManager] Hotkey registered {Modifiers=Ctrl+Shift, Key=D}
[2025-09-17 14:30:22.456] [INFO] [StateMachine] State transition {From=Idle, To=Recording}
[2025-09-17 14:30:27.789] [INFO] [AudioRecorder] Recording stopped {Duration=5.3s, FilePath=C:\...\tmp\rec_20250917_143022456.wav, FileSize=169KB}
[2025-09-17 14:30:28.012] [INFO] [STTClient] STT invocation started {Command=whisper-cli.exe --model ... --language de rec_20250917_143022456.wav}
[2025-09-17 14:30:29.234] [INFO] [STTClient] STT completed {ExitCode=0, Duration=1.2s, TranscriptLength=62}
[2025-09-17 14:30:29.345] [INFO] [ClipboardService] Clipboard write succeeded {TextLength=62}
[2025-09-17 14:30:29.456] [INFO] [HistoryWriter] History file created {Path=C:\...\history\2025\2025-09\2025-09-17\20250917_143022_let-me-check-on-that.md}
[2025-09-17 14:30:29.567] [INFO] [FlyoutNotification] Flyout shown {Message=Transkript im Clipboard}
[2025-09-17 14:30:29.678] [INFO] [StateMachine] State transition {From=Processing, To=Idle}
```

**Log Levels:**
- **DEBUG:** Detailed diagnostic information (verbose, not shown by default)
- **INFO:** General informational messages (state changes, successful operations)
- **WARN:** Warning conditions (non-critical issues, fallbacks)
- **ERROR:** Error conditions (operation failures, exceptions)

**Rotation:**
- When `app.log` reaches 10 MB, rename to `app.log.1` and start new `app.log`
- Keep up to 5 archived logs (`app.log.1` through `app.log.5`)
- Oldest archives are deleted

---

## Glossary File (Optional)

### `config/glossary.txt`

**Format:** Plain text (UTF-8), one entry per line
**Purpose:** User-defined abbreviations for post-processing expansion

**Syntax:**
```
<abbreviation> = <expansion>
```

**Example:**
```
ASAP = as soon as possible
FYI = for your information
CEO = Chief Executive Officer
KB = knowledge base
```

**Usage:**
- Post-processor (if enabled) reads this file
- Replaces abbreviations in transcript with expansions
- Case-insensitive matching recommended

---

## Data Migration & Versioning

### Config Version Handling

**Current Version:** v0.1

**Future Considerations:**
- If config schema changes in v0.2, include migration logic:
  - Read old `config.toml`
  - Add missing keys with defaults
  - Write updated `config.toml`
  - Log migration event

**History Format Stability:**
- Front-matter fields may be added in future versions (backward-compatible)
- Existing history files remain readable by newer versions
- No automatic migration of old history files

---

## Related Documents

- **Functional Requirements:** `specification/functional-requirements.md` (FR-014, FR-024)
- **Architecture:** `architecture/architecture-overview.md`
- **ADR-0003:** Storage layout decision
- **Interface Contracts:** `architecture/interface-contracts.md` (CLI JSON format)

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial data structures)
