# Interface Contracts

**Purpose:** Define external interfaces (CLI tools, file formats, APIs)
**Audience:** Developers implementing adapters and integrations
**Status:** Normative (implementations must conform)
**Last Updated:** 2025-09-17

---

## Whisper CLI Contract (v1)

### Command-Line Invocation

**Executable:** `whisper-cli.exe` (or `whisper` on Linux/Mac if cross-platform later)
**Location:** Configured in `config.toml` (`paths.whisper_cli_path`)

**Command Format:**
```bash
whisper-cli.exe \
  --model <model_path> \
  --language <lang_code> \
  --output-format json \
  --output-file <output_path> \
  <input_wav_file>
```

**Parameters:**

| Parameter | Required | Description | Example |
|-----------|----------|-------------|---------|
| `--model` | Yes | Path to Whisper model file (.gguf) | `C:\...\models\ggml-small-de.gguf` |
| `--language` | Yes | Language code (ISO 639-1) or "auto" | `de`, `en`, `auto` |
| `--output-format` | Yes | Must be `json` | `json` |
| `--output-file` | Yes | Path for JSON output | `C:\...\tmp\stt_result.json` |
| `<input_wav_file>` | Yes | Path to input WAV file | `C:\...\tmp\rec_20250917_143022.wav` |

**Example Command:**
```bash
whisper-cli.exe --model "C:\Data\models\ggml-small-de.gguf" --language de --output-format json --output-file "C:\Data\tmp\stt_result.json" "C:\Data\tmp\rec_20250917_143022.wav"
```

---

### JSON Output Format

**File:** `stt_result.json` (UTF-8 encoded)

**Schema:**

```json
{
  "text": "<full_transcription>",
  "language": "<detected_or_specified_lang>",
  "duration_sec": <float>,
  "segments": [
    {
      "start": <float>,
      "end": <float>,
      "text": "<segment_text>"
    }
  ],
  "meta": {
    "model": "<model_name>",
    "processing_time_sec": <float>,
    "confidence": <float>
  }
}
```

**Field Definitions:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | String | **Yes** | Full transcription (all segments concatenated) |
| `language` | String | **Yes** | Detected or specified language (ISO 639-1) |
| `duration_sec` | Float | **Yes** | Audio duration in seconds |
| `segments` | Array | No | Word/sentence-level segments with timestamps |
| `segments[].start` | Float | If segments | Segment start time (seconds from beginning) |
| `segments[].end` | Float | If segments | Segment end time (seconds) |
| `segments[].text` | String | If segments | Segment transcription |
| `meta` | Object | No | Additional metadata |
| `meta.model` | String | No | Model identifier (e.g., "whisper-small") |
| `meta.processing_time_sec` | Float | No | STT processing duration |
| `meta.confidence` | Float | No | Average confidence score (0.0-1.0) |

**Example Output:**

```json
{
  "text": "Let me check on that and get back to you tomorrow morning.",
  "language": "en",
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

---

### Exit Codes

| Exit Code | Meaning | Adapter Action |
|-----------|---------|----------------|
| `0` | Success | Parse JSON, return transcript |
| `1` | General error | Show generic error dialog, log stderr |
| `2` | Model file not found or invalid | Show "Model error" dialog (FR-021), log path |
| `3` | Audio device error | Show "Device error" dialog (FR-021) |
| `4` | Timeout | Show "Timeout" dialog (FR-021) |
| `5` | Invalid input file | Show "Invalid audio file" dialog, log path |

**Adapter Behavior:**
- Capture stdout and stderr
- Log command, exit code, stdout, stderr
- If exit code ≠ 0, parse stderr for additional context
- Timeout after 60 seconds (configurable)

---

### Error Handling

**If JSON is malformed or missing `text`:**
- Log error with file path and content snippet
- Show dialog: "Transkription fehlgeschlagen. Bitte prüfen Sie die Logs."
- Do not crash

**If `text` is empty (`""`):**
- Treat as valid but silent audio
- Option 1: Skip clipboard/history write, show flyout "Keine Sprache erkannt"
- Option 2: Write "[empty]" to clipboard/history
- (Decision: Option 1 preferred)

---

## Post-Processor CLI Contract (Optional)

### Command-Line Invocation

**Executable:** User-configured (e.g., `llama-cli.exe`, `ollama.exe`)
**Location:** Configured in `config.toml` (`postprocessing.llm_cli_path`)

**Command Format:**
```bash
<llm_cli> --prompt <prompt_file> < <input_text_file>
```

**Input:** Transcript text via **stdin** (UTF-8)
**Output:** Formatted text via **stdout** (UTF-8)

**Prompt (embedded or file):**
```
Format the following text for readability:
- Fix punctuation and capitalization
- Format lists as bullet points
- Apply glossary substitutions (if provided)
- DO NOT change the meaning

