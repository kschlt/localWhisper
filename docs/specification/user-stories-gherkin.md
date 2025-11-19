# User Stories (Gherkin Format)

**Purpose:** Executable acceptance criteria for all user stories
**Format:** BDD (Behavior-Driven Development) using Gherkin syntax
**Status:** Ready for TDD implementation
**Last Updated:** 2025-11-17

---

## How to Use These Stories (TDD/BDD Workflow)

**For AI Implementation:**

1. **Read Gherkin scenario** (acceptance criteria)
2. **Implement step definitions** (test code)
3. **Run tests** → They fail (RED)
4. **Implement feature code** to make tests pass
5. **Run tests** → They pass (GREEN)
6. **Refactor** if needed
7. **Commit** when all scenarios pass

**Tag System:**
- `@Iter-{N}` - Iteration number
- `@UC-{ID}` - Use case ID
- `@FR-{ID}` - Functional requirement ID
- `@Priority-{High|Medium|Low}` - Implementation priority

---

# ITERATION 1: Hotkey & App Skeleton

## US-001: Hotkey Toggles State

```gherkin
@Iter-1 @UC-001 @FR-010 @Priority-High
Feature: Hotkey State Management
  As a user
  I want to press and hold a hotkey to start recording
  So that I can begin dictation hands-free

  Background:
    Given the app is running
    And the app state is "Idle"
    And the configured hotkey is "Ctrl+Shift+D"

  @Unit @CanRunInClaudeCode
  Scenario: Hotkey down transitions to Recording state
    When the hotkey down event occurs
    Then the app state should be "Recording"
    And a state transition log should be created
    And the log should contain "State transition: Idle -> Recording"

  @Unit @CanRunInClaudeCode
  Scenario: Hotkey up transitions to Processing state
    Given the app state is "Recording"
    When the hotkey up event occurs
    Then the app state should be "Processing"
    And a state transition log should be created
    And the log should contain "State transition: Recording -> Processing"

  @Unit @CanRunInClaudeCode
  Scenario: Invalid transition is rejected
    Given the app state is "Idle"
    When an attempt is made to transition directly to "Processing"
    Then an InvalidStateTransitionException should be thrown
    And the app state should remain "Idle"

  @WindowsOnly @Manual
  Scenario: Hotkey works when app is not in focus
    Given another application has focus
    And the app is running in the background
    When I press and hold "Ctrl+Shift+D"
    Then the app state should become "Recording"
    And the tray icon should update within 100 milliseconds
```

## US-002: Tray Icon Shows Status

```gherkin
@Iter-1 @FR-010 @Priority-High
Feature: Tray Icon Status Indicator
  As a user
  I want to see the app's current state in the tray icon
  So that I know when recording is active

  Background:
    Given the app is running
    And the tray icon is visible

  @WindowsOnly @Manual
  Scenario: Tray icon shows Idle state
    Given the app state is "Idle"
    Then the tray icon should display the default icon
    And the icon tooltip should be "Dictate-to-Clipboard: Bereit"

  @WindowsOnly @Manual
  Scenario: Tray icon shows Recording state
    Given the app state transitions to "Recording"
    Then the tray icon should display the recording icon (red/active)
    And the icon tooltip should be "Dictate-to-Clipboard: Aufnahme..."
    And the icon update should occur within 100 milliseconds

  @WindowsOnly @Manual
  Scenario: Tray icon shows Processing state
    Given the app state transitions to "Processing"
    Then the tray icon should display the processing icon (spinner)
    And the icon tooltip should be "Dictate-to-Clipboard: Verarbeitung..."
```

## US-003: Hotkey Conflict Error Dialog

```gherkin
@Iter-1 @FR-021 @Priority-Medium
Feature: Hotkey Conflict Detection
  As a user
  I want to be notified if my chosen hotkey is already in use
  So that I can choose a different combination

  Background:
    Given the app is starting up

  @WindowsOnly @Manual
  Scenario: Hotkey conflict is detected on startup
    Given another application has registered "Ctrl+Shift+D"
    When the app attempts to register the hotkey
    Then hotkey registration should fail
    And an error dialog should appear
    And the dialog title should be "Hotkey nicht verfügbar"
    And the dialog message should be "Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination in den Einstellungen."
    And the app should remain running (no crash)
    And the error should be logged with severity "Warning"

  @WindowsOnly @Manual
  Scenario: User can open settings from error dialog
    Given hotkey registration failed
    And the error dialog is displayed
    When the user clicks "Einstellungen öffnen"
    Then the Settings window should open
    And the Hotkey configuration field should be focused
```

---

# ITERATION 2: Audio Recording

## US-010: Audio Recording (WASAPI)

```gherkin
@Iter-2 @UC-001 @FR-011 @Priority-High
Feature: Audio Recording via WASAPI
  As the app
  I need to record audio from the microphone
  So that it can be transcribed

  Background:
    Given the app state is "Recording"
    And the microphone is available

  @Integration @CanRunInClaudeCode
  Scenario: WAV file is created with correct format
    When recording runs for 5 seconds
    And recording is stopped
    Then a WAV file should exist in the tmp/ directory
    And the WAV file should have a name matching "rec_YYYYMMDD_HHmmssfff.wav"
    And the WAV file should be at least 1 second in duration

  @Contract @CanRunInClaudeCode
  Scenario: WAV file has correct technical specifications
    Given a WAV file was created from a 5-second recording
    When the WAV file is analyzed
    Then the sample rate should be 16000 Hz
    And the channel count should be 1 (mono)
    And the bit depth should be 16 bits
    And the format should be PCM

  @WindowsOnly @Manual
  Scenario: Recording duration matches hotkey hold time
    Given the microphone is recording
    When the hotkey is held for exactly 3 seconds
    And the hotkey is released
    Then the resulting WAV file should be approximately 3 seconds long (±0.5s tolerance)
```

## US-011: WAV File Validation

```gherkin
@Iter-2 @FR-011 @Priority-Medium
Feature: WAV File Validation
  As the app
  I need to validate recorded audio files
  So that only valid files are processed

  @Contract @CanRunInClaudeCode
  Scenario: Valid WAV file passes validation
    Given a WAV file with correct header and format
    When the file is validated
    Then validation should succeed
    And no errors should be logged

  @Contract @CanRunInClaudeCode
  Scenario: Corrupted WAV file fails validation
    Given a WAV file with an invalid header
    When the file is validated
    Then validation should fail
    And an error should be logged with message "Invalid WAV file format"
    And the file should be moved to a failed/ subdirectory

  @Unit @CanRunInClaudeCode
  Scenario: WAV file size is reasonable
    Given a 5-second recording
    When the WAV file is created
    Then the file size should be between 150 KB and 200 KB
```

## US-012: Microphone Error Handling

```gherkin
@Iter-2 @FR-021 @Priority-High
Feature: Microphone Error Handling
  As a user
  I want clear error messages when the microphone is unavailable
  So that I can fix the issue

  @WindowsOnly @Manual
  Scenario: Microphone permissions denied
    Given the microphone permissions are denied
    When the hotkey down event occurs
    Then recording should not start
    And the app state should remain "Idle"
    And an error dialog should appear
    And the dialog title should be "Mikrofon nicht verfügbar"
    And the dialog message should be "Mikrofon nicht verfügbar. Bitte überprüfen Sie die Berechtigungen und Geräteverbindung."
    And the error should be logged with microphone device info

  @WindowsOnly @Manual
  Scenario: Microphone is in use by another app
    Given the microphone is being used by another application
    When the hotkey down event occurs
    Then recording should fail gracefully
    And an error dialog should suggest closing other apps using the microphone
    And the app should not crash

  @WindowsOnly @Manual
  Scenario: No microphone device found
    Given no microphone is connected to the system
    When the hotkey down event occurs
    Then an error dialog should appear
    And the dialog should suggest connecting a microphone
```

---

# ITERATION 3: STT with Whisper

## US-020: STT via Whisper CLI

