# Iteration 7: Optional Post-Processing - IMPLEMENTATION DECISIONS

**Status:** ✅ Ready for Implementation
**Date:** 2025-11-18
**User Stories:** US-060, US-061, US-062, US-063 (new: glossary), US-064 (new: wizard integration)
**Functional Requirements:** FR-022
**Estimated Effort:** 6-8 hours (including glossary + wizard integration)

---

## Executive Summary

All critical decisions have been made. Implementation can proceed immediately.

**Key Decisions:**
1. ✅ Auto-download llama-cli.exe from GitHub releases on first use
2. ✅ Include glossary feature (simple to implement)
3. ✅ Settings UI: Enable + LLM path + Model path (standard level)
4. ✅ Add new `PostProcessing` state to state machine
5. ✅ **NEW:** Llama model download during first-run wizard (default: enabled)
6. ✅ Config schema extended with `[postprocessing]` section

---

## Decision Details

### 1. llama-cli.exe Distribution

**Decision:** Auto-download from llama.cpp GitHub releases on first use

**Implementation:**
- Download URL: `https://github.com/ggerganov/llama.cpp/releases/latest/download/llama-cli-win-x64.zip`
- Extract to: `<DATA_ROOT>/bin/llama-cli.exe`
- Verify checksum (if provided by llama.cpp releases)
- Fallback: Provide manual download instructions if auto-download fails

**Benefits:**
- Consistent with model download approach
- Always gets latest stable version
- No bundling in installer (keeps app size small)
- Easy to update (re-download)

**Code Impact:**
- New class: `LlamaCLIDownloader` (similar to `ModelDownloader`)
- Settings UI: "Download" button if not found
- First-use detection: Check if `<DATA_ROOT>/bin/llama-cli.exe` exists

---

### 2. Glossary Feature

**Decision:** Include in Iteration 7 (simple to implement)

**Implementation:**
- **File:** `<DATA_ROOT>/config/glossary.txt`
- **Format:**
  ```
  # LocalWhisper Glossary - Abbreviation expansion
  asap = as soon as possible
  fyi = for your information
  imho = in my humble opinion
  btw = by the way
  ```
- **Approach:** Include glossary content in LLM system prompt
  - Read glossary file (if exists)
  - Append to prompt: "\n\nAPPLY THESE ABBREVIATIONS:\n{glossary}"
  - Max size: 500 entries (prevent prompt overflow)

**Config:**
```toml
[postprocessing]
use_glossary = false  # default: disabled
glossary_path = "<DATA_ROOT>/config/glossary.txt"
```

**Settings UI:**
```
[ ] Benutzerdefiniertes Glossar verwenden
    Glossar-Pfad: [<DATA_ROOT>\config\glossary.txt] [Browse...]
```

**New User Story:** US-063 (Glossary Support)

**Estimated Effort:** +45 minutes

---

### 3. Settings UI

**Decision:** Standard level (Option B)

**UI Layout:**
```
┌─ Post-Processing ────────────────────────────────┐
│                                                   │
│  [ ] Post-Processing aktivieren                  │
│                                                   │
│  LLM CLI Pfad:                                    │
│  [C:\...\LocalWhisper\bin\llama-cli.exe] [Browse...] [Download]
│                                                   │
│  Modell-Pfad:                                     │
│  [C:\...\LocalWhisper\models\llama-3.2-...] [Browse...] [Download]
│                                                   │
│  [ ] GPU-Beschleunigung (auto-detect)            │
│  [ ] Benutzerdefiniertes Glossar verwenden       │
│      Glossar: [<DATA_ROOT>\config\glossary.txt] [Browse...]
│                                                   │
│  Timeout: [5] Sekunden                            │
│                                                   │
└───────────────────────────────────────────────────┘
```

**No exposed advanced params** (temp, top_p) - hardcoded for reliability

---

### 4. State Machine Integration

**Decision:** Add new `PostProcessing` state

**State Flow:**
```
Idle → Recording → Processing → PostProcessing → Idle
                                      ↓ (if disabled or error)
                                    Idle
```

**Benefits:**
- Clean separation of concerns
- Better logging ("PostProcessing started", "PostProcessing completed in 1.2s")
- Easier to debug/troubleshoot
- Tray icon can show distinct state (optional)

