# Iteration 3: STT Integration with Whisper CLI

**Duration:** 6-8 hours
**Status:** In Progress
**Dependencies:** Iteration 2 (Audio recording with WAV files)
**Last Updated:** 2025-11-17

---

## Overview

**Goal:** Integrate Whisper CLI for speech-to-text transcription, processing recorded WAV files and producing text transcripts.

**Value:** After this iteration, the app can convert recorded audio into text, completing the core dictation pipeline (hold hotkey → record → transcribe).

**Scope:**
- ✅ Whisper CLI subprocess invocation
- ✅ JSON output parsing
- ✅ Exit code error mapping
- ✅ Timeout handling (60 seconds)
- ⚠️ Model hash verification (placeholder implementation)
- ⚠️ Clipboard/history integration (deferred to Iter-4)

---

## User Stories

### US-020: STT via Whisper CLI

**As a** developer
**I want** to invoke Whisper CLI as a subprocess
**So that** WAV files can be transcribed to text

**Acceptance Criteria:**

**AC-1:** Whisper CLI is invoked with correct arguments
- ✓ Model path from configuration
- ✓ Language parameter (default: "de" for German)
- ✓ Output format: JSON
- ✓ Input file: WAV file from recording
- ✓ Output file: tmp/stt_result_{timestamp}.json

**AC-2:** Process execution with timeout
- ✓ 60-second timeout enforced
- ✓ Process killed if timeout exceeded
- ✓ STTTimeoutException thrown on timeout
- ✓ Resources cleaned up properly

**AC-3:** Standard output and error captured
- ✓ Stdout captured for logging
- ✓ Stderr captured for error diagnostics
- ✓ Exit code captured for error mapping

**AC-4:** CLI path configuration
- ✓ Whisper CLI path read from config.toml
- ✓ Default path validation on startup
- ✓ Error dialog if CLI not found

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 255-290

**Implementation Files:**
- `src/LocalWhisper/Adapters/WhisperCLIAdapter.cs` (new)
- `src/LocalWhisper/Models/STTResult.cs` (new)
- `src/LocalWhisper/Models/STTExceptions.cs` (new)
- `tests/LocalWhisper.Tests/Unit/WhisperCLIAdapterTests.cs` (new)

---

### US-021: JSON Output Parsing

**As a** developer
**I want** to parse Whisper JSON output correctly
**So that** transcription text can be extracted reliably

**Acceptance Criteria:**

**AC-1:** Valid JSON parsed successfully
- ✓ Extract "text" field (transcript)
- ✓ Extract "language" field
- ✓ Extract "duration_sec" field
- ✓ Optionally extract "segments" and "meta"

**AC-2:** Malformed JSON handled gracefully
- ✓ STTException thrown with descriptive message
- ✓ File contents logged for debugging
- ✓ App returns to Idle state

**AC-3:** Empty transcript detection
- ✓ Empty "text" field treated as "no speech detected"
- ✓ User notified via log message
- ✓ No clipboard write occurs (placeholder for Iter-4)

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 292-334

**Implementation Files:**
- `src/LocalWhisper/Adapters/WhisperCLIAdapter.cs` (JSON parsing logic)
- `src/LocalWhisper/Models/STTResult.cs` (data model)
- `tests/LocalWhisper.Tests/Unit/STTResultTests.cs` (new)

---

### US-022: Exit Code Mapping

**As a** user
**I want** clear error messages when transcription fails
**So that** I understand what went wrong

**Acceptance Criteria:**

**AC-1:** Exit code 0 → Success
- ✓ No exception thrown
- ✓ JSON output parsed normally

**AC-2:** Exit code 1 → General STT error
- ✓ STTException thrown
- ✓ Dialog: "Fehler bei Transkription"
- ✓ Stderr contents included in error message

**AC-3:** Exit code 2 → Model not found
- ✓ ModelNotFoundException thrown
- ✓ Dialog: "Modell nicht gefunden"
- ✓ Suggests checking model path in config