```gherkin
@Iter-3 @UC-001 @FR-012 @Priority-High
Feature: Speech-to-Text with Whisper CLI
  As the app
  I need to invoke Whisper CLI to transcribe audio
  So that speech becomes text

  Background:
    Given a WAV file "sample-5s.wav" exists in tmp/
    And the Whisper CLI path is configured
    And the model file exists and is valid

  @Integration @CanRunInClaudeCode
  Scenario: Whisper CLI is invoked with correct arguments
    When transcription is requested for "sample-5s.wav" with language "de"
    Then the CLI should be invoked with the following arguments:
      | Argument | Value |
      | --model | <model_path> |
      | --language | de |
      | --output-format | json |
      | --output-file | <tmp_path>/stt_result.json |
      | <input> | <tmp_path>/sample-5s.wav |
    And the process should have a 60-second timeout

  @Contract @CanRunInClaudeCode
  Scenario: Successful transcription returns valid result
    Given Whisper CLI exits with code 0
    And stt_result.json contains valid JSON
    When the result is parsed
    Then the result should have a "text" field (not empty)
    And the result should have a "language" field
    And the result should have a "duration_sec" field
    And the result should optionally have "segments" and "meta" fields
```

## US-021: JSON Output Parsing

```gherkin
@Iter-3 @FR-012 @Priority-High
Feature: Whisper CLI JSON Output Parsing
  As the app
  I need to parse Whisper CLI output correctly
  So that transcription text can be extracted

  @Contract @CanRunInClaudeCode
  Scenario: Valid JSON is parsed successfully
    Given stt_result.json contains:
      """
      {
        "text": "This is a test transcript.",
        "language": "en",
        "duration_sec": 5.0,
        "segments": [{"start": 0.0, "end": 5.0, "text": "This is a test transcript."}],
        "meta": {"model": "whisper-small", "processing_time_sec": 0.5}
      }
      """
    When the JSON is parsed
    Then the transcript text should be "This is a test transcript."
    And the language should be "en"
    And the duration should be 5.0 seconds
    And the segments array should have 1 element

  @Contract @CanRunInClaudeCode
  Scenario: Malformed JSON triggers error
    Given stt_result.json contains invalid JSON
    When the JSON is parsed
    Then a STTException should be thrown
    And the error message should contain "Invalid JSON format"
    And the error should be logged with the file contents

  @Contract @CanRunInClaudeCode
  Scenario: Empty transcript text is handled
    Given stt_result.json contains a "text" field with empty string ""
    When the JSON is parsed
    Then the result should be treated as "no speech detected"
    And no clipboard write should occur
    And a flyout should show "Keine Sprache erkannt"
```

## US-022: Exit Code Mapping

```gherkin
@Iter-3 @FR-012 @FR-021 @Priority-High
Feature: Whisper CLI Exit Code Handling
  As the app
  I need to map Whisper CLI exit codes to user-friendly errors
  So that users understand what went wrong

  @Integration @CanRunInClaudeCode
  Scenario Outline: Exit codes are mapped correctly
    Given Whisper CLI exits with code <exit_code>
    When the exit code is processed
    Then a <exception_type> should be thrown
    And an error dialog should show with title "<dialog_title>"
    And the error should be logged

    Examples:
      | exit_code | exception_type | dialog_title |
      | 0 | None | (no error) |
      | 1 | STTException | Fehler bei Transkription |
      | 2 | ModelNotFoundException | Modell nicht gefunden |
      | 3 | AudioDeviceException | Audio-Gerät nicht verfügbar |
      | 4 | STTTimeoutException | Transkription dauerte zu lange |
      | 5 | InvalidAudioException | Ungültige Audiodatei |

  @Integration @CanRunInClaudeCode
  Scenario: STT timeout after 60 seconds
    Given Whisper CLI is processing
    When 60 seconds have elapsed
    Then the process should be killed
    And a STTTimeoutException should be thrown
    And a dialog should show "Transkription dauerte zu lange und wurde abgebrochen"
    And the app should return to Idle state
```

## US-023: Model Hash Verification

```gherkin
@Iter-3 @FR-017 @Priority-Medium
Feature: Model Integrity Verification
  As the app
  I need to verify the model file hasn't been corrupted
  So that transcription is accurate

  @Contract @CanRunInClaudeCode
  Scenario: Valid model file passes hash check
    Given a model file "ggml-small.bin" exists
    And the SHA-256 hash matches the known-good hash
    When the model is verified
    Then verification should succeed
    And a log entry should be created "Model hash verified: ggml-small.bin"

  @Contract @CanRunInClaudeCode
  Scenario: Corrupted model file fails hash check
    Given a model file exists
    But the SHA-256 hash does NOT match the expected hash
    When the model is verified
    Then verification should fail
    And an error should be logged with expected vs. actual hash
    And transcription should not proceed

  @WindowsOnly @Manual
  Scenario: User is warned about corrupted model
    Given model hash verification failed
    When the user attempts to transcribe
    Then a dialog should appear with title "Modell ungültig"
    And the dialog should suggest re-downloading the model
    And the user should have options: "Neu herunterladen" or "Anderen Pfad wählen"
```

---

# ITERATION 4: Clipboard + History + Flyout

## US-030: Clipboard Write

```gherkin
@Iter-4 @UC-001 @FR-013 @Priority-High
Feature: Clipboard Write
  As a user
  I want the transcript to appear in my clipboard immediately
  So that I can paste it anywhere

  Background:
    Given a transcription has completed successfully
    And the transcript text is "Let me check on that and get back to you."

  @WindowsOnly @Manual
  Scenario: Transcript is written to clipboard
    When the clipboard write operation completes
    Then pressing Ctrl+V should paste "Let me check on that and get back to you."
    And the clipboard operation should be logged

  @Integration @CanRunInClaudeCode
  Scenario: Clipboard write with mock service
    Given a mock clipboard service
    When the transcript is written to clipboard
    Then the mock service should contain the transcript text
    And a "clipboard_write_success" log entry should exist

  @WindowsOnly @Manual
  Scenario: Clipboard locked by another app (retry)
    Given another application has locked the clipboard
    When clipboard write is attempted
    Then the app should wait 100 milliseconds
    And retry clipboard write once
    And if still locked, show error dialog "Zwischenablage gesperrt"
    But the transcript should still be saved to history
```

## US-031: History File Creation

```gherkin
@Iter-4 @UC-001 @FR-014 @Priority-High
Feature: History File Creation
  As a user
  I want all my dictations saved automatically
  So that I have a searchable history

  Background:
    Given a transcription completed at "2025-09-17T14:30:22Z"
    And the transcript is "Let me check on that and get back to you tomorrow morning."
    And the language is "en"
    And the model is "whisper-small"
    And the duration is 4.8 seconds

  @Contract @CanRunInClaudeCode
  Scenario: History file is created with correct path
    When the history file is written
    Then a file should exist at path matching:
      "history/2025/2025-09/2025-09-17/20250917_143022_let-me-check-on-that-and-get.md"
    And the file should be UTF-8 encoded

  @Contract @CanRunInClaudeCode
  Scenario: History file contains correct front-matter
    When the history file is written
    Then the file should contain YAML front-matter between "---" delimiters
    And the front-matter should have field "created" with value "2025-09-17T14:30:22Z"
    And the front-matter should have field "lang" with value "en"
    And the front-matter should have field "stt_model" with value "whisper-small"
    And the front-matter should have field "duration_sec" with value 4.8
    And the front-matter should have field "post_processed" with value false

  @Contract @CanRunInClaudeCode
  Scenario: History file contains correct body
    When the history file is written
    Then the file should contain heading "# Diktat – 17.09.2025 14:30"
    And the file should contain the transcript text
    And the body should be on separate lines after front-matter

  @Integration @CanRunInClaudeCode
  Scenario: History write failure does not block clipboard
    Given the history directory is write-protected
    When history write is attempted
    Then the operation should fail gracefully
    And the clipboard write should succeed anyway
    And a warning should be logged "History write failed: permission denied"
    And a flyout should show "History konnte nicht gespeichert werden"
```