Text:
```

**Example Command:**
```bash
echo "lets meet at 3pm asap fyi i checked the kb" | llama-cli.exe --prompt "fix punctuation, expand abbreviations: asap=as soon as possible, fyi=for your information, kb=knowledge base"
```

**Expected Output:**
```
Let's meet at 3pm, as soon as possible. For your information, I checked the knowledge base.
```

---

### Adapter Behavior

1. **Prepare input:** Write transcript text to stdin
2. **Invoke CLI:** Launch process with prompt
3. **Capture output:** Read stdout
4. **Timeout:** 10 seconds (shorter than STT timeout)
5. **Fallback:** If exit code ≠ 0 or timeout, use original STT text
6. **Validation:** Ensure output length is reasonable (< 2x input length; prevent runaway generation)

**Error Handling:**
- Log error, use original text, show warning flyout: "Post-Processing fehlgeschlagen (Fallback: Original-Text)"
- Do not block clipboard write

---

## Windows API Contracts

### Global Hotkey Registration (Win32 API)

**Function:** `RegisterHotKey`

**P/Invoke Signature:**
```csharp
[DllImport("user32.dll", SetLastError = true)]
public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
```

**Parameters:**
- `hWnd`: Window handle (use app's main window handle)
- `id`: Unique hotkey ID (e.g., 1)
- `fsModifiers`: Modifier flags (MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_ALT = 0x0001, MOD_WIN = 0x0008)
- `vk`: Virtual key code (e.g., VK_D = 0x44)

**Return Value:**
- `true`: Success
- `false`: Failure (check `GetLastError()` for error code, often `ERROR_HOTKEY_ALREADY_REGISTERED = 1409`)

**Event Handling:**
- Listen for `WM_HOTKEY` message (0x0312) in window procedure
- Parse `lParam` to determine key down vs. key up (bit 31: 0 = down, 1 = up)

---

### Clipboard Write (Windows API)

**Function:** `Clipboard.SetText()` (.NET wrapper)

**Behavior:**
- Opens clipboard, empties it, writes text as Unicode, closes clipboard
- Thread-safe (STA thread required for WPF)

**Error Handling:**
- Catch `ExternalException` if clipboard is locked by another app
- Retry once after 100ms delay
- If still fails, log and show error dialog

---

### WASAPI Audio Recording

**Library:** NAudio (recommended) or P/Invoke to `Winmm.dll`

**Parameters:**
- Device: Default microphone (`MMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console)`)
- Format: 16 kHz, Mono, 16-bit PCM
- Buffer: 100ms latency

**Output:** WAV file via `WaveFileWriter`

**Error Handling:**
- Catch `NAudio.CoreAudioApi.COMException` if microphone is unavailable
- Show dialog: "Mikrofon nicht verfügbar"

---

## File Format Contracts

### History File (Markdown)

**See:** `specification/data-structures.md` (comprehensive definition)

**Key Points:**
- UTF-8 with BOM
- YAML front-matter between `---` delimiters
- Required fields: `created`, `lang`, `stt_model`, `duration_sec`, `post_processed`

---

### Configuration File (TOML)

**See:** `specification/data-structures.md`

**Key Points:**
- TOML format
- UTF-8 encoding
- Supports comments (`#`)
- Validation on load (missing keys → defaults; invalid values → error)

---

## Version Compatibility

**Current Version:** v1 (for all contracts)

**Future Changes:**
- Whisper CLI contract: may add `--max-tokens`, `--temperature` parameters (backward-compatible)
- JSON output: may add fields (backward-compatible; parsers ignore unknown fields)
- Breaking changes: will increment contract version (e.g., v2) and support both

**Adapter Versioning:**
- Adapter code should check CLI version (if available via `--version` flag)
- Log version mismatch warnings but attempt to proceed

---

## Testing Contracts

### CLI Adapter Test Doubles

**For unit/integration tests:**
- Create mock CLI executables that output predefined JSON
- Example: `mock-whisper.exe` that reads input path, outputs fixed JSON after 100ms delay
- Place in test fixtures, configure adapter to use mock path

**Test Scenarios:**
- Success (exit 0, valid JSON)
- Model error (exit 2, stderr: "Model not found")
- Timeout (hang for 120s, expect adapter to kill after 60s)
- Malformed JSON (exit 0, but JSON is invalid)
- Empty transcript (exit 0, `text` field is `""`)

---

## Related Documents

- **Data Structures:** `specification/data-structures.md`
- **Architecture Overview:** `architecture/architecture-overview.md`
- **ADR-0002:** CLI Subprocesses Decision
- **Functional Requirements:** `specification/functional-requirements.md` (FR-012, FR-022)

---

**Last updated:** 2025-09-17
**Version:** v1 (Initial contract definitions)
