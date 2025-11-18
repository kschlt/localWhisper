# Iteration 7: Post-Processing BDD Scenarios

**Related:** US-060, US-061, US-062, FR-022
**Status:** 📋 Specification Complete
**Last Updated:** 2025-11-18

---

## US-060: Post-Processing Enable/Disable (Extended)

```gherkin
@Iter-7 @FR-022 @Priority-Medium
Feature: Optional Post-Processing with LLM
  As a user
  I want to optionally improve transcription formatting using a local LLM
  So that my text is more readable without losing accuracy

  Background:
    Given the app is running
    And llama.cpp is installed at "<APP_DIR>\llama-cli.exe"
    And the data root is "C:\Users\Test\LocalWhisper"

  @Acceptance
  Scenario: Enable post-processing in Settings
    Given the Settings window is open
    When the user checks "Post-Processing aktivieren"
    Then the model download dialog should appear
    And the app should download "llama-3.2-3b-q4.gguf" from Hugging Face
    And the model should be saved to "<DATA_ROOT>\models\llama-3.2-3b-q4.gguf"
    And the SHA-256 hash should be verified
    And post-processing should be enabled in config
    And no restart should be required

  @Acceptance
  Scenario: Post-processing improves formatting (Plain Text mode)
    Given post-processing is enabled
    And the STT result is "lets meet at 3pm asap fyi we should discuss the budget and timeline"
    When the dictation completes
    Then the post-processor should detect Plain Text mode (no trigger)
    And the LLM should be invoked with the Plain Text prompt
    And the output should be improved:
      """
      Let's meet at 3pm, as soon as possible. For your information, we should discuss the budget and timeline.
      """
    And the formatted text should be written to clipboard
    And the formatted text should be saved to history
    And the flyout should show "✓ Transkription formatiert"

  @Acceptance
  Scenario: Post-processing with Markdown mode trigger (start)
    Given post-processing is enabled
    And the STT result is "markdown mode okay I want three sections first what the feature does second why its useful and third what the limitations are"
    When the dictation completes
    Then the post-processor should detect Markdown mode (trigger at start)
    And the trigger phrase "markdown mode" should be stripped
    And the LLM should be invoked with the Markdown prompt
    And the output should include Markdown headings:
      """
      ## What the feature does

      ...

      ## Why it's useful

      ...

      ## Limitations

      ...
      """
    And the formatted text should be written to clipboard
    And the flyout should show "✓ Transkription formatiert (Markdown-Modus)"

  @Acceptance
  Scenario: Post-processing with Markdown mode trigger (end)
    Given post-processing is enabled
    And the STT result is "here are the three main benefits first its fast second its accurate and third its private markdown mode"
    When the dictation completes
    Then the post-processor should detect Markdown mode (trigger at end)
    And the trigger phrase "markdown mode" should be stripped from the end
    And the LLM should be invoked with the Markdown prompt
    And the output should be formatted with lists

  @Acceptance
  Scenario: No Markdown trigger - uses Plain Text mode
    Given post-processing is enabled
    And the STT result is "lets markdown this document in a different mode of thinking"
    When the dictation completes
    Then the post-processor should detect Plain Text mode (no exact phrase)
    And the LLM should be invoked with the Plain Text prompt
    And the output should NOT include Markdown headings

  @Performance
  Scenario: Post-processing completes within 1 second (GPU)
    Given post-processing is enabled
    And a GPU is available (CUDA or DirectML)
    And the STT result is 50 words long
    When the dictation completes
    Then the post-processing should complete within 1000ms
    And the total latency (STT + post-processing) should be logged
    And the p95 latency should be <2.5s (including STT)

  @Performance
  Scenario: Post-processing completes within 2 seconds (CPU fallback)
    Given post-processing is enabled
    And no GPU is available
    And the STT result is 50 words long
    When the dictation completes
    Then the post-processing should complete within 2000ms
    And the CPU fallback should be logged
    And the user should see normal completion flyout

  @Settings
  Scenario: Model already downloaded - no re-download
    Given post-processing was previously enabled
    And the model file exists at "<DATA_ROOT>\models\llama-3.2-3b-q4.gguf"
    When the user opens Settings
    And checks "Post-Processing aktivieren"
    Then no download dialog should appear
    And the setting should be saved immediately
```

---

## US-061: Post-Processing Fallback on Error (Extended)