## US-032: Custom Flyout Notification

```gherkin
@Iter-4 @FR-015 @Priority-High
Feature: Custom Flyout Notification
  As a user
  I want visual confirmation that my transcript is ready
  So that I know when to paste

  Background:
    Given the app language is "de"
    And transcription has completed

  @WindowsOnly @Manual
  Scenario: Flyout appears after clipboard write
    When clipboard write completes
    Then a flyout window should appear near the system tray
    And the flyout should display text "Transkript im Clipboard"
    And the flyout should be visible for approximately 3 seconds
    And the flyout should auto-dismiss without user interaction

  @WindowsOnly @Manual
  Scenario: Flyout does not steal focus
    Given another application has focus
    And the user is typing
    When the flyout appears
    Then the other application should remain in focus
    And the user's typing should not be interrupted

  @WindowsOnly @Manual
  Scenario: Flyout appearance time (performance)
    Given clipboard write completes at timestamp T0
    When the flyout appears at timestamp T1
    Then (T1 - T0) should be ≤ 500 milliseconds
```

## US-033: E2E Latency Measurement (NFR-001)

```gherkin
@Iter-4 @NFR-001 @Priority-High
Feature: End-to-End Latency Performance
  As a user
  I want fast transcription
  So that I don't wait too long

  @WindowsOnly @Manual
  Scenario: p95 latency is within target
    Given 100 test dictations are performed
    And dictations vary in length: 3s, 5s, 10s
    When latency is measured from hotkey-up to clipboard-write
    And p50, p95, and p99 percentiles are calculated
    Then p95 latency should be ≤ 2.5 seconds
    And results should be documented in changelog

  @Integration @CanRunInClaudeCode
  Scenario: Individual operation timings are logged
    Given a dictation flow completes
    Then log entries should include:
      | Operation | Timing Field |
      | Audio stop | AudioRecorder_Stop_Duration_Ms |
      | STT invocation | STT_Duration_Ms |
      | Clipboard write | Clipboard_Write_Duration_Ms |
      | History write | History_Write_Duration_Ms |
    And total E2E latency should be logged as "E2E_Latency_Ms"
```

## US-034: Flyout Latency Measurement (NFR-004)

```gherkin
@Iter-4 @NFR-004 @Priority-Medium
Feature: Flyout Display Latency
  As a user
  I want the flyout to appear immediately
  So that I get instant feedback

  @WindowsOnly @Manual
  Scenario: Flyout appears quickly
    Given clipboard write completes at time T0
    When the flyout becomes visible at time T1
    Then (T1 - T0) should be ≤ 0.5 seconds

  @Integration @CanRunInClaudeCode
  Scenario: Flyout latency is logged
    Given a dictation completes
    Then a log entry "Flyout_Display_Latency_Ms" should exist
    And the value should be < 500
```

## US-035: End-to-End Logging

```gherkin
@Iter-4 @FR-023 @Priority-Medium
Feature: Comprehensive Operation Logging
  As a developer/support
  I need complete logs of the dictation flow
  So that I can diagnose issues

  @Integration @CanRunInClaudeCode
  Scenario: Full dictation flow is logged
    Given a dictation completes successfully
    Then the log should contain entries in order:
      | Event | Log Message Pattern |
      | Hotkey down | State transition: Idle -> Recording |
      | Recording start | AudioRecorder started |
      | Hotkey up | State transition: Recording -> Processing |
      | Recording stop | AudioRecorder stopped, Duration=<N>s, FilePath=<path> |
      | STT start | STT invocation started, Command=<cmd> |
      | STT complete | STT completed, ExitCode=0, Duration=<N>ms, TranscriptLength=<N> |
      | Clipboard write | Clipboard write succeeded, TextLength=<N> |
      | History write | History file created, Path=<path> |
      | Flyout | Flyout shown, Message=Transkript im Clipboard |
      | State return | State transition: Processing -> Idle |
```

## US-036: Slug Generation

```gherkin
@Iter-4 @FR-024 @Priority-Medium
Feature: Slug Generation for History Filenames
  As a user
  I want meaningful filenames for my history
  So that I can identify dictations without opening them

  @Unit @CanRunInClaudeCode
  Scenario Outline: Slug is generated correctly
    Given the transcript text is "<input>"
    When the slug is generated
    Then the slug should be "<expected>"

    Examples:
      | input | expected |
      | Let me check on that and get back to you | let-me-check-on-that-and-get |
      | Meeting at 3:00 PM | meeting-at-3-00-pm |
      | Re: Project Alpha — status update | re-project-alpha-status-update |
      | Äpfel, Öl & Übung | apfel-ol-ubung |
      | (empty string) | transcript |

  @Unit @CanRunInClaudeCode
  Scenario: Slug is truncated to 50 characters
    Given a transcript with 200 words
    When the slug is generated
    Then the slug length should be ≤ 50 characters

  @Unit @CanRunInClaudeCode
  Scenario: Multiple hyphens are compressed
    Given the transcript "Hello-----world"
    When the slug is generated
    Then the slug should be "hello-world" (single hyphen)

  @Integration @CanRunInClaudeCode
  Scenario: Duplicate slugs are handled
    Given a history file already exists: "20250917_143022_let-me-check.md"
    And a new dictation has the same slug
    When the history file is written
    Then the new file should be named "20250917_143023_let-me-check_2.md"
```

---

# ITERATION 5: First-Run Wizard + Repair

## US-040: Wizard Step 1 - Data Root Selection

```gherkin
@Iter-5 @UC-002 @FR-016 @Priority-High
Feature: Wizard Step 1: Data Root Selection
  As a new user
  I want to choose where my data is stored
  So that I have control over file locations

  Background:
    Given the app is starting for the first time
    And no config.toml exists

  @WindowsOnly @Manual
  Scenario: Wizard shows default data root
    When the wizard opens to Step 1
    Then the default path should be displayed as "%LOCALAPPDATA%\SpeechClipboardApp\"
    And the path should be resolved (e.g., "C:\Users\JohnDoe\AppData\Local\SpeechClipboardApp\")

  @WindowsOnly @Manual
  Scenario: User can choose custom data root
    Given the wizard is on Step 1
    When the user clicks "Durchsuchen..." (Browse)
    And selects a folder "D:\MyApps\Dictation\"
    And clicks "Weiter" (Next)
    Then the chosen path should be validated
    And if writable, the wizard should proceed to Step 2

  @Integration @CanRunInClaudeCode
  Scenario: Folder structure is created
    Given the user has chosen data root "D:\MyApps\Dictation\"
    When Step 1 completes
    Then the following directories should be created:
      | Directory |
      | config/ |
      | models/ |
      | history/ |
      | logs/ |
      | tmp/ |
```

## US-041a: Wizard Step 2 - Model Verification (File Selection)

**Note:** This user story was split from US-041. HTTP download is deferred to US-041b (Iteration 5b).

