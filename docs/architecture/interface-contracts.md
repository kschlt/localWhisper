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

## Post-Processor CLI Contract (llama.cpp)

**Version:** v1 (Iteration 7)
**Executable:** `llama-cli.exe` (from llama.cpp project)
**Location:** `<APP_DIR>\llama\llama-cli.exe` (auto-downloaded during wizard)
**Model:** Llama 3.2 3B Instruct (GGUF, Q4_K_M quantization)

---

### Command-Line Invocation

**Full Command:**
```bash
llama-cli.exe \
  -m "<DATA_ROOT>\models\llama-3.2-3b-q4.gguf" \
  -sys "<SYSTEM_PROMPT>" \
  -p "<TRANSCRIPT>" \
  -n 512 \
  --temp 0.0 \
  --top-p 0.25 \
  --repeat-penalty 1.05 \
  -t <CPU_CORES> \
  -ngl 99 \
  --log-disable
```

**Parameters:**

| Parameter | Required | Description | Value |
|-----------|----------|-------------|-------|
| `-m, --model` | Yes | Path to GGUF model file | `<DATA_ROOT>\models\llama-3.2-3b-q4.gguf` |
| `-sys, --system-prompt` | Yes | System instruction for model | Plain Text or Markdown prompt (see below) |
| `-p, --prompt` | Yes | User input (transcript text) | Cleaned transcript (trigger stripped) |
| `-n, --predict` | Yes | Max tokens to generate | `512` (hardcoded) |
| `--temp` | Yes | Temperature (creativity) | `0.0` (deterministic) |
| `--top-p` | Yes | Nucleus sampling threshold | `0.25` (low creativity) |
| `--repeat-penalty` | Yes | Penalize repeated tokens | `1.05` (slight penalty) |
| `-t, --threads` | Yes | CPU threads for computation | `Environment.ProcessorCount` |
| `-ngl, --gpu-layers` | Yes | GPU layers to offload | `99` (all layers if GPU available) |
| `--log-disable` | Yes | Disable logging output | (flag only, no value) |

---

### System Prompts

**Plain Text Mode (Default):**
```
You are a careful transcript formatter. Your task:
- Fix punctuation and capitalization
- Expand common abbreviations (e.g., "asap" → "as soon as possible")
- Improve readability while preserving exact meaning
- DO NOT add new content or change the intent
- Output only the formatted text, no explanations

Keep it concise and natural.
```

**Markdown Mode (Triggered by "markdown mode" in transcript):**
```
You are a formatter that converts speech transcripts into well-structured Markdown.
- Use headings (## for sections, ### for subsections)
- Format lists as - bullet points or 1. numbered lists
- Use **bold** for emphasis, *italic* for terms
- Preserve exact meaning, don't add new content
- Output only the Markdown, no explanations

Keep structure clear and logical.
```

---

### Example Invocations

**Plain Text Mode:**
```bash
llama-cli.exe -m "C:\Data\models\llama-3.2-3b-q4.gguf" \
  -sys "You are a careful transcript formatter. Fix punctuation, capitalization, and expand common abbreviations. DO NOT change the meaning." \
  -p "lets meet at 3pm asap fyi i checked the kb" \
  -n 512 --temp 0.0 --top-p 0.25 --repeat-penalty 1.05 -t 8 -ngl 99 --log-disable
```

**Expected stdout:**
```
llama_print_timings:        load time =     234.56 ms
llama_print_timings:      sample time =      12.34 ms /   50 tokens
llama_print_timings: eval time =     456.78 ms /   30 tokens

Let's meet at 3pm, as soon as possible. For your information, I checked the knowledge base.
```

**Markdown Mode:**
```bash
llama-cli.exe -m "C:\Data\models\llama-3.2-3b-q4.gguf" \
  -sys "You are a formatter that converts speech transcripts into Markdown. Use headings, lists, bold for emphasis." \
  -p "okay I want three sections first overview then requirements then next steps" \
  -n 512 --temp 0.0 --top-p 0.25 --repeat-penalty 1.05 -t 8 -ngl 99 --log-disable
```

**Expected stdout (after cleanup):**
```markdown
## Overview
[content]

## Requirements
[content]

## Next Steps
[content]
```

---

### Output Format

**Raw stdout:**
llama-cli outputs timing information followed by the generated text:

```
llama_print_timings:        load time =     234.56 ms
llama_print_timings:      sample time =      12.34 ms /   50 tokens (  0.25 ms per token)
llama_print_timings: eval time =     456.78 ms /   30 tokens (  15.23 ms per token)

Let's meet at 3pm, as soon as possible.
```

**Parsing Algorithm:**
1. Capture stdout as string
2. Split into lines
3. Filter out lines starting with `llama_` (case insensitive)
4. Join remaining lines with spaces
5. Trim whitespace

**Implementation:**
```csharp
private string CleanLlamaOutput(string rawOutput)
{
    var lines = rawOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

    var textLines = lines
        .Where(line => !line.TrimStart().StartsWith("llama_", StringComparison.OrdinalIgnoreCase))
        .Where(line => !string.IsNullOrWhiteSpace(line));

    return string.Join(" ", textLines).Trim();
}
```