```gherkin
@Iter-7 @FR-022 @Priority-High
Feature: Post-Processing Error Fallback
  As a user
  I want my transcript even if post-processing fails
  So that I don't lose data

  Background:
    Given post-processing is enabled
    And the STT result is "This is the original text from Whisper"

  @Critical
  Scenario: LLM CLI not found - fallback to original
    Given llama-cli.exe does not exist
    When the dictation completes
    Then the post-processor should log an error: "LLM CLI not found"
    And the original STT text should be used
    And clipboard should contain "This is the original text from Whisper"
    And the flyout should show "⚠ Post-Processing fehlgeschlagen (Original-Text verwendet)"

  @Critical
  Scenario: Model file not found - fallback to original
    Given the model file does not exist at "<DATA_ROOT>\models\llama-3.2-3b-q4.gguf"
    When the dictation completes
    Then the post-processor should log an error: "Model file not found"
    And the original STT text should be used
    And the flyout should show warning

  @Critical
  Scenario: LLM process exits with error code
    Given llama-cli.exe exits with code 1
    And stderr contains "Failed to load model"
    When the dictation completes
    Then the post-processor should detect the error
    And the original STT text should be used
    And the error should be logged with stderr output
    And the flyout should show warning

  @Critical
  Scenario: LLM output is empty
    Given llama-cli.exe runs successfully (exit code 0)
    But stdout is empty
    When the dictation completes
    Then the post-processor should detect invalid output
    And the original STT text should be used
    And the error should be logged: "Empty LLM output"

  @Critical
  Scenario: Post-processing timeout (5 seconds)
    Given llama-cli.exe runs for 5 seconds without completing
    When the timeout is reached
    Then the process should be killed
    And the original STT text should be used
    And the error should be logged: "Post-processing timeout (5s)"
    And the flyout should show warning

  @Critical
  Scenario: GPU out of memory - fallback to CPU
    Given GPU is available but has insufficient memory
    When the LLM invocation fails with GPU error
    Then the post-processor should retry with CPU-only mode
    And the fallback should be logged
    And if CPU succeeds, the formatted text should be used
    And if CPU also fails, the original text should be used

  @Critical
  Scenario: Consecutive failures - suggest disabling
    Given post-processing has failed 3 times in a row
    When the next failure occurs
    Then the flyout should show enhanced warning:
      """
      ⚠ Post-Processing wiederholt fehlgeschlagen
      (Original-Text verwendet)

      Erwägen Sie, Post-Processing in den Einstellungen zu deaktivieren.
      """
    And the error count should be logged
```

---

## US-062: Meaning Preservation Contract (Extended)

```gherkin
@Iter-7 @FR-022 @Priority-High
Feature: Post-Processing Meaning Preservation
  As a user
  I want post-processing to format, not alter meaning
  So that my transcript remains accurate

  Background:
    Given post-processing is enabled

  @Contract
  Scenario: Prompt enforces "Do not change meaning"
    Given the Plain Text prompt contains:
      """
      DON'T:
      - Don't add new ideas or explanations.
      - Don't change meaning.
      - Don't summarize or shorten.
      - Don't change technical terms or names.
      """
    When the post-processor builds the LLM invocation
    Then the full prompt should be sent to stdin
    And the system message should be included

  @Validation
  Scenario: Technical terms are preserved
    Given the STT result is "deploy the kubernetes cluster with helm and terraform"
    When post-processing runs
    Then the output should preserve "kubernetes", "helm", "terraform" exactly
    And capitalization may be corrected ("Kubernetes")
    But the terms should not be replaced or expanded

  @Validation
  Scenario: Names are preserved
    Given the STT result is "call john smith at anthropic about claude api"
    When post-processing runs
    Then the output should preserve "John Smith", "Anthropic", "Claude API"
    And no additional context should be added

  @Validation
  Scenario: Numbers and dates are preserved
    Given the STT result is "meeting on november 18th at 3pm budget is 50000 dollars"
    When post-processing runs
    Then the output should preserve "November 18th", "3pm", "50000 dollars"
    And no conversions should occur (e.g., no "18.11.2025" or "$50,000")

  @Validation
  Scenario: Filler words removed only when safe
    Given the STT result is "um I think we should uh maybe like consider this option you know"
    When post-processing runs
    Then "um", "uh", "you know" should be removed
    But "maybe" should be kept (affects meaning)
    And "like" should be removed only if used as filler (not "like this")

  @Validation
  Scenario: Meaning preserved in edge cases
    Given the STT result is "dont do that not that do this"
    When post-processing runs
    Then negations should be preserved: "don't", "not"
    And the semantic structure should remain the same

  @Validation
  Scenario: No hallucination - no invented details
    Given the STT result is "meeting at 3pm to discuss budget"
    When post-processing runs
    Then the output should NOT add invented details like:
      - "meeting at headquarters" (location not mentioned)
      - "discuss Q4 budget" (Q4 not mentioned)
      - "with the team" (team not mentioned)
    And only the mentioned facts should appear
```

---

## Additional Scenarios: Model Download & Setup