**AC-4:** Exit code 3 → Audio device error
- ✓ AudioDeviceException thrown
- ✓ Dialog: "Audio-Gerät nicht verfügbar"

**AC-5:** Exit code 4 → Timeout
- ✓ STTTimeoutException thrown
- ✓ Dialog: "Transkription dauerte zu lange"
- ✓ Process killed forcefully

**AC-6:** Exit code 5 → Invalid audio file
- ✓ InvalidAudioException thrown
- ✓ Dialog: "Ungültige Audiodatei"
- ✓ WAV file moved to failed/ directory

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 336-370

**Implementation Files:**
- `src/LocalWhisper/Adapters/WhisperCLIAdapter.cs` (exit code mapping)
- `src/LocalWhisper/Models/STTExceptions.cs` (exception types)

---

### US-023: Model Hash Verification (Placeholder)

**As a** developer
**I want** model integrity verification
**So that** transcription accuracy is ensured

**Acceptance Criteria (Placeholder for Iteration 3):**

**AC-1:** Hash verification function exists
- ✓ Method signature: `bool VerifyModelHash(string modelPath, string expectedHash)`
- ✓ Returns true (always passes for prototype)
- ✓ Logs: "Model hash verification skipped (placeholder)"

**AC-2:** Full implementation deferred to Iteration 5
- ⚠️ TODO: Implement SHA-256 hash calculation
- ⚠️ TODO: Store known-good hashes in config or embedded
- ⚠️ TODO: Add user dialog for corrupted models

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 372-405

**Implementation Files:**
- `src/LocalWhisper/Services/ModelManager.cs` (new, placeholder)

---

## Technical Specifications

### Whisper CLI Invocation

**Command Structure:**
```bash
whisper-cli \
  --model <model_path> \
  --language de \
  --output-format json \
  --output-file <output_json_path> \
  <input_wav_file>
```

**Example:**
```bash
whisper-cli \
  --model "C:\LocalWhisper\models\ggml-small.bin" \
  --language de \
  --output-format json \
  --output-file "C:\LocalWhisper\tmp\stt_result_20251117_145023456.json" \
  "C:\LocalWhisper\tmp\rec_20251117_145023456.wav"
```

**Timeout:** 60 seconds (configurable)

### JSON Output Format

**Expected Structure:**
```json
{
  "text": "Dies ist ein Test-Transkript.",
  "language": "de",
  "duration_sec": 5.2,
  "segments": [
    {
      "start": 0.0,
      "end": 5.2,
      "text": "Dies ist ein Test-Transkript."
    }
  ],
  "meta": {
    "model": "whisper-small",
    "processing_time_sec": 0.8
  }
}
```

**Fields:**
- `text` (string, required): Full transcript
- `language` (string, required): Detected language code
- `duration_sec` (double, required): Audio duration
- `segments` (array, optional): Timestamped segments
- `meta` (object, optional): Processing metadata

### Exit Code Mapping

| Exit Code | Exception | User Message (German) |
|-----------|-----------|----------------------|
| 0 | None | (success) |
| 1 | STTException | "Fehler bei Transkription" |
| 2 | ModelNotFoundException | "Modell nicht gefunden" |
| 3 | AudioDeviceException | "Audio-Gerät nicht verfügbar" |
| 4 | STTTimeoutException | "Transkription dauerte zu lange" |
| 5 | InvalidAudioException | "Ungültige Audiodatei" |

---

## Implementation Tasks

### Task 1: Create STT Exception Types (TDD - RED)

**File:** `src/LocalWhisper/Models/STTExceptions.cs`

**Exception Classes:**
1. `STTException` (base)
2. `ModelNotFoundException`
3. `AudioDeviceException`
4. `STTTimeoutException`
5. `InvalidAudioException`

### Task 2: Create STTResult Model (TDD - RED)

**File:** `src/LocalWhisper/Models/STTResult.cs`

**Properties:**
- `string Text { get; set; }`
- `string Language { get; set; }`
- `double DurationSeconds { get; set; }`
- `List<STTSegment>? Segments { get; set; }`
- `Dictionary<string, object>? Meta { get; set; }`