---

### Exit Codes

| Exit Code | Meaning | Adapter Action |
|-----------|---------|----------------|
| `0` | Success | Parse stdout, strip metadata, return text |
| `1` | General error | Parse stderr, log error, fallback to original transcript |
| Other | Undefined | Log stderr, fallback to original transcript |

**stderr Patterns:**

| stderr Contains | Meaning | Action |
|-----------------|---------|--------|
| `failed to load model` | Model file not found or corrupt | Log error, fallback, suggest model re-download |
| `CUDA error` or `out of memory` | GPU OOM | Log warning, **retry with `-ngl 0`** (CPU fallback) |
| `invalid argument` | Bad command-line parameters | Log error (bug in adapter), fallback |

**GPU Fallback Strategy:**
If first invocation fails with CUDA error:
1. Log warning: "GPU error detected, retrying with CPU"
2. Retry same command with `-ngl 0` (force CPU-only)
3. If still fails: Fallback to original transcript

---

### Timeout Enforcement

**Timeout:** 5000ms (5 seconds, configurable in `config.toml`)

**Implementation:**
```csharp
using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_timeoutMs));
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

Process process = new Process { /* StartInfo setup */ };
process.Start();

try
{
    await process.WaitForExitAsync(linkedCts.Token);
}
catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
{
    Log.Warning("LLM process timeout after {Timeout}ms", _timeoutMs);
    process.Kill(entireProcessTree: true); // Kill child processes too
    return originalTranscript; // Fallback
}
```

**If timeout reached:**
- Kill process (including child processes)
- Log warning: "Post-processing timeout after {Timeout}ms"
- Return original transcript (no error dialog)

---

### Error Handling & Fallback

**Adapter Behavior:**

1. **Invoke llama-cli** with transcript
2. **Wait with timeout** (5s default)
3. **Check exit code:**
   - `0` → Parse stdout, strip metadata, return text
   - Non-zero → Parse stderr, log error, **fallback to original transcript**
4. **Timeout reached:**
   - Kill process
   - Log warning
   - **Fallback to original transcript**
5. **Validation:**
   - If cleaned output is empty → Fallback
   - If output > 2x input length → Log warning, **still use output** (user intended verbose formatting)

**Fallback Contract:**
- ✅ Always preserve original STT text
- ✅ No error dialogs (silent fallback, log only)
- ✅ Optional: Show transient flyout "Post-Processing nicht verfügbar (Original-Text verwendet)"

**Consecutive Failure Handling:**
- After **3 consecutive failures**, disable post-processing automatically
- Show dialog: "Post-Processing wurde nach mehreren Fehlern deaktiviert. Bitte prüfen Sie die Logs."
- User can re-enable in Settings

---

### Model File Verification

**Location:** `<DATA_ROOT>\models\llama-3.2-3b-q4.gguf`

**Expected File:**
- **SHA-256:** `6c1a2b41161032677be168d354123594c0e6e67d2b9227c84f296ad037c728ff`
- **Size:** 2,168,659,968 bytes (2.02 GB)
- **Source:** https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF

**Verification (during wizard or settings):**
1. Compute SHA-256 hash of file
2. Compare against expected hash
3. If mismatch: "Modell ungültig oder beschädigt. Bitte erneut herunterladen."
4. If missing: Offer to download during wizard

---

### Distribution: llama-cli.exe Download

**Source:** https://github.com/ggml-org/llama.cpp/releases

**Builds:**

| Build | URL | Size | SHA-256 | Use Case |
|-------|-----|------|---------|----------|
| **CUDA 12.4** | `cudart-llama-bin-win-cuda-12.4-x64.zip` | 373 MB | `8c79a9b2...` | NVIDIA GPU (driver ≥ 531.14) |
| **CPU-only** | `llama-bin-win-x64.zip` | ~100 MB | TBD | No GPU or fallback |

**Download Strategy:**
1. During wizard Step 2b (Post-Processing Setup):
   - Detect NVIDIA GPU via WMI (`Win32_VideoController`)
   - If NVIDIA found → Download CUDA build
   - Else → Download CPU-only build
2. Extract `llama-cli.exe` to `<APP_DIR>\llama\llama-cli.exe`
3. Verify SHA-256 hash
4. Test invocation: `llama-cli.exe --version`
5. If success: ✓ Ready | If fail: Offer manual install

**Fallback if download fails:**
> "llama-cli.exe konnte nicht heruntergeladen werden. Bitte laden Sie es manuell von https://github.com/ggml-org/llama.cpp/releases herunter und platzieren Sie es unter `<APP_DIR>\llama\llama-cli.exe`."

---

### Related Documents

- **ADR-0010:** LLM Post-Processing Architecture
- **ADR-0010 Addendum:** Implementation details (GPU detection, output parsing, etc.)
- **Data Structures:** `specification/data-structures.md` (config schema)
- **Functional Requirements:** FR-022 (Post-Processing)

---

**Version History:**
- **v1 (2025-11-18):** Initial specification (Iteration 7)

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
