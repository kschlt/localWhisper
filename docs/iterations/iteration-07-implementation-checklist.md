# Iteration 7: Post-Processing Implementation Checklist

**Status:** 📋 Ready to Implement
**Estimated Effort:** 5-6 hours
**Prerequisites:** Iterations 1-4 complete (STT pipeline working)
**Last Updated:** 2025-11-18

---

## Pre-Implementation Checklist

- [ ] Read **ADR-0010** (LLM Post-Processing Architecture)
- [ ] Read **iteration-07-bdd-scenarios.md** (30+ test scenarios)
- [ ] Read **FR-022** (Functional Requirements)
- [ ] Review **US-060, US-061, US-062** (User Stories)
- [ ] Understand **trigger detection** algorithm (regex for "markdown mode")
- [ ] Understand **fallback strategy** (always preserve original text)

---

## Phase 1: Foundation (~2h)

### Task 1.1: Create `LlmPostProcessor` Service (~30min)

**File:** `src/LocalWhisper/Services/LlmPostProcessor.cs`

**Responsibilities:**
- Detect trigger words ("markdown mode" in first/last ~20 words)
- Build LLM CLI command (llama-cli.exe with parameters)
- Execute subprocess with timeout (5s)
- Parse output from stdout
- Handle errors and fallback

**Key Methods:**
```csharp
public class LlmPostProcessor
{
    public async Task<(bool Success, string Text)> ProcessAsync(string transcript, CancellationToken ct);
    private bool DetectMarkdownMode(string transcript, out string cleanedTranscript);
    private string BuildPlainTextPrompt(string transcript);
    private string BuildMarkdownPrompt(string transcript);
    private async Task<(int ExitCode, string Stdout, string Stderr)> InvokeLlmAsync(string prompt, CancellationToken ct);
}
```

**Acceptance Criteria:**
- [ ] Detects "markdown mode" in first/last 20 words (case-insensitive)
- [ ] Strips trigger phrase preserving punctuation
- [ ] Returns (Success=true, FormattedText) on success
- [ ] Returns (Success=false, OriginalText) on any error
- [ ] Logs all invocations with latency

**Tests:**
- [ ] Unit test: `DetectMarkdownMode_FirstWords_ReturnsTrue()`
- [ ] Unit test: `DetectMarkdownMode_LastWords_ReturnsTrue()`
- [ ] Unit test: `DetectMarkdownMode_NoTrigger_ReturnsFalse()`
- [ ] Unit test: `StripTrigger_PreservesPunctuation()`

---

### Task 1.2: Define Prompt Constants (~15min)

**File:** `src/LocalWhisper/Services/PromptTemplates.cs`

**Content:**
```csharp
public static class PromptTemplates
{
    public const string PlainTextSystem = "You are a careful transcript formatter...";
    public const string MarkdownSystem = "You are a formatter that converts...";

    public static string BuildPrompt(string systemPrompt, string userTranscript)
    {
        return $"System: {systemPrompt}\n\nUser: {userTranscript}\n\nAssistant:";
    }
}
```

**Acceptance Criteria:**
- [ ] Plain text prompt matches ADR-0010 exactly
- [ ] Markdown prompt matches ADR-0010 exactly
- [ ] BuildPrompt() formats correctly for llama-cli

**Tests:**
- [ ] Unit test: `BuildPrompt_FormatsCorrectly()`

---

### Task 1.3: Create `LlmCliAdapter` (~45min)

**File:** `src/LocalWhisper/Services/LlmCliAdapter.cs`

**Responsibilities:**
- Execute llama-cli.exe subprocess
- Pass prompt via stdin
- Read stdout/stderr
- Handle timeout (5s)
- GPU detection and fallback

**Key Methods:**
```csharp
public class LlmCliAdapter
{
    public async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string modelPath,
        string prompt,
        int timeoutMs = 5000,
        CancellationToken ct = default);

    private string BuildCommandLine(string modelPath, bool useGpu);
    private bool IsGpuAvailable();
}
```

**CLI Command:**
```bash
llama-cli.exe \
  --model "<modelPath>" \
  --prompt "<prompt>" \
  --n-predict 512 \
  --temp 0.0 \
  --top-p 0.25 \
  --repeat-penalty 1.05 \
  --threads <CPU_CORES> \
  --n-gpu-layers 99 \  # If GPU available
  --quiet
```