```gherkin
@Iter-5a @FR-017 @Priority-High
Feature: Wizard Step 2: Model Verification (File Selection)
  As a new user
  I want to provide my existing Whisper model file
  So that transcription works correctly

  Background:
    Given the wizard is on Step 2
    And the data root has been configured
    And the user has already downloaded a model from HuggingFace or whisper.cpp

  @WindowsOnly @Manual
  Scenario: User provides existing model file
    When the user clicks "Modell-Datei auswählen..."
    And selects a file "C:\Downloads\ggml-small.bin"
    Then SHA-1 hash verification should start
    And a progress indicator should show "Berechne Hash..."
    And when hash matches "55356645c2b361a969dfd0ef2c5a50d530afd8d5"
    Then the file should be copied to "<data_root>/models/ggml-small.bin"
    And a success message "Modell OK ✓" should display
    And the "Weiter" button should be enabled

  @Contract @CanRunInClaudeCode
  Scenario: Model file hash verification (SHA-1)
    Given a model file "ggml-small.bin" is selected
    When SHA-1 hash is computed
    And the computed hash is "55356645c2b361a969dfd0ef2c5a50d530afd8d5"
    Then verification succeeds
    And the model is marked as valid

  @WindowsOnly @Manual
  Scenario: Invalid model file hash
    Given a corrupted or wrong file "ggml-small.bin" is selected
    When SHA-1 hash is computed
    And the hash does NOT match expected value "55356645c2b361a969dfd0ef2c5a50d530afd8d5"
    Then an error dialog should show: "Modell-Datei ist beschädigt oder ungültig"
    And the "Weiter" button should remain disabled

  @WindowsOnly @Manual
  Scenario: Model tradeoff display
    Given the wizard is on Step 2
    When the user views the model selection options
    Then the following models should be listed:
      | Model | Description |
      | base | Schnell (142 MB) - Gut für Echtzeit |
      | small | Empfohlen (466 MB) - Beste Balance ⭐ |
      | medium | Hohe Qualität (1.5 GB) - Langsamer |
      | large-v3 | Höchste Qualität (2.9 GB) - Am langsamsten |
    And "small" should be highlighted as recommended

  @WindowsOnly @Manual
  Scenario: Language selection affects model list
    Given the wizard is on Step 2
    When the user selects language "English"
    Then the model list should show: base.en, small.en, medium.en, large-v3
    When the user selects language "German"
    Then the model list should show: base, small, medium, large-v3
```

## US-041b: Wizard Step 2 - Model Download

**Status:** Deferred to Iteration 5b
**Rationale:** HTTP download is a UX enhancement, not blocking for v0.1. Users can download models manually (one-time setup).

```gherkin
@Iter-5b @FR-017 @Priority-Medium
Feature: Wizard Step 2: Model Download
  As a new user
  I want to download a model directly from the wizard
  So that I don't have to find it manually

  Background:
    Given the wizard is on Step 2
    And the data root has been configured

  @WindowsOnly @Manual
  Scenario: User downloads model with progress tracking
    When the user selects "Modell herunterladen"
    And selects language "German / Deutsch"
    And selects model size "small (466 MB, empfohlen)"
    And clicks "Herunterladen"
    Then a download progress dialog should appear
    And the model should download from configured URL
    And progress bar should update every 100ms
    And download speed should be displayed (MB/s)
    And ETA should be displayed
    And when complete, SHA-1 verification should run
    And on success, "Modell OK ✓" should display

  @WindowsOnly @Manual
  Scenario: Download can be cancelled
    Given a model download is in progress
    When the user clicks "Abbrechen"
    Then the download should stop immediately
    And the partial file should be deleted
    And the wizard should return to model selection

  @Integration @CanRunInClaudeCode
  Scenario: Download retry on failure
    Given the model download fails (network error)
    When the download error occurs
    Then the system should retry up to 3 times
    And each retry should use exponential backoff (1s, 2s, 4s)
    And if all retries fail, an error dialog should show

  @Contract @CanRunInClaudeCode
  Scenario: SHA-1 verification after download
    Given a model file "ggml-small.bin" has been downloaded
    When SHA-1 hash is computed
    And the hash is "55356645c2b361a969dfd0ef2c5a50d530afd8d5"
    Then verification succeeds
    And the model is ready for use
    When the hash does NOT match
    Then the download is marked as failed
    And the file is deleted
    And an error dialog shows: "Download beschädigt - bitte erneut versuchen"
```

## US-042: Wizard Step 3 - Hotkey Configuration

```gherkin
@Iter-5 @FR-016 @Priority-High
Feature: Wizard Step 3: Hotkey Configuration
  As a new user
  I want to choose my dictation hotkey
  So that it doesn't conflict with other apps

  Background:
    Given the wizard is on Step 3

  @WindowsOnly @Manual
  Scenario: Default hotkey is shown
    When Step 3 loads
    Then the hotkey field should display "Ctrl+Shift+D"
    And a label should explain "Halten Sie diese Tastenkombination während des Diktierens"

  @WindowsOnly @Manual
  Scenario: User chooses custom hotkey
    When the user clicks "Ändern..."
    And presses "Ctrl+Alt+D"
    Then the hotkey field should update to "Ctrl+Alt+D"
    And the hotkey should be validated (not already registered)
    And if valid, "Fertig" button should be enabled

  @WindowsOnly @Manual
  Scenario: Hotkey conflict is detected
    Given another app has registered "Ctrl+Shift+D"
    When the user attempts to finish the wizard
    Then a warning should appear: "Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination."
    And the wizard should remain on Step 3

  # NOTE: Autostart removed from scope (v0.1)
  # No autostart checkbox in Step 3
```

## US-043: Repair Flow (Data Root Missing)

```gherkin
@Iter-5 @UC-003 @FR-016 @Priority-Medium
Feature: Repair Flow for Missing Data Root
  As a user
  I want to recover if my data folder was moved or deleted
  So that I don't lose access to the app

  Background:
    Given the app was previously configured
    And config.toml exists but data root is invalid

  @WindowsOnly @Manual
  Scenario: App detects missing data root on startup
    When the app starts
    And the configured data root does not exist
    Then a repair dialog should appear
    And the dialog should say "Datenordner nicht gefunden"
    And options should be: "Neuen Ordner wählen" or "Neu einrichten"

  @WindowsOnly @Manual
  Scenario: User re-links to moved folder
    Given the repair dialog is shown
    When the user clicks "Neuen Ordner wählen"
    And selects the moved folder "D:\Backup\SpeechClipboardApp\"
    And the folder contains valid subfolders (config/, models/, history/)
    Then the config should be updated with the new path
    And the app should start normally

  @WindowsOnly @Manual
  Scenario: User restarts wizard (fresh setup)
    Given the repair dialog is shown
    When the user clicks "Neu einrichten"
    Then the full wizard should launch
    And old data should be orphaned (not deleted automatically)
```

## US-044: Wizard Completion Time (NFR-004)

```gherkin
@Iter-5 @NFR-004 @Priority-Medium
Feature: Wizard Completion Time
  As a new user
  I want setup to be quick
  So that I can start using the app immediately

  @WindowsOnly @Manual
  Scenario: Wizard completes in under 2 minutes
    Given the wizard starts at time T0
    And the user accepts all defaults:
      | Step | Action |
      | 1 | Accept default data root |
      | 2 | Model already downloaded, just verify hash |
      | 3 | Accept default hotkey |
    When the user clicks "Fertig" at time T1
    Then (T1 - T0) should be < 120 seconds
    And the wizard window should close
    And the app should start normally
    Note: Completion time measured when user clicks "Fertig", excluding model download time
```

## US-045: Wizard Logging (NFR-006)

```gherkin
@Iter-5 @NFR-006 @Priority-Low
Feature: Wizard Operation Logging
  As a developer/support
  I need logs of wizard actions
  So that I can diagnose setup issues

  @Integration @CanRunInClaudeCode
  Scenario: Wizard steps are logged
    Given the wizard runs to completion
    Then the log should contain:
      | Event | Log Pattern |
      | Wizard start | Wizard started: first run detected |
      | Step 1 | Data root chosen: <path> |
      | Step 1 | Directories created: config, models, history, logs, tmp |
      | Step 2 | Model verification started: <filename> |
      | Step 2 | Model hash verified: OK |
      | Step 3 | Hotkey configured: <combination> |
      | Wizard complete | Wizard completed successfully, config saved |
```

## US-046: Write-Protected Folder Error

```gherkin
@Iter-5 @FR-021 @Priority-Medium
Feature: Write-Protected Folder Error Handling
  As a user
  I want clear guidance if I choose a read-only folder
  So that I can fix the issue

  @WindowsOnly @Manual
  Scenario: User selects write-protected folder
    Given the wizard is on Step 1
    When the user selects folder "C:\Program Files\MyApp\" (read-only)
    And clicks "Weiter"
    Then folder validation should fail
    And an error should display: "Ordner ist schreibgeschützt. Bitte wählen Sie einen anderen Ordner."
    And the wizard should remain on Step 1
    And the user should be able to choose a different folder
```

