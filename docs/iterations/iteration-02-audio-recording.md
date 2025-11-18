# Iteration 2: Audio Recording (WASAPI)

**Duration:** 4-6 hours
**Status:** In Progress
**Dependencies:** Iteration 1 (State machine, logging, data root)
**Last Updated:** 2025-11-17

---

## Overview

**Goal:** Implement audio recording functionality using Windows Audio Session API (WASAPI), producing valid WAV files in the correct format for speech-to-text processing.

**Value:** After this iteration, the app can capture audio from the microphone when the Recording state is active, saving properly formatted WAV files that will be processed by Whisper in Iteration 3.

**Scope:**
- ✅ Audio capture via WASAPI (default input device)
- ✅ WAV file generation (16kHz, mono, 16-bit PCM)
- ✅ WAV file validation
- ✅ Microphone error handling
- ✅ Temporary file management (tmp/ directory)
- ⚠️ Placeholder: STT processing (resolved in Iter 3)

---

## User Stories

### US-010: Audio Recording (WASAPI)

**As a** user
**I want** the app to record my voice when I hold the hotkey
**So that** my speech can be transcribed

**Acceptance Criteria:**

**AC-1:** Recording starts when state transitions to Recording
- ✓ AudioRecorder initializes WASAPI capture device
- ✓ Recording begins immediately on state change
- ✓ Default input device (microphone) is used
- ✓ Log message: `"Audio recording started"`

**AC-2:** WAV file is saved with correct format
- ✓ Sample rate: 16000 Hz (Whisper requirement)
- ✓ Channels: 1 (mono)
- ✓ Bit depth: 16 bits
- ✓ Format: PCM
- ✓ File location: `{DataRoot}/tmp/rec_YYYYMMDD_HHmmssfff.wav`
- ✓ Filename includes timestamp to microsecond precision

**AC-3:** Recording stops when state transitions to Processing
- ✓ AudioRecorder stops capture
- ✓ WAV file is finalized with correct header
- ✓ File size is reasonable (30-40 KB per second for 16kHz mono 16-bit)
- ✓ Log message: `"Audio recording stopped, saved to {FilePath}"`

**AC-4:** Recording duration matches hotkey hold time
- ✓ Recording runs only while state is Recording
- ✓ No audio captured before or after state window
- ✓ Manual test: Hold hotkey for 3 seconds → WAV file ~3 seconds (±0.5s tolerance)

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 146-182

**Implementation Files:**
- `src/LocalWhisper/Services/AudioRecorder.cs` (new)
- `src/LocalWhisper/Models/AudioConfig.cs` (new, for future config expansion)
- `tests/LocalWhisper.Tests/Unit/AudioRecorderTests.cs` (new)

---

### US-011: WAV File Validation

**As a** developer
**I want** recorded WAV files to be validated
**So that** only correct-format files are processed by STT

**Acceptance Criteria:**

**AC-1:** Valid WAV files pass validation
- ✓ Check RIFF header (`RIFF....WAVE`)
- ✓ Verify fmt chunk (PCM format code 1)
- ✓ Verify sample rate = 16000 Hz
- ✓ Verify channels = 1
- ✓ Verify bit depth = 16
- ✓ No errors logged for valid files

**AC-2:** Invalid WAV files fail validation
- ✓ Corrupted header detected
- ✓ Wrong format detected (non-PCM)
- ✓ Wrong sample rate detected
- ✓ Error logged: `"Invalid WAV file format: {Reason}"`
- ✓ Validation returns false

**AC-3:** File size validation
- ✓ Size is reasonable for duration (30-40 KB/second)
- ✓ Minimum size check (at least 1 second: ~32 KB)
- ✓ Maximum size check (prevent runaway recordings)

**AC-4:** Failed files are moved to failed/ subdirectory
- ✓ Corrupted files moved to `{DataRoot}/tmp/failed/`
- ✓ Original filename preserved
- ✓ Log message: `"Moved invalid WAV file to failed/"`

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 184-213