**Code Impact:**
- Update `AppState` enum: Add `PostProcessing`
- Update `StateMachine`: Add transition `Processing → PostProcessing`
- Update `App.xaml.cs`: Add post-processing logic in dictation flow
- Update `TrayIconManager`: Add icon/tooltip for `PostProcessing` state (optional)

---

### 5. LLM Model Download - Wizard Integration ⭐ NEW DECISION

**Decision:** Add Llama model download to first-run wizard (default: enabled)

**Wizard Flow Change:**
```
Step 1: Data Root Selection
Step 2: Whisper Model Selection
Step 3: [NEW] Post-Processing Setup  ← NEW STEP
        [ ] Enable Post-Processing (default: checked)
        Model: Llama 3.2 3B Instruct (~2GB)
        [Download automatically] [Skip for now]
Step 4: Hotkey Selection
```

**Behavior:**
- **Default:** Checkbox is CHECKED
- **If checked:** Download Llama model alongside Whisper model
  - Show combined progress: "Downloading models... (Whisper + Llama)"
  - Total download: ~1.5GB (Whisper) + ~2GB (Llama) = ~3.5GB
- **If unchecked:** Skip Llama download
  - User can enable and download later in Settings

**Why wizard integration:**
- Post-processing is a key feature, not an afterthought
- Default-enabled means most users get better experience
- One-time setup (no need to hunt for settings later)
- Consistent with "batteries included" philosophy

**Code Impact:**
- New wizard step: `PostProcessingStep.xaml` (simple checkbox + explanation)
- Update `WizardManager`: Add step 3
- Update `ModelDownloader`: Support downloading multiple models in parallel (or sequential)
- Update iteration-05 documentation: Add new step to wizard flow

**New User Story:** US-064 (Wizard: Post-Processing Setup)

**Estimated Effort:** +1.5 hours

---

### 6. Config Schema

**Decision:** Extend `config.toml` with `[postprocessing]` section

**Schema:**
```toml
[postprocessing]
enabled = false  # default: disabled (user must opt-in via wizard or settings)
llm_cli_path = "<DATA_ROOT>/bin/llama-cli.exe"
model_path = "<DATA_ROOT>/models/llama-3.2-3b-q4.gguf"
timeout_seconds = 5
gpu_acceleration = true  # auto-detect GPU, fallback to CPU
use_glossary = false
glossary_path = "<DATA_ROOT>/config/glossary.txt"

# Advanced (not exposed in UI, hardcoded for now)
temperature = 0.0
top_p = 0.25
repeat_penalty = 1.05
max_tokens = 512
```

**Validation:**
- `enabled`: bool
- `llm_cli_path`: Must be .exe file (Windows)
- `model_path`: Must be .gguf file
- `timeout_seconds`: 1-30 range
- `gpu_acceleration`: bool
- `use_glossary`: bool
- `glossary_path`: Must exist if `use_glossary = true`

**Code Impact:**
- Update `Models/AppConfig.cs`: Add `PostProcessingConfig` class
- Update `Core/ConfigManager.cs`: Parse/validate new section
- Default values provided if section missing (backward compatibility)

---

## Updated User Stories

### US-063: Glossary Support (NEW)

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
    Given the glossary file contains:
      """
      asap = as soon as possible
      fyi = for your information
      """
    And the STT result is "please reply asap fyi"
    When post-processing runs
    Then the output should contain "as soon as possible"
    And the output should contain "for your information"

  @Manual
  Scenario: Invalid glossary format is ignored
    Given the glossary file contains invalid syntax
    When post-processing runs
    Then the glossary should be ignored (logged as warning)
    And post-processing should proceed without errors
```

---

### US-064: Wizard - Post-Processing Setup (NEW)

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
  Scenario: User enables post-processing in wizard (default)
    Given the "Enable Post-Processing" checkbox is checked by default
    When the user clicks "Next"
    Then Llama 3.2 3B model should be downloaded (~2GB)
    And llama-cli.exe should be downloaded
    And config should have postprocessing.enabled = true
    And the wizard should proceed to Step 4 (Hotkey Selection)

  @WindowsOnly @Manual
  Scenario: User skips post-processing in wizard
    Given the "Enable Post-Processing" checkbox is checked
    When the user unchecks it
    And clicks "Next"
    Then NO Llama model should be downloaded
    And config should have postprocessing.enabled = false
    And the user can enable it later in Settings

  @Integration @CanRunInClaudeCode
  Scenario: Download progress shows both models
    Given the user enabled post-processing in wizard
    When downloading models
    Then progress should show: "Downloading models... (Whisper + Llama)"
    And total size should reflect both models (~3.5GB)
```