### Task 3: Write WhisperCLIAdapter Tests (TDD - RED)

**File:** `tests/LocalWhisper.Tests/Unit/WhisperCLIAdapterTests.cs`

**Test Cases:**
1. `InvokeWhisper_ValidWavFile_ReturnsTranscript`
2. `InvokeWhisper_ExitCode2_ThrowsModelNotFoundException`
3. `InvokeWhisper_Timeout_ThrowsSTTTimeoutException`
4. `ParseJSON_ValidOutput_ReturnsSTTResult`
5. `ParseJSON_MalformedJSON_ThrowsSTTException`
6. `ParseJSON_EmptyText_ReturnsEmptyResult`

### Task 4: Implement WhisperCLIAdapter (TDD - GREEN)

**File:** `src/LocalWhisper/Adapters/WhisperCLIAdapter.cs`

**Methods:**
- `Task<STTResult> TranscribeAsync(string wavFilePath, string language = "de")`
- `STTResult ParseJSONOutput(string jsonPath)`
- `void HandleExitCode(int exitCode, string stderr)`

**Dependencies:**
- Uses System.Diagnostics.Process for subprocess
- Uses System.Text.Json for JSON parsing

### Task 5: Integrate with App State Machine

**File:** `src/LocalWhisper/App.xaml.cs` (update)

**Changes:**
- Instantiate WhisperCLIAdapter on app startup
- Replace placeholder in HandleHotkeyPressAsync():
  ```csharp
  // Old: await Task.Delay(300); // Simulate STT
  // New: var result = await _whisperAdapter.TranscribeAsync(wavFilePath);
  ```
- Handle STT exceptions and show appropriate dialogs

### Task 6: Update Configuration Model

**File:** `src/LocalWhisper/Models/AppConfig.cs` (update)

**Add:**
```csharp
public class WhisperConfig
{
    public string CLIPath { get; set; } = "whisper-cli";
    public string ModelPath { get; set; } = "";
    public string Language { get; set; } = "de";
    public int TimeoutSeconds { get; set; } = 60;
}
```

### Task 7: Update Documentation

**Files:**
- `docs/changelog/v0.1-planned.md` - Mark Iteration 3 complete
- `docs/specification/traceability-matrix.md` - Add WhisperCLIAdapter module

---

## Definition of Done

- [ ] All US-020, US-021, US-022, US-023 acceptance criteria satisfied
- [ ] Unit tests written and passing:
  - WhisperCLIAdapter: 6+ tests
  - STTResult: 3+ tests
- [ ] Integration test: Actual Whisper CLI invocation (if CLI available)
- [ ] Manual test: Record → Transcribe → See text in logs
- [ ] No regressions (Iteration 1+2 tests still pass)
- [ ] STT exceptions properly mapped to user dialogs
- [ ] Logging added for:
  - CLI invocation (command, args, exit code)
  - JSON parsing (success/failure)
  - Transcription results
- [ ] Traceability matrix updated
- [ ] Changelog entry added
- [ ] Commit messages reference US-020, US-021, US-022, US-023
- [ ] CI/CD pipeline passes

---

## Out of Scope (Deferred)

**Iteration 4:**
- Clipboard write
- History file storage
- Flyout notification with transcript

**Iteration 5:**
- Model hash verification (full implementation)
- Model download wizard
- Model path configuration UI

**Iteration 6:**
- Whisper CLI settings (language, model selection)

---

## Notes

- **Whisper CLI required:** Tests that invoke actual CLI are integration tests
- **Timeout is critical:** Prevents app hang on long/invalid audio
- **Exit code mapping:** Ensures users get actionable error messages
- **JSON parsing:** Use System.Text.Json for performance and modern .NET practices
- **Placeholder tracking:** US-023 placeholder will be completed in Iteration 5

---

**Status:** Ready for implementation
**Next:** Start Task 1 (STT Exception types - TDD RED phase)