**Implementation Files:**
- `src/LocalWhisper/Utils/WavValidator.cs` (new)
- `tests/LocalWhisper.Tests/Unit/WavValidatorTests.cs` (new)

---

### US-012: Microphone Error Handling

**As a** user
**I want** clear error messages when the microphone is unavailable
**So that** I can troubleshoot the issue

**Acceptance Criteria:**

**AC-1:** No microphone detected
- ✓ WASAPI device enumeration fails
- ✓ Error dialog shown: `"Kein Mikrofon gefunden"`
- ✓ Message: `"Bitte schließen Sie ein Mikrofon an oder prüfen Sie die Windows-Audioeinstellungen."`
- ✓ App remains running (no crash)
- ✓ State returns to Idle

**AC-2:** Microphone access denied
- ✓ WASAPI initialization fails (access denied error)
- ✓ Error dialog shown: `"Mikrofonzugriff verweigert"`
- ✓ Message: `"Bitte erlauben Sie den Mikrofonzugriff in den Windows-Datenschutzeinstellungen."`
- ✓ Link/button to open Windows Privacy Settings (optional for Iter 2, defer to Iter 6)

**AC-3:** Microphone disconnected during recording
- ✓ WASAPI capture error detected mid-recording
- ✓ Recording stops gracefully
- ✓ Partial WAV file is saved (if > 1 second)
- ✓ Warning logged: `"Microphone disconnected during recording"`
- ✓ User notified via flyout (deferred to Iter 4)

**BDD Scenarios:** See `docs/specification/user-stories-gherkin.md` lines 215-250 (estimated)

**Implementation Files:**
- `src/LocalWhisper/Services/AudioRecorder.cs` (error handling logic)
- `src/LocalWhisper/UI/Dialogs/ErrorDialog.xaml.cs` (reuse from Iter 1)

---

## Technical Specifications

### WASAPI Integration

**API:** Windows Core Audio APIs (`Windows.Media.Core.Audio` or P/Invoke to `Mmdevapi.dll`)

**Recommended Approach:**
- Use `NAudio` NuGet package for WASAPI abstraction (simpler than raw P/Invoke)
- Alternative: Direct P/Invoke to `IAudioClient`, `IAudioCaptureClient` (more control, more complex)

**Decision:** Use NAudio for Iteration 2 (faster implementation, well-tested library)

**Package:** `NAudio` v2.2.1 or later

### WAV File Format

**Header Structure:**
```
RIFF header (12 bytes):
  - "RIFF" (4 bytes)
  - File size - 8 (4 bytes, little-endian)
  - "WAVE" (4 bytes)

fmt chunk (24 bytes):
  - "fmt " (4 bytes)
  - Chunk size = 16 (4 bytes)
  - Audio format = 1 (PCM, 2 bytes)
  - Channels = 1 (2 bytes)
  - Sample rate = 16000 (4 bytes)
  - Byte rate = 32000 (4 bytes, = sample rate * channels * bit depth / 8)
  - Block align = 2 (2 bytes, = channels * bit depth / 8)
  - Bit depth = 16 (2 bytes)

data chunk:
  - "data" (4 bytes)
  - Data size (4 bytes)
  - Audio samples (N bytes)
```

**File Size Calculation:**
- Bit rate = 16000 Hz * 1 channel * 16 bits = 256,000 bits/sec = 32,000 bytes/sec
- 5-second recording = 160,000 bytes = ~156 KB (header adds ~44 bytes)

---

## Implementation Tasks

### Task 1: Write AudioRecorder Tests (TDD - RED)

**File:** `tests/LocalWhisper.Tests/Unit/AudioRecorderTests.cs`

**Test Cases:**
1. `StartRecording_InitializesWasapiDevice`
2. `StopRecording_SavesWavFile`
3. `SavedWavFile_HasCorrectFormat` (16kHz, mono, 16-bit PCM)
4. `SavedWavFile_HasTimestampFilename`
5. `Recording_ThrowsException_WhenNoMicrophoneAvailable`