---

## Implementation Checklist

### Phase 1: Foundation (2-3h)
- [ ] Create `Models/PostProcessingConfig.cs`
- [ ] Update `Models/AppConfig.cs` to include `PostProcessingConfig`
- [ ] Update `Core/ConfigManager.cs` to parse `[postprocessing]` section
- [ ] Create `Services/LlamaCLIDownloader.cs` (download llama-cli.exe)
- [ ] Create `Services/LlmPostProcessor.cs` (main post-processing logic)
- [ ] Implement trigger word detection ("markdown mode")
- [ ] Load plain text / markdown prompts (embedded resources or config)

### Phase 2: Glossary (45min)
- [ ] Create `Services/GlossaryLoader.cs`
- [ ] Read and parse glossary.txt
- [ ] Append glossary to LLM prompt
- [ ] Add glossary validation (max 500 entries)

### Phase 3: State Machine (1h)
- [ ] Update `Models/AppState.cs`: Add `PostProcessing` enum value
- [ ] Update `Core/StateMachine.cs`: Add `Processing → PostProcessing` transition
- [ ] Update `App.xaml.cs`: Integrate post-processing into dictation flow
- [ ] Implement fallback logic (use original STT text on error)

### Phase 4: Wizard Integration (1.5h)
- [ ] Create `UI/Wizard/PostProcessingStep.xaml`
- [ ] Create `UI/Wizard/PostProcessingStep.xaml.cs`
- [ ] Update `UI/Wizard/WizardWindow.xaml.cs`: Add Step 3
- [ ] Update `Core/WizardManager.cs`: Handle post-processing step
- [ ] Update `Services/ModelDownloader.cs`: Support Llama model download

### Phase 5: Settings UI (1h)
- [ ] Update `UI/Settings/SettingsWindow.xaml`: Add post-processing section
- [ ] Update `UI/Settings/SettingsWindow.xaml.cs`: Wire up controls
- [ ] Add "Download" buttons for llama-cli.exe and Llama model
- [ ] Add glossary file browser
- [ ] Validate paths on save

### Phase 6: Testing (1h)
- [ ] Test plain text mode (default)
- [ ] Test markdown mode (trigger detection)
- [ ] Test glossary expansion
- [ ] Test fallback on LLM error
- [ ] Test timeout handling (5s)
- [ ] Measure latency (CPU vs GPU)
- [ ] Test wizard flow with post-processing enabled/disabled

### Phase 7: Documentation (30min)
- [ ] Update README: Post-processing setup instructions
- [ ] Document llama-cli.exe + Llama model download
- [ ] Add troubleshooting guide
- [ ] Update traceability matrix

**Total Estimated Effort:** 7-8 hours

---

## Performance Targets

- **Ideal:** <500ms (feels instant)
- **Target:** <1s (acceptable)
- **Acceptable:** <2s (noticeable but tolerable)
- **Timeout:** 5s (fallback to original text)

**Logging:**
```
[INFO] PostProcessor: Started (mode: PlainText, input_length: 87, gpu: true)
[INFO] PostProcessor: Completed in 0.734s (output_length: 92)
```

---

## Prompts (Embedded in Code)

See ADR-0010 for full prompt text. Summary:

**Plain Text Mode:**
- Fix grammar, punctuation, capitalization
- Add paragraph breaks
- Simple lists (`- item` or `1. item`)
- Remove filler words
- **No Markdown** headings/bold/italics

**Markdown Mode:**
- Same as plain text
- **Plus:** Markdown headings (`## Heading`)
- Triggered by "markdown mode" in transcript

---

## Next Steps

1. ✅ All decisions documented
2. ✅ Ready for implementation
3. → Commit documentation changes
4. → Begin implementation (TDD approach)

---

**Last Updated:** 2025-11-18
**Status:** ✅ READY FOR IMPLEMENTATION