---

# ITERATION 6: Settings

## US-050: Settings - Hotkey Change

```gherkin
@Iter-6 @FR-020 @Priority-Medium
Feature: Settings: Hotkey Configuration
  As a user
  I want to change my hotkey after setup
  So that I can avoid conflicts with new apps

  Background:
    Given the Settings window is open
    And the current hotkey is "Ctrl+Shift+D"

  @WindowsOnly @Manual
  Scenario: User changes hotkey successfully
    When the user clicks the hotkey field
    And presses "Ctrl+Alt+D"
    And clicks "Speichern"
    Then the config should be updated with the new hotkey
    And a message should display "Neustart erforderlich"
    And after app restart, the new hotkey should be active

  @WindowsOnly @Manual
  Scenario: Hotkey conflict is detected in settings
    When the user attempts to set a hotkey already in use
    Then a warning should appear: "Hotkey bereits belegt"
    And the old hotkey should remain active
```

## US-051: Settings - Data Root Change

```gherkin
@Iter-6 @FR-020 @Priority-Medium
Feature: Settings: Data Root Relocation
  As a user
  I want to move my data folder
  So that I can reorganize my files

  @WindowsOnly @Manual
  Scenario: User changes data root
    Given the current data root is "C:\Users\...\AppData\Local\SpeechClipboardApp"
    When the user clicks "Ordner wählen..."
    And selects "D:\MyData\Dictation"
    And clicks "Speichern"
    Then a confirmation dialog should ask "Dateien verschieben oder nur Pfad ändern?"
    And if user clicks "Verschieben", files should be moved to new location
    And if user clicks "Nur Pfad ändern", only config should be updated
    And a message should display "Neustart erforderlich"
```

## US-052: Settings - Language and Format

```gherkin
@Iter-6 @FR-020 @Priority-Low
Feature: Settings: Language and File Format
  As a user
  I want to customize app language and history format
  So that the app fits my preferences

  @WindowsOnly @Manual
  Scenario: User changes app language
    Given the app language is "de"
    When the user selects language "English"
    And clicks "Speichern"
    Then a message should display "Restart required / Neustart erforderlich"
    And after restart, UI should be in English

  @Integration @CanRunInClaudeCode
  Scenario: User changes file format to .txt
    Given the current history format is ".md"
    When the user selects format ".txt"
    And clicks "Speichern"
    Then the next dictation should create a .txt file
    And existing .md files should remain unchanged
    And no restart should be required
```

## US-053: Settings - Model Check/Reload

```gherkin
@Iter-6 @FR-017 @Priority-Medium
Feature: Settings: Model Verification
  As a user
  I want to verify my model is still valid
  So that I can troubleshoot transcription issues

  @WindowsOnly @Manual
  Scenario: User checks model integrity
    Given the Settings window is open
    When the user clicks "Modell prüfen"
    Then the SHA-1 hash should be recomputed
    And if valid, a message should display "Modell OK ✓"
    And if invalid, a warning should display "Modell ungültig. Bitte neu herunterladen."

  @WindowsOnly @Manual
  Scenario: User reloads/changes model
    When the user clicks "Anderes Modell wählen..."
    And selects a different model file
    Then the new model should be verified
    And if valid, config should be updated
    And no restart should be required (next transcription uses new model)
```

## US-054: Settings Window - Access and Navigation

```gherkin
@Iter-6 @FR-020 @Priority-High
Feature: Settings Window Access
  As a user
  I want to access settings easily
  So that I can configure the app

  @WindowsOnly @Manual
  Scenario: User opens Settings from tray menu
    Given the app is running in the system tray
    When the user right-clicks the tray icon
    Then a menu should appear with options: "Einstellungen", "History", "Beenden"
    When the user clicks "Einstellungen"
    Then the Settings window should open
    And the Settings window should be modal (blocks app interaction)
    And the Settings window should be centered on screen

  @WindowsOnly @Manual
  Scenario: User opens history folder from tray menu
    Given the app is running
    When the user right-clicks the tray icon
    And clicks "History"
    Then Windows Explorer should open showing the history folder
    And the path should be "<data_root>/history/"

  @WindowsOnly @Manual
  Scenario: Settings window shows current configuration
    Given the current hotkey is "Ctrl+Shift+D"
    And the data root is "C:\Users\...\LocalWhisper"
    And the language is "de"
    When the Settings window opens
    Then all fields should display current values
    And the Save button should be disabled (no changes yet)
    And the version number "v0.1.0" should be displayed at bottom-left
```

## US-055: Settings - Save and Cancel Behavior

```gherkin
@Iter-6 @FR-020 @Priority-High
Feature: Settings Save and Cancel
  As a user
  I want my changes saved correctly
  So that my preferences persist

  @Integration @CanRunInClaudeCode
  Scenario: Save button disabled until changes made
    Given the Settings window is open
    And no fields have been modified
    Then the Save button should be disabled
    When the user changes any field
    Then the Save button should be enabled

  @WindowsOnly @Manual
  Scenario: User saves changes without restart requirement
    Given the Settings window is open
    When the user changes file format from ".md" to ".txt"
    And clicks "Speichern"
    Then the config.toml should be updated
    And the Settings window should close immediately
    And no restart dialog should appear

  @WindowsOnly @Manual
  Scenario: User saves changes requiring restart
    Given the Settings window is open
    When the user changes the hotkey to "Ctrl+Alt+D"
    And clicks "Speichern"
    Then the config should be saved
    And a restart dialog should appear: "Einige Änderungen erfordern einen Neustart. Jetzt neu starten?"
    When the user clicks "Ja"
    Then the app should restart
    When the user clicks "Nein"
    Then the Settings window should close
    And changes should be saved but not yet active

  @WindowsOnly @Manual
  Scenario: Multiple changes requiring restart
    Given the Settings window is open
    When the user changes hotkey, language, and data root
    And clicks "Speichern"
    Then ONE restart dialog should appear (not multiple)
    And after restart, all changes should be active

  @WindowsOnly @Manual
  Scenario: User cancels with no changes
    Given the Settings window is open
    And no fields were modified
    When the user clicks "Abbrechen"
    Then the window should close immediately (no confirmation)

  @WindowsOnly @Manual
  Scenario: User cancels with unsaved changes
    Given the Settings window is open
    And the user changed the hotkey to "Ctrl+Alt+V"
    When the user clicks "Abbrechen"
    Then a confirmation dialog should appear: "Änderungen verwerfen?"
    When the user clicks "Ja"
    Then the Settings window should close
    And the config should NOT be changed
    When the user clicks "Nein"
    Then the confirmation dialog should close
    And the Settings window should remain open

  @Integration @CanRunInClaudeCode
  Scenario: Save button disabled with validation errors
    Given the Settings window is open
    When the user selects an invalid data root path
    Then the field should show a red error: "⚠ Pfad nicht gefunden"
    And the Save button should be disabled
    When the user fixes the validation error
    Then the Save button should be enabled
```

## US-056: Settings - Validation and Error Handling

```gherkin
@Iter-6 @FR-020 @Priority-High
Feature: Settings Validation
  As a user
  I want validation feedback
  So that I don't save invalid settings

  @WindowsOnly @Manual
  Scenario: Invalid data root path validation
    Given the Settings window is open
    When the user clicks "Durchsuchen"
    And selects a folder without valid structure (no config/, models/ subdirectories)
    Then an error should display: "Dieser Ordner enthält keine gültige LocalWhisper-Installation"
    And the path should not be updated
    And the Save button should remain disabled

  @WindowsOnly @Manual
  Scenario: Data root validation on browse
    Given the Settings window is open
    When the user clicks "Durchsuchen"
    And selects a folder with valid structure
    Then the path should update immediately
    And no error should be shown
    And the Save button should be enabled (change detected)

  @Integration @CanRunInClaudeCode
  Scenario: Config save failure handling
    Given the Settings window is open
    And the user makes valid changes
    When the user clicks "Speichern"
    And writing to config.toml fails (permissions error)
    Then an error dialog should appear: "Fehler beim Speichern"
    And the error message should be displayed
    And the Settings window should remain open
    And the user can retry or cancel
```