**Acceptance Criteria:**
- [ ] Executes llama-cli.exe with correct parameters
- [ ] Writes prompt to stdin
- [ ] Reads stdout and returns formatted text
- [ ] Times out after 5s and kills process
- [ ] Detects GPU (CUDA/DirectML) and uses `--n-gpu-layers 99`
- [ ] Falls back to CPU (`--n-gpu-layers 0`) if GPU fails
- [ ] Logs exit code, duration, GPU/CPU mode

**Tests:**
- [ ] Integration test: `RunAsync_ValidModel_ReturnsOutput()`
- [ ] Integration test: `RunAsync_Timeout_KillsProcess()`
- [ ] Unit test: `BuildCommandLine_GpuAvailable_IncludesGpuLayers()`
- [ ] Unit test: `BuildCommandLine_NoGpu_CpuOnly()`

---

## Phase 2: Integration with StateMachine (~1-2h)

### Task 2.1: Add `PostProcessing` State (~30min)

**File:** `src/LocalWhisper/Core/StateMachine.cs`

**Changes:**
```csharp
public enum AppState
{
    Idle,
    Recording,
    Processing,      // Whisper STT
    PostProcessing,  // NEW: LLM formatting
    Completing       // Clipboard + History + Flyout
}
```

**State Transition:**
```
Processing (STT complete)
  → PostProcessing (if enabled)
  → Completing (write to clipboard/history)
```

**Acceptance Criteria:**
- [ ] New `PostProcessing` state added
- [ ] Transition from `Processing` to `PostProcessing` (if enabled)
- [ ] Transition from `Processing` to `Completing` (if disabled)
- [ ] State logged with timestamps

**Tests:**
- [ ] Unit test: `Transition_ProcessingToPostProcessing_WhenEnabled()`
- [ ] Unit test: `Transition_ProcessingToCompleting_WhenDisabled()`

---

### Task 2.2: Wire PostProcessor into Dictation Flow (~1h)

**File:** `src/LocalWhisper/Core/StateMachine.cs` (or orchestrator)

**Logic:**
```csharp
private async Task OnProcessingComplete(string sttText)
{
    string finalText = sttText;

    if (_config.PostProcessing?.Enabled == true)
    {
        TransitionTo(AppState.PostProcessing);

        var (success, processedText) = await _postProcessor.ProcessAsync(sttText, _cts.Token);

        if (success)
        {
            finalText = processedText;
            _flyoutMessage = "✓ Transkription formatiert";
            AppLogger.LogInformation("Post-processing succeeded", new { OriginalLength = sttText.Length, ProcessedLength = processedText.Length });
        }
        else
        {
            // Fallback to original (error already logged in PostProcessor)
            finalText = sttText;
            _flyoutMessage = "⚠ Post-Processing fehlgeschlagen (Original-Text verwendet)";
        }
    }

    TransitionTo(AppState.Completing);
    await WriteToClipboardAndHistory(finalText);
    ShowFlyout(_flyoutMessage);
    TransitionTo(AppState.Idle);
}
```

**Acceptance Criteria:**
- [ ] Post-processing runs AFTER STT completes
- [ ] On success: Use formatted text
- [ ] On failure: Use original STT text (fallback)
- [ ] Flyout shows appropriate message
- [ ] Latency logged (STT + post-processing total)

**Tests:**
- [ ] Integration test: `DictationFlow_PostProcessingEnabled_UsesFormattedText()`
- [ ] Integration test: `DictationFlow_PostProcessingFails_UsesOriginalText()`
- [ ] Integration test: `DictationFlow_PostProcessingDisabled_SkipsPostProcessing()`

---

## Phase 3: Settings UI (~1h)

### Task 3.1: Add Post-Processing Section to SettingsWindow (~30min)

**File:** `src/LocalWhisper/UI/Settings/SettingsWindow.xaml`

**XAML Addition:**
```xaml
<!-- POST-PROCESSING SECTION -->
<Border BorderBrush="#D1D1D1" BorderThickness="0,0,0,1" Padding="0,0,0,20" Margin="0,0,0,20">
    <StackPanel>
        <TextBlock Text="Post-Processing (Optional)" FontSize="14" FontWeight="Bold" Margin="0,0,0,10"/>

        <CheckBox x:Name="PostProcessingEnabled"
                  Content="Post-Processing aktivieren"
                  Checked="PostProcessingEnabled_Changed"
                  Unchecked="PostProcessingEnabled_Changed"/>

        <TextBlock x:Name="PostProcessingStatus"
                   Margin="0,5,0,0"
                   FontSize="11"
                   Foreground="#666666"
                   Text="Modell: Nicht installiert"
                   Visibility="Visible"/>

        <Button x:Name="DownloadModelButton"
                Content="Modell herunterladen (~2GB)"
                Width="200"
                Height="30"
                Margin="0,10,0,0"
                Background="#0078D4"
                Foreground="White"
                Click="DownloadModelButton_Click"
                Visibility="Collapsed"/>
    </StackPanel>
</Border>
```