```gherkin
@Iter-7 @FR-022 @Setup
Feature: LLM Model Download and Setup
  As a user
  I want the LLM model to be downloaded automatically
  So that I don't have to manually configure it

  @Acceptance
  Scenario: Auto-download on first enable
    Given post-processing has never been enabled
    And the model file does not exist
    When the user enables post-processing in Settings
    Then a download dialog should appear:
      """
      LLM-Modell herunterladen

      LocalWhisper benötigt ein Sprachmodell für Post-Processing:
      - Name: Llama 3.2 3B Instruct
      - Größe: ~2 GB
      - Quelle: Hugging Face

      [Herunterladen] [Abbrechen] [Manuell...]
      """
    When the user clicks "Herunterladen"
    Then the model should download with progress bar
    And the SHA-256 hash should be verified after download
    And the setting should be enabled on success

  @Acceptance
  Scenario: Download failure - show manual instructions
    Given the download fails (network error)
    When the error occurs
    Then the dialog should show manual instructions:
      """
      ⚠ Automatischer Download fehlgeschlagen

      Manuelle Installation:
      1. Download von: https://huggingface.co/...
      2. Speichern als: <DATA_ROOT>\models\llama-3.2-3b-q4.gguf
      3. Post-Processing in Einstellungen aktivieren

      [Erneut versuchen] [Abbrechen]
      """

  @Acceptance
  Scenario: SHA-256 verification failure
    Given the model downloads successfully
    But the SHA-256 hash does not match expected value
    When verification runs
    Then the downloaded file should be deleted
    And an error dialog should show: "Datei beschädigt. Bitte erneut herunterladen."
    And the user should be prompted to retry

  @Acceptance
  Scenario: llama.cpp CLI not found - show installation guide
    Given the model file exists
    But llama-cli.exe is not found in expected locations
    When the user enables post-processing
    Then a dialog should show installation instructions:
      """
      ⚠ llama.cpp nicht gefunden

      Bitte installieren Sie llama.cpp:
      1. Download von: https://github.com/ggerganov/llama.cpp/releases
      2. Extrahieren nach: <APP_DIR>
      3. Datei: llama-cli.exe

      [Pfad manuell wählen] [Abbrechen]
      """
```

---

## Additional Scenarios: GPU Detection & Fallback

```gherkin
@Iter-7 @FR-022 @Performance
Feature: GPU Detection and CPU Fallback
  As a user with a GPU
  I want post-processing to use my GPU for speed
  But fall back to CPU if GPU fails

  @Acceptance
  Scenario: GPU detected and used successfully
    Given an NVIDIA GPU with CUDA is available
    When post-processing runs for the first time
    Then llama-cli.exe should be invoked with "--n-gpu-layers 99"
    And the inference should complete in <500ms
    And the log should show "Post-processing: GPU accelerated (CUDA)"

  @Acceptance
  Scenario: AMD GPU detected and used with DirectML
    Given an AMD GPU is available
    And DirectML is supported
    When post-processing runs
    Then llama-cli.exe should be invoked with DirectML support
    And the inference should complete faster than CPU-only
    And the log should show "Post-processing: GPU accelerated (DirectML)"

  @Acceptance
  Scenario: GPU fails - automatic CPU fallback
    Given a GPU is available
    But the GPU fails with "Out of memory"
    When post-processing runs
    Then the app should retry with "--n-gpu-layers 0" (CPU-only)
    And the inference should complete in <2s (CPU speed)
    And the log should show "Post-processing: GPU failed, using CPU fallback"
    And the user should see normal completion (no error flyout)

  @Acceptance
  Scenario: No GPU - CPU-only from start
    Given no GPU is available
    When post-processing runs
    Then llama-cli.exe should be invoked with "--n-gpu-layers 0"
    And the inference should complete in <2s
    And the log should show "Post-processing: CPU-only (no GPU detected)"
```

---

## Test Data Examples

### Plain Text Mode Examples

**Input:** "um so I think we should uh maybe consider meeting at 3pm to discuss the budget asap"

**Expected Output:**
```
I think we should maybe consider meeting at 3pm to discuss the budget as soon as possible.
```

---

**Input:** "okay first we need to set up the environment then we install the dependencies and finally we run the tests"

**Expected Output:**
```
First, we need to set up the environment. Then we install the dependencies, and finally we run the tests.
```

Or as a list:
```
We need to:

- Set up the environment
- Install the dependencies
- Run the tests
```

---

### Markdown Mode Examples

**Input:** "markdown mode okay the benefits are first its fast second its accurate and third its private the limitations are it requires a good cpu and it needs 2gb of disk space"

**Expected Output (Markdown):**
```markdown
## Benefits

- It's fast
- It's accurate
- It's private

## Limitations

- It requires a good CPU
- It needs 2GB of disk space
```

---

**Last Updated:** 2025-11-18
**Status:** Ready for implementation