## US-057: Settings - Hotkey Capture Enhancement

```gherkin
@Iter-6 @Enhancement @FR-010 @Priority-Medium
Feature: In-Place Hotkey Capture
  As a user
  I want to capture my hotkey directly in the field
  So that I can quickly change it without dialogs

  Background:
    Given the Settings window is open
    And the current hotkey is "Ctrl+Shift+D"

  @WindowsOnly @Manual
  Scenario: User captures new hotkey in-place
    When the user clicks "Ändern..." button
    Then the hotkey field should enter capture mode
    And show placeholder "Drücke Tastenkombination..." in gray
    And the background should change to light yellow
    When the user presses "Ctrl"
    Then the field should display "Ctrl" in real-time
    When the user presses "Shift" (while holding Ctrl)
    Then the field should display "Ctrl+Shift" in real-time
    When the user presses "V" (while holding Ctrl+Shift)
    Then the field should display "Ctrl+Shift+V"
    And exit capture mode automatically (white background)
    And the Save button should be enabled

  @WindowsOnly @Manual
  Scenario: User tries to capture forbidden system hotkey
    Given the hotkey field is in capture mode
    When the user presses "Ctrl+Alt+Del"
    Then a warning should appear "⚠ Hotkey bereits belegt durch Systemfunktion"
    And the field should remain in capture mode (allow retry)
    And the Save button should remain enabled (warning, not error)

  @WindowsOnly @Manual
  Scenario: User cancels hotkey capture with Esc
    Given the hotkey field is in capture mode
    And the placeholder shows "Drücke Tastenkombination..."
    When the user presses Esc
    Then the field should exit capture mode
    And display the original hotkey "Ctrl+Shift+D"
    And the background should return to white

  @WindowsOnly @Manual
  Scenario: User tries invalid hotkey (no modifier)
    Given the hotkey field is in capture mode
    When the user presses just "D" (no modifiers)
    Then the keypress should be ignored
    And the field should remain in capture mode
    And no text should be displayed (still shows placeholder)

  @Integration @CanRunInClaudeCode
  Scenario: Hotkey conflict detection immediate
    Given the hotkey field is in capture mode
    When the user captures "Ctrl+C" (common conflict)
    Then conflict detection should run immediately
    And show warning "⚠ Hotkey bereits belegt durch andere Anwendung"
    And the field should exit capture mode with new hotkey displayed
    And the user can try again by clicking "Ändern..."
```

## US-058: Settings - SHA-1 Model Verification Enhancement

```gherkin
@Iter-6 @Enhancement @FR-017 @Priority-Low
Feature: Background SHA-1 Hash Verification
  As a user
  I want my model file verified in the background
  So that I know it's not corrupted

  Background:
    Given the Settings window is open
    And a model file "ggml-small.bin" (461 MB) is configured

  @WindowsOnly @Manual
  Scenario: User verifies model file integrity
    When the user clicks "Prüfen"
    Then the button should be disabled
    And the status should show "⏳ Verifiziere Modell..."
    And the status text should be gray
    When verification completes after ~8 seconds
    Then the status should show "✓ Modell OK"
    And the status text should be green
    And the button should be re-enabled

  @WindowsOnly @Manual
  Scenario: Model verification fails (file not found)
    Given the model file has been deleted
    When the user clicks "Prüfen"
    Then the status should show "⏳ Verifiziere Modell..."
    When verification completes
    Then the status should show "⚠ Modell nicht gefunden oder beschädigt"
    And the status text should be red
    And the button should be re-enabled

  @WindowsOnly @Manual
  Scenario: Model auto-verified when changed
    When the user clicks "Ändern..." button
    And selects a new model file "ggml-medium.bin"
    Then verification should start automatically
    And the status should show "⏳ Verifiziere Modell..."
    When verification completes successfully
    Then the status should show "✓ Modell OK"
    And the Save button should be enabled (change detected)

  @Integration @CanRunInClaudeCode
  Scenario: Verification runs asynchronously (UI responsive)
    Given verification is in progress
    When the user moves the Settings window
    Then the window should move smoothly (no freeze)
    And the status should continue showing "⏳ Verifiziere Modell..."
```

## US-059: Settings - Keyboard Shortcuts Enhancement

```gherkin
@Iter-6 @Enhancement @UX @Priority-Low
Feature: Keyboard Shortcuts for Settings Window
  As a power user
  I want keyboard shortcuts
  So that I can navigate efficiently

  Background:
    Given the Settings window is open

  @WindowsOnly @Manual
  Scenario: User saves settings with Enter key
    Given the user has changed the language to "English"
    And the Save button is enabled
    When the user presses Enter
    Then the Save button should be triggered
    And the config should be saved
    And the restart dialog should appear

  @WindowsOnly @Manual
  Scenario: Enter does nothing when Save disabled
    Given the user has made no changes
    And the Save button is disabled
    When the user presses Enter
    Then nothing should happen (no error)
    And the Settings window should remain open

  @WindowsOnly @Manual
  Scenario: User cancels settings with Esc key
    Given the user has changed file format to ".txt"
    When the user presses Esc
    Then a confirmation dialog should appear: "Änderungen verwerfen?"
    When the user confirms "Ja"
    Then the Settings window should close
    And the config should NOT be changed

  @WindowsOnly @Manual
  Scenario: Esc closes immediately with no changes
    Given the user has made no changes
    When the user presses Esc
    Then the Settings window should close immediately
    And no confirmation dialog should appear

  @WindowsOnly @Manual
  Scenario: Alt+S triggers Save button
    Given the user has made changes
    When the user presses Alt+S
    Then the Save button should be activated
    And the save process should start

  @WindowsOnly @Manual
  Scenario: Alt+A triggers Cancel button
    Given the user has made changes
    When the user presses Alt+A
    Then the Cancel button should be activated
    And the confirmation dialog should appear

  @WindowsOnly @Manual
  Scenario: Alt+D triggers Browse data root
    When the user presses Alt+D
    Then the folder browser dialog should open
    And the user can select a new data root

  @WindowsOnly @Manual
  Scenario: Alt+P triggers Model verification
    Given a model is configured
    When the user presses Alt+P
    Then the "Prüfen" button should be activated
    And verification should start
```

---

# ITERATION 7: Optional Post-Processing

## US-060: Post-Processing Enable/Disable

```gherkin
@Iter-7 @FR-022 @Priority-Low
Feature: Optional Post-Processing
  As a user
  I want to optionally improve transcription formatting
  So that my text is more readable

  Background:
    Given the Settings window is open

  @WindowsOnly @Manual
  Scenario: User enables post-processing
    When the user checks "Post-Processing aktivieren"
    And provides LLM CLI path "C:\Tools\llama-cli.exe"
    And clicks "Speichern"
    Then the config should be updated
    And the next dictation should run post-processing
    And no restart should be required

  @Integration @CanRunInClaudeCode
  Scenario: Post-processing improves formatting
    Given post-processing is enabled
    And the STT result is "lets meet at 3pm asap fyi"
    When post-processing runs
    Then the output should be "Let's meet at 3pm, as soon as possible. For your information."
    And the post-processed text should be written to clipboard and history
```

## US-061: Post-Processing Fallback on Error