**Acceptance Criteria:**
- [ ] Checkbox to enable/disable post-processing
- [ ] Status text shows "Modell: Bereit" or "Modell: Nicht installiert"
- [ ] Download button appears if model not found
- [ ] No restart required (setting takes effect immediately)

**Tests:**
- [ ] UI test: `PostProcessingCheckbox_CheckedUnchecked_UpdatesConfig()`

---

### Task 3.2: Implement Model Download Dialog (~30min)

**File:** `src/LocalWhisper/UI/Dialogs/ModelDownloadDialog.xaml`

**Features:**
- Progress bar for download
- SHA-256 verification after download
- Manual download option (show URL)
- Cancel button

**Acceptance Criteria:**
- [ ] Downloads `Llama-3.2-3B-Instruct-Q4_K_M.gguf` from Hugging Face
- [ ] Shows progress (MB downloaded / total MB)
- [ ] Verifies SHA-256 hash after download
- [ ] Deletes file if hash mismatch
- [ ] Saves to `<DATA_ROOT>/models/llama-3.2-3b-q4.gguf`

**Tests:**
- [ ] Integration test: `DownloadModel_Success_VerifiesHash()`
- [ ] Integration test: `DownloadModel_HashMismatch_DeletesFile()`

---

## Phase 4: Config Schema (~30min)

### Task 4.1: Expand AppConfig for Post-Processing

**File:** `src/LocalWhisper/Models/AppConfig.cs`

**Add:**
```csharp
public class AppConfig
{
    // ... existing properties ...

    public PostProcessingConfig? PostProcessing { get; set; }
}

public class PostProcessingConfig
{
    public bool Enabled { get; set; } = false;
    public string ModelPath { get; set; } = string.Empty;
    public string CliPath { get; set; } = "llama-cli.exe";
}
```

**File:** `src/LocalWhisper/Core/ConfigManager.cs`

**Update Load/Save:**
```csharp
// Load
if (tomlTable.TryGetValue("post_processing", out var ppTable) && ppTable is TomlTable pp)
{
    config.PostProcessing = new PostProcessingConfig
    {
        Enabled = ParseBool(pp, "enabled", false),
        ModelPath = ParseString(pp, "model_path", string.Empty),
        CliPath = ParseString(pp, "cli_path", "llama-cli.exe")
    };
}

// Save
if (config.PostProcessing != null)
{
    tomlTable["post_processing"] = new TomlTable
    {
        ["enabled"] = config.PostProcessing.Enabled,
        ["model_path"] = config.PostProcessing.ModelPath,
        ["cli_path"] = config.PostProcessing.CliPath
    };
}
```

**Acceptance Criteria:**
- [ ] Config loads/saves post-processing settings
- [ ] Default: Enabled=false, ModelPath=empty
- [ ] CliPath defaults to "llama-cli.exe" (search PATH)

**Tests:**
- [ ] Unit test: `ConfigManager_SaveLoadPostProcessing_RoundTrips()`

---

## Phase 5: Error Handling & Logging (~1h)

### Task 5.1: Implement Comprehensive Error Handling

**Scenarios to handle:**
1. ✅ llama-cli.exe not found
2. ✅ Model file not found
3. ✅ Process timeout (5s)
4. ✅ Non-zero exit code
5. ✅ Empty stdout
6. ✅ GPU out of memory (retry with CPU)

**All scenarios → Fallback to original text**

**Logging:**
```csharp
// Success
AppLogger.LogInformation("Post-processing succeeded", new
{
    Mode = "PlainText",  // or "Markdown"
    OriginalLength = 87,
    ProcessedLength = 92,
    DurationMs = 834,
    GpuUsed = true
});

// Fallback
AppLogger.LogWarning("Post-processing failed, using original text", new
{
    Error = "Timeout (5s)",
    OriginalLength = 87
});
```

**Acceptance Criteria:**
- [ ] All error scenarios logged with details
- [ ] User never loses transcript (fallback always works)
- [ ] Flyout shows appropriate warning on errors
- [ ] Consecutive failures (3+) → suggest disabling

**Tests:**
- [ ] Unit test for each error scenario
- [ ] Integration test: `PostProcessing_ConsecutiveFailures_SuggestsDisabling()`

---