### Task 2: Implement AudioRecorder (TDD - GREEN)

**File:** `src/LocalWhisper/Services/AudioRecorder.cs`

**Methods:**
- `void StartRecording(string outputDirectory)` - Starts WASAPI capture
- `string StopRecording()` - Stops capture, returns WAV file path
- `bool IsMicrophoneAvailable()` - Checks for default input device

**Dependencies:**
- `NAudio.Wave` for WASAPI wrapper
- `PathHelpers.GetTmpPath()` for output directory

### Task 3: Write WavValidator Tests (TDD - RED)

**File:** `tests/LocalWhisper.Tests/Unit/WavValidatorTests.cs`

**Test Cases:**
1. `ValidateWavFile_ValidFile_ReturnsTrue`
2. `ValidateWavFile_CorruptedHeader_ReturnsFalse`
3. `ValidateWavFile_WrongSampleRate_ReturnsFalse`
4. `ValidateWavFile_WrongChannels_ReturnsFalse`
5. `MoveInvalidFile_MovesToFailedDirectory`

### Task 4: Implement WavValidator (TDD - GREEN)

**File:** `src/LocalWhisper/Utils/WavValidator.cs`

**Methods:**
- `bool ValidateWavFile(string filePath, out string errorMessage)` - Validates format
- `void MoveToFailedDirectory(string filePath)` - Moves invalid files

### Task 5: Integrate with App State Machine

**File:** `src/LocalWhisper/App.xaml.cs` (update)

**Changes:**
- Instantiate `AudioRecorder` on app startup
- Hook `StateMachine.StateChanged` event:
  - Idle → Recording: Call `audioRecorder.StartRecording()`
  - Recording → Processing: Call `audioRecorder.StopRecording()`, validate WAV file
- Handle microphone errors → show ErrorDialog

### Task 6: Update Documentation

**Files:**
- `docs/changelog/v0.1-planned.md` - Add Iteration 2 completion notes
- `docs/specification/traceability-matrix.md` - Update AudioRecorder module mapping

---

## Definition of Done

- [ ] All US-010, US-011, US-012 acceptance criteria satisfied
- [ ] Unit tests written and passing:
  - AudioRecorder: 5+ tests
  - WavValidator: 5+ tests
- [ ] Manual test: Record 3-second audio → WAV file exists with correct format
- [ ] Manual test: Unplug microphone → error dialog appears
- [ ] No regressions (Iteration 1 tests still pass)
- [ ] WAV files validated with external tool (e.g., ffprobe)
- [ ] Logging added for:
  - Recording start/stop
  - WAV file validation
  - Microphone errors
- [ ] Traceability matrix updated
- [ ] Changelog entry added
- [ ] Commit messages reference US-010, US-011, US-012
- [ ] CI/CD pipeline passes

---

## Out of Scope (Deferred)

**Iteration 3:**
- STT processing (Whisper CLI integration)
- Transcription result handling

**Iteration 4:**
- Flyout notification showing transcript
- History file integration
- Clipboard write

**Iteration 5:**
- User-selectable input device (Settings UI)
- Audio quality configuration (sample rate, bit depth)

**Iteration 6:**
- Settings UI for audio device selection

---

## Notes

- **NAudio simplifies WASAPI:** Using NAudio library avoids low-level COM interop complexity
- **Filename timestamp precision:** Microseconds ensure unique filenames even for rapid re-records
- **tmp/ directory cleanup:** Not implemented in Iter 2 (deferred to Iter 8 cleanup logic)
- **Placeholder tracking:** See `docs/meta/placeholders-tracker.md` for PH-005 (real audio recording)

---

**Status:** Ready for implementation
**Next:** Start Task 1 (AudioRecorder tests - TDD RED phase)