```gherkin
@Iter-7 @FR-022 @Priority-High
Feature: Post-Processing Error Fallback
  As a user
  I want my transcript even if post-processing fails
  So that I don't lose data

  @Integration @CanRunInClaudeCode
  Scenario: LLM CLI fails, fallback to original text
    Given post-processing is enabled
    And the STT result is "This is the original text"
    When the LLM CLI exits with code 1 (error)
    Then the original STT text should be used
    And clipboard should contain "This is the original text"
    And a warning flyout should show "Post-Processing fehlgeschlagen (Original-Text verwendet)"
    And the error should be logged

  @Integration @CanRunInClaudeCode
  Scenario: Post-processing timeout (10 seconds)
    Given post-processing is enabled
    When the LLM CLI runs for 10 seconds without completing
    Then the process should be killed
    And the original STT text should be used (fallback)
```

## US-062: Post-Processing "Do Not Change Meaning" Contract

```gherkin
@Iter-7 @FR-022 @Priority-High
Feature: Post-Processing Meaning Preservation
  As a user
  I want post-processing to format, not alter meaning
  So that my transcript remains accurate

  @Contract @CanRunInClaudeCode
  Scenario: Post-processing preserves meaning
    Given the prompt to LLM is: "Format lists, apply glossary, fix punctuation. DO NOT change meaning."
    And the input is "meeting 3pm discuss budget"
    When post-processing runs
    Then the output should have correct capitalization and punctuation
    But the semantic meaning should remain the same
    And no hallucination should occur (e.g., no invented details like "meeting at headquarters")

  @Manual
  Scenario: User verifies post-processing output
    Given post-processing is enabled
    When the user completes 10 test dictations
    Then the user should review post-processed vs. original text
    And confirm meaning was preserved in all cases
```

## US-063: Glossary Support

```gherkin
@Iter-7 @FR-022 @Priority-Medium
Feature: Custom Glossary for Post-Processing
  As a user with domain-specific abbreviations
  I want to define custom expansions
  So that post-processing understands my terminology

  Background:
    Given post-processing is enabled
    And glossary is enabled in config

  @Integration @CanRunInClaudeCode
  Scenario: Glossary expands abbreviations
    Given the glossary file at "<DATA_ROOT>/config/glossary.txt" contains:
      """
      asap = as soon as possible
      fyi = for your information
      imho = in my humble opinion
      """
    And the STT result is "please reply asap fyi"
    When post-processing runs
    Then the LLM prompt should include the glossary content
    And the output should contain "as soon as possible"
    And the output should contain "for your information"

  @Unit @CanRunInClaudeCode
  Scenario: Glossary is appended to system prompt
    Given the glossary file contains 3 entries
    When building the LLM prompt
    Then the system prompt should end with "\n\nAPPLY THESE ABBREVIATIONS:\nasap = as soon as possible\nfyi = for your information\nimho = in my humble opinion"

  @Manual
  Scenario: User creates custom glossary
    Given the Settings window is open
    And post-processing is enabled
    When the user checks "Benutzerdefiniertes Glossar verwenden"
    And browses to a custom glossary file
    And clicks "Speichern"
    Then the config should have postprocessing.use_glossary = true
    And the next dictation should use the glossary

  @Integration @CanRunInClaudeCode
  Scenario: Invalid glossary format is ignored
    Given the glossary file contains invalid syntax (no = sign)
    When post-processing runs
    Then a warning should be logged "Glossary format invalid, skipping"
    And post-processing should proceed without the glossary
    And no error should occur

  @Integration @CanRunInClaudeCode
  Scenario: Large glossary is truncated
    Given the glossary file contains 600 entries
    When loading the glossary
    Then only the first 500 entries should be loaded
    And a warning should be logged "Glossary truncated to 500 entries"
```

## US-064: Wizard - Post-Processing Setup

```gherkin
@Iter-5 @Iter-7 @FR-022 @Priority-High
Feature: First-Run Wizard - Post-Processing Setup
  As a new user
  I want to enable post-processing during setup
  So that I get the best experience from the start

  Background:
    Given this is the first run (no config exists)
    And the wizard is on Step 3 (Post-Processing Setup)

  @WindowsOnly @Manual
  Scenario: User enables post-processing in wizard (default behavior)
    Given the "Enable Post-Processing" checkbox is checked by default
    And the explanation text describes the feature
    When the user clicks "Next"
    Then Llama 3.2 3B model should be queued for download (~2GB)
    And llama-cli.exe should be queued for download
    And the wizard should proceed to Step 4 (Hotkey Selection)

  @WindowsOnly @Manual
  Scenario: Download progress shows both models
    Given the user enabled post-processing in wizard
    And Step 3 is complete
    When downloading models after the wizard
    Then progress should show: "Downloading Whisper model..."
    And then: "Downloading Llama model..."
    Or combined: "Downloading models (2/2)..."
    And total size should reflect both models (~3.5GB)

  @WindowsOnly @Manual
  Scenario: User skips post-processing in wizard
    Given the "Enable Post-Processing" checkbox is checked
    When the user unchecks it
    And clicks "Next"
    Then NO Llama model download should be queued
    And NO llama-cli.exe download should occur
    And config should have postprocessing.enabled = false
    And the wizard should proceed to Step 4 (Hotkey Selection)
    And the user can enable post-processing later in Settings

  @Integration @CanRunInClaudeCode
  Scenario: Config reflects wizard choice (enabled)
    Given the user enabled post-processing in wizard
    When the wizard completes
    Then config.toml should contain:
      """
      [postprocessing]
      enabled = true
      llm_cli_path = "<DATA_ROOT>/bin/llama-cli.exe"
      model_path = "<DATA_ROOT>/models/llama-3.2-3b-q4.gguf"
      """

  @Integration @CanRunInClaudeCode
  Scenario: Config reflects wizard choice (disabled)
    Given the user disabled post-processing in wizard
    When the wizard completes
    Then config.toml should contain:
      """
      [postprocessing]
      enabled = false
      """
```

## US-065: Settings UI - Post-Processing Configuration (Simplified)

```gherkin
@Iter-7 @FR-022 @Priority-Low
Feature: Settings UI - Post-Processing File Path Recovery
  As a user who accidentally moved/deleted files after installation
  I want to fix post-processing paths in Settings
  So that I can restore functionality without reinstalling

  Background:
    Given the app is installed and configured
    And the Settings window is open

  @WindowsOnly @Manual
  Scenario: Browse for llama-cli.exe (simple file picker)
    Given I click "Durchsuchen" for llama-cli.exe path
    When the file dialog opens
    Then it should filter "All Files (*.*)"
    And start in the directory of the current path (or Documents if empty)
    When I select a file
    Then the textbox should update with the selected path
    And the Save button should be enabled

  @WindowsOnly @Manual
  Scenario: Browse for Llama model (simple file picker)
    Given I click "Durchsuchen" for Llama model path
    When the file dialog opens
    Then it should filter "All Files (*.*)"
    And start in the directory of the current path (or Documents if empty)
    When I select a file
    Then the textbox should update with the selected path
    And the Save button should be enabled

  @WindowsOnly @Manual
  Scenario: Browse for glossary (simple file picker)
    Given glossary is enabled
    And I click "Durchsuchen" for glossary path
    When the file dialog opens
    Then it should filter "All Files (*.*)"
    And start in the directory of the current path (or Documents if empty)
    When I select a file
    Then the textbox should update with the selected path
    And the Save button should be enabled

  @Integration @CanRunInClaudeCode
  Scenario: Save with valid paths (minimal validation)
    Given I have changed post-processing paths
    And all files exist (File.Exists returns true)
    When I click "Speichern"
    Then config.toml should be updated with new paths
    And the Settings window should close
    And no success message should be shown

  @Integration @CanRunInClaudeCode
  Scenario: Save with missing llama-cli.exe
    Given post-processing is enabled
    And llama-cli.exe path does not exist
    When I click "Speichern"
    Then a MessageBox should appear with:
      """
      Post-Processing Dateien fehlen oder ungültig.

      Bitte führen Sie den Ersteinrichtungs-Assistenten
      erneut aus oder installieren Sie die Anwendung neu.
      """
    And config should NOT be saved
    And the Settings window should remain open

  @Integration @CanRunInClaudeCode
  Scenario: Save with missing Llama model
    Given post-processing is enabled
    And Llama model path does not exist
    When I click "Speichern"
    Then the error MessageBox should appear
    And config should NOT be saved

  @Integration @CanRunInClaudeCode
  Scenario: Save with missing glossary (if enabled)
    Given post-processing is enabled
    And glossary is enabled
    And glossary path does not exist
    When I click "Speichern"
    Then the error MessageBox should appear
    And config should NOT be saved

  @Integration @CanRunInClaudeCode
  Scenario: Glossary validation skipped if disabled
    Given post-processing is enabled
    And glossary is disabled (checkbox unchecked)
    And glossary path does not exist
    When I click "Speichern"
    Then validation should NOT check glossary path
    And config should be saved successfully

  @Unit @CanRunInClaudeCode
  Scenario: Text change marks form as dirty
    Given the Settings window is open
    When I manually type in any path textbox
    Then the Save button should be enabled

  @Manual @WindowsOnly
  Scenario: All controls always enabled (no cascading disables)
    Given the Settings window is open
    When I uncheck "Post-Processing aktivieren"
    Then all path textboxes should remain enabled
    And all browse buttons should remain enabled
    And glossary controls should remain enabled
```