## Phase 6: Performance Monitoring (~30min)

### Task 6.1: Add Latency Tracking

**Metrics to track:**
- Post-processing latency (p50, p95, p99)
- GPU vs CPU usage
- Success vs fallback rate

**File:** `src/LocalWhisper/Services/PerformanceMonitor.cs`

**Methods:**
```csharp
public class PerformanceMonitor
{
    public void RecordPostProcessing(long durationMs, bool success, bool gpuUsed);
    public PerformanceStats GetStats();
}

public class PerformanceStats
{
    public double PostProcessingP50Ms { get; set; }
    public double PostProcessingP95Ms { get; set; }
    public double PostProcessingP99Ms { get; set; }
    public double SuccessRate { get; set; }
}
```

**Acceptance Criteria:**
- [ ] Latency logged for every invocation
- [ ] Stats viewable in Settings (optional)
- [ ] Warning shown if p95 > 2s

**Tests:**
- [ ] Unit test: `PerformanceMonitor_RecordsLatency()`

---

## Phase 7: Testing (~1h)

### Task 7.1: Manual Testing Checklist

**Plain Text Mode:**
- [ ] Test with short transcript (~20 words)
- [ ] Test with long transcript (~100 words)
- [ ] Test with filler words ("um", "uh", "like")
- [ ] Test with technical terms (Kubernetes, API, etc.)
- [ ] Verify no meaning changes
- [ ] Verify latency <1s on GPU, <2s on CPU

**Markdown Mode:**
- [ ] Test with "markdown mode" at start
- [ ] Test with "markdown mode" at end
- [ ] Test with "MARKDOWN MODE" (caps)
- [ ] Verify headings generated
- [ ] Verify lists formatted correctly

**Error Scenarios:**
- [ ] Disable llama-cli.exe → verify fallback
- [ ] Delete model file → verify fallback
- [ ] Simulate timeout → verify fallback
- [ ] Test consecutive failures → verify suggestion

**Performance:**
- [ ] Measure 100 dictations with post-processing enabled
- [ ] Verify p95 latency <2s (including STT)
- [ ] Verify no regressions in STT-only latency

---

### Task 7.2: Automated Tests

**Coverage targets:**
- [ ] Unit tests: 80% coverage (Services, Core logic)
- [ ] Integration tests: All BDD scenarios in iteration-07-bdd-scenarios.md
- [ ] All 30+ scenarios passing

---

## Phase 8: Documentation (~30min)

### Task 8.1: Update User Documentation

**Files to update:**
- [ ] `README.md` - Add post-processing section
- [ ] `docs/user-guide/post-processing.md` (new file)
- [ ] `CHANGELOG.md` - Add Iteration 7 entry

**Content:**
- How to enable post-processing
- How to download/install llama.cpp
- How to use "markdown mode" trigger
- Troubleshooting (model not found, slow performance)

---

### Task 8.2: Update Traceability Matrix

**File:** `docs/specification/traceability-matrix.md`

**Add:**
- [ ] Map FR-022 → Services (LlmPostProcessor, LlmCliAdapter)
- [ ] Map US-060, US-061, US-062 → Code modules
- [ ] Update iteration completion status

---

## Definition of Done

- [ ] All 8 phases complete
- [ ] All BDD scenarios passing (30+ scenarios)
- [ ] Unit test coverage ≥80%
- [ ] Integration tests passing
- [ ] Manual testing complete (all scenarios)
- [ ] Performance targets met (p95 <2s total latency)
- [ ] Error handling robust (no crashes, always fallback)
- [ ] Logging comprehensive (all state transitions, errors)
- [ ] Documentation updated (README, traceability matrix, changelog)
- [ ] Code reviewed (self-review using BDD scenarios as checklist)
- [ ] No regressions (Iterations 1-4 still working)

---

## Commit Strategy

**Recommended commits:**
1. `feat(iter-7): Add LlmPostProcessor service and prompt templates`
2. `feat(iter-7): Add LlmCliAdapter with GPU detection and fallback`
3. `feat(iter-7): Integrate post-processing into StateMachine`
4. `feat(iter-7): Add post-processing settings UI and model download`
5. `feat(iter-7): Add comprehensive error handling and fallback logic`
6. `test(iter-7): Add unit and integration tests for post-processing`
7. `docs(iter-7): Update README and traceability matrix`
8. `chore(iter-7): Final iteration 7 completion (US-060, US-061, US-062)`

---

**Last Updated:** 2025-11-18
**Status:** Ready for implementation
**Total Estimated Effort:** 5-6 hours