**Implementation Notes:**
- File dialogs: OpenFileDialog, filter "All Files (*.*)" only
- Validation: File.Exists() only, no extension checks, no execution tests
- No download buttons (wizard-only feature)
- No hot reload (config applied on app restart)
- No inline validation (only on Save)
- No "restart required" message (implicit)
- No success message on Save (just close window)
- Keep it simple: Settings is a "last resort fix tool" for users who moved files

---

# ITERATION 8: Stabilization + Reset + Logs

## US-070: Reset/Uninstall

```gherkin
@Iter-8 @UC-004 @FR-019 @Priority-Medium
Feature: Reset and Uninstall
  As a user
  I want to cleanly remove the app and its data
  So that I can reinstall or free up space

  Background:
    Given the app is running

  @WindowsOnly @Manual
  Scenario: User resets all data
    When the user right-clicks the tray icon
    And selects "Zurücksetzen/Deinstallieren..."
    And a confirmation dialog appears
    And the user clicks "Alles löschen"
    Then the entire data root folder should be deleted
    And a final message should display "Daten gelöscht. Bitte löschen Sie die EXE-Datei manuell."
    And the app should exit
    And no app folders should remain under the data root

  @WindowsOnly @Manual
  Scenario: User resets settings only (keeps history)
    When the user selects "Zurücksetzen/Deinstallieren..."
    And clicks "Nur Einstellungen löschen"
    Then the config/ and logs/ folders should be deleted
    But the history/ folder should remain
    And the app should exit
```

## US-071: Comprehensive Error Handling

```gherkin
@Iter-8 @FR-021 @NFR-003 @Priority-High
Feature: Comprehensive Error Handling
  As a user
  I want the app to handle all errors gracefully
  So that I never lose data or experience crashes

  @WindowsOnly @Manual
  Scenario Outline: Error matrix tests
    Given the error scenario "<scenario>" occurs
    When the app handles the error
    Then a user-friendly dialog should appear
    And the app should remain stable (no crash)
    And the error should be logged with context

    Examples:
      | scenario |
      | Microphone denied |
      | Model hash mismatch |
      | Hotkey conflict |
      | Disk full (history write) |
      | STT timeout |
      | Data root moved |
      | Model file missing |
```

## US-072: Logging Hardening (NFR-006)

```gherkin
@Iter-8 @FR-023 @NFR-006 @Priority-Medium
Feature: Logging Infrastructure
  As a developer/support
  I need comprehensive, structured logs
  So that I can diagnose any issue

  @Integration @CanRunInClaudeCode
  Scenario: All key events are logged
    Given the app runs through a complete dictation flow
    Then the log should contain structured entries for:
      | Event | Required Fields |
      | App start | Version, OS, DataRoot |
      | Hotkey registration | Modifiers, Key, Success/Failure |
      | State transitions | From, To, Timestamp |
      | Audio recording | FilePath, Duration, FileSize |
      | STT invocation | Command, Duration, ExitCode, TranscriptLength |
      | Clipboard write | TextLength, Success/Failure |
      | History write | FilePath, Success/Failure |
      | Errors | Exception type, Message, Stack trace (if available) |

  @Integration @CanRunInClaudeCode
  Scenario: Log rotation works
    Given the log file reaches 10 MB
    When a new log entry is written
    Then the log file should be renamed to app.log.1
    And a new app.log should be created
    And old logs (app.log.5) should be deleted
```

## US-073: Final Performance Verification (NFR-001, NFR-004)

```gherkin
@Iter-8 @NFR-001 @NFR-004 @Priority-High
Feature: Final Performance Verification
  As a project owner
  I want to verify all performance targets are met
  So that v0.1 can be released

  @WindowsOnly @Manual
  Scenario: p95 latency target met
    Given 100 test dictations have been performed
    When latency statistics are calculated
    Then p95 latency should be ≤ 2.5 seconds
    And results should be documented in changelog

  @WindowsOnly @Manual
  Scenario: Flyout latency target met
    Given 50 test dictations have been performed
    When flyout latency is measured
    Then average latency should be ≤ 0.5 seconds
```

## US-074: README and Documentation

```gherkin
@Iter-8 @Priority-Low
Feature: User-Facing Documentation
  As a new user
  I want clear installation and usage instructions
  So that I can get started quickly

  @Manual
  Scenario: README is complete
    Given the README.md file
    Then it should contain:
      | Section |
      | Installation instructions |
      | First-run wizard guide |
      | Hotkey usage instructions |
      | Troubleshooting (SmartScreen, microphone permissions) |
      | Known limitations (v0.1 scope) |
```

## US-075: Changelog and Release

```gherkin
@Iter-8 @Priority-Low
Feature: Release Preparation
  As a project owner
  I want a clear changelog
  So that users know what's included

  @Manual
  Scenario: Changelog is finalized
    Given all 8 iterations are complete
    Then the changelog should document:
      | Section |
      | All implemented features (FRs) |
      | Performance metrics (p95 latency, etc.) |
      | Known issues |
      | Out-of-scope features (deferred to v0.2) |

  @Manual
  Scenario: Git tag is created
    When the release is ready
    Then a git tag "v0.1.0" should be created
    And the commit should reference all completed iterations
```

## US-076: PR Template and House Rules

```gherkin
@Iter-8 @Priority-Low
Feature: Development Workflow Documentation
  As a developer
  I want clear contribution guidelines
  So that future work follows established patterns

  @Manual
  Scenario: PR template exists
    Given a new pull request is created
    Then the template should require:
      | Field |
      | User story IDs (US-###) |
      | Functional requirements (FR-###) |
      | Test coverage (BDD scenarios passing) |
      | Definition of Done checklist |
```

---

## Implementation Decisions Finalized

All open questions have been resolved with the following decisions:

1. **Error Dialog Messages:**
   - Microphone unavailable: "Mikrofon nicht verfügbar. Bitte überprüfen Sie die Berechtigungen und Geräteverbindung."
   - Hotkey conflict: "Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination in den Einstellungen."

2. **Wizard Completion Definition:**
   - Completion measured when user clicks "Fertig" (excludes model download time)

3. **Settings Restart Behavior:**
   - Hotkey change: Requires restart ✓
   - Data Root change: Requires restart ✓
   - File Format change: Immediate (no restart) ✓

4. **Model Download Source:**
   - Hugging Face: `https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin`

5. **Data Root Migration:**
   - User chooses via dialog: "Verschieben?" or "Nur Pfad ändern?"
   - Both options supported

---

**Last Updated:** 2025-11-17
**Status:** ✅ Complete — Ready for TDD Implementation
**Total Scenarios:** 60+ executable acceptance criteria
**Coverage:** All 8 iterations (US-001 through US-076)
