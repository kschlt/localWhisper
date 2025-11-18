# Iteration 4: Clipboard + History + Flyout

**Goal:** Complete end-to-end dictation flow with clipboard write, history persistence, and visual feedback

**Status:** In Progress
**Estimated Effort:** 6-10 hours
**Dependencies:** Iteration 1, 2, 3 (STT integration)

---

## Overview

This iteration completes the **core value proposition** of LocalWhisper: Hold hotkey → speak → release → text in clipboard with history saved and flyout confirmation.

**Major Milestone:** This is the **first E2E functional milestone**. After this iteration, the app delivers full dictation workflow.

---

## User Stories

| ID | Title | Priority | Tags |
|----|-------|----------|------|
| US-030 | Clipboard Write | High | @FR-013 |
| US-031 | History File Creation | High | @FR-014 |
| US-032 | Custom Flyout Notification | High | @FR-015 |
| US-033 | E2E Latency Measurement | High | @NFR-001 |
| US-034 | Flyout Latency Measurement | Medium | @NFR-004 |
| US-035 | End-to-End Logging | Medium | @FR-023 |
| US-036 | Slug Generation | Medium | @FR-024 |

**Total:** 7 user stories

---

## Functional Requirements Covered

| FR ID | Title | Scope |
|-------|-------|-------|
| FR-013 | Clipboard Write | ✅ Full |
| FR-014 | History Persistence | ✅ Full |
| FR-015 | Notification UI | ✅ Full (flyout implementation) |
| FR-023 | Structured Logging | ✅ Extended (E2E flow) |
| FR-024 | Slug Generation | ✅ Full |

---

## Non-Functional Requirements Verified

| NFR ID | Title | Target | Verification |
|--------|-------|--------|--------------|
| NFR-001 | E2E Latency | p95 ≤ 2.5s | Manual measurement (100 dictations) |
| NFR-004 | Flyout Latency | ≤ 0.5s | Logged automatically |

---

## Architecture Changes

### New Components

**1. Services/ClipboardWriter.cs**
- Writes transcript to Windows clipboard
- Retry logic (1 retry, 100ms delay) for locked clipboard
- Logs success/failure

**2. Services/HistoryWriter.cs**
- Writes markdown files to history/ directory
- Date-based folder structure: `history/YYYY/YYYY-MM/YYYY-MM-DD/`
- YAML front-matter with metadata
- Slug-based filename: `YYYYMMDD_HHmmssfff_{slug}.md`

**3. Utils/SlugGenerator.cs**
- Generates kebab-case slugs from transcript text
- Normalizes German umlauts (ä→a, ö→o, ü→u)
- 50 character limit
- Handles edge cases (empty, special chars, multiple hyphens)

**4. Models/HistoryEntry.cs**
- Data model for history file metadata
- Created timestamp, language, model, duration
- Post-processing flag (always false in Iter 4)

**5. UI/Flyout/FlyoutWindow.xaml + .cs**
- Custom WPF window near system tray
- 3-second auto-dismiss timer
- No focus stealing (Topmost + NoActivate)
- Displays success message or warning

### Modified Components

**App.xaml.cs - HandleHotkeyPressAsync:**
- After STT success:
  1. Measure E2E latency start
  2. Write to clipboard (with retry)
  3. Write to history (parallel with clipboard)
  4. Show flyout notification
  5. Log all operations with timings
  6. Transition to Idle

---

## Implementation Tasks

### Task 1: Slug Generation (US-036)
**File:** `src/LocalWhisper/Utils/SlugGenerator.cs`

**Requirements:**
- Static method: `string Generate(string text, int maxLength = 50)`
- Lowercase conversion
- German umlaut normalization: ä→a, ö→o, ü→u, ß→ss
- Remove special characters except alphanumeric and hyphen
- Replace spaces with hyphens
- Compress multiple hyphens to single hyphen
- Trim to maxLength characters
- Fallback to "transcript" if empty

**Test Cases:**
- "Let me check on that" → "let-me-check-on-that"
- "Äpfel & Übung" → "apfel-ubung"
- "(empty)" → "transcript"
- Long text → truncated to 50 chars

**File:** `tests/LocalWhisper.Tests/Unit/SlugGeneratorTests.cs`

---

### Task 2: History Entry Model (US-031)
**File:** `src/LocalWhisper/Models/HistoryEntry.cs`

**Properties:**
- Created (DateTimeOffset)
- Text (string)
- Language (string)
- SttModel (string)
- DurationSeconds (double)
- PostProcessed (bool) - always false in Iter 4

**Methods:**
- ToMarkdown(): Generates markdown with YAML front-matter
- GetFileName(string slug): Returns "YYYYMMDD_HHmmssfff_{slug}.md"
- GetRelativeDirectory(): Returns "history/YYYY/YYYY-MM/YYYY-MM-DD/"

---

### Task 3: History Writer Service (US-031)
**File:** `src/LocalWhisper/Services/HistoryWriter.cs`

**Method:** `Task<string> WriteAsync(HistoryEntry entry, string dataRoot)`

**Logic:**
1. Generate slug from entry.Text
2. Build directory path: `{dataRoot}/history/YYYY/YYYY-MM/YYYY-MM-DD/`
3. Ensure directory exists
4. Build filename: `YYYYMMDD_HHmmssfff_{slug}.md`
5. Check for duplicate filename, append `_2`, `_3`, etc. if needed
6. Write markdown content (front-matter + body)
7. Return absolute file path
8. Log operation with path and duration

**Error Handling:**
- Catch IOException and log warning
- Do not throw (graceful degradation)

**File:** `tests/LocalWhisper.Tests/Unit/HistoryWriterTests.cs`

---

### Task 4: Clipboard Writer Service (US-030)
**File:** `src/LocalWhisper/Services/ClipboardWriter.cs`

**Method:** `Task WriteAsync(string text, int maxRetries = 1, int retryDelayMs = 100)`

**Logic:**
1. Use WPF `Clipboard.SetText(text)` on STA thread
2. Wrap in try-catch for `COMException` (clipboard locked)
3. On lock: Wait `retryDelayMs`, retry up to `maxRetries` times
4. On final failure: Log error and throw `ClipboardLockedException`
5. Log success with text length

**Error Handling:**
- `ClipboardLockedException` (custom exception)
- Log each retry attempt

**File:** `tests/LocalWhisper.Tests/Unit/ClipboardWriterTests.cs` (integration test with mock)

---

### Task 5: Flyout Window (US-032)
**Files:**
- `src/LocalWhisper/UI/Flyout/FlyoutWindow.xaml`
- `src/LocalWhisper/UI/Flyout/FlyoutWindow.xaml.cs`

**UI Requirements:**
- Small window (300x80 px)
- Green checkmark icon (or warning icon for errors)
- Message text: "Transkript im Clipboard" or custom message
- Positioned near system tray (bottom-right, 10px margin)
- No taskbar button
- No activation (NoActivate flag)
- Topmost window
- Auto-dismiss after 3 seconds

**Properties:**
- `ShowFlyout(string message, FlyoutType type = Success)`
- `FlyoutType` enum: Success, Warning, Error

**Implementation:**
- Use `DispatcherTimer` for 3-second auto-dismiss
- Position calculation based on screen working area
- Fade-in animation (optional, nice-to-have)

---

### Task 6: Integration in App.xaml.cs (US-030, US-031, US-032, US-033, US-035)
**File:** `src/LocalWhisper/App.xaml.cs`

**Changes in `HandleHotkeyPressAsync` after STT success:**

```csharp
// After: var sttResult = await _whisperAdapter!.TranscribeAsync(wavFilePath);

if (sttResult.IsEmpty)
{
    AppLogger.LogInformation("No speech detected in recording");
    ShowFlyout("Keine Sprache erkannt", FlyoutType.Warning);
    _stateMachine.TransitionTo(AppState.Idle);
    return;
}

// Start E2E latency measurement
var e2eStopwatch = Stopwatch.StartNew();

// 1. Write to clipboard (with retry)
try
{
    var clipboardStopwatch = Stopwatch.StartNew();
    await ClipboardWriter.WriteAsync(sttResult.Text);
    clipboardStopwatch.Stop();

    AppLogger.LogInformation("Clipboard write succeeded", new
    {
        TextLength = sttResult.Text.Length,
        Duration_Ms = clipboardStopwatch.ElapsedMilliseconds
    });
}
catch (ClipboardLockedException ex)
{
    AppLogger.LogError("Clipboard locked - cannot write", ex);
    ShowFlyout("Zwischenablage gesperrt", FlyoutType.Warning);
    // Continue to history write anyway
}

// 2. Write to history (parallel-capable, but sequential for simplicity in Iter 4)
try
{
    var historyStopwatch = Stopwatch.StartNew();
    var historyEntry = new HistoryEntry
    {
        Created = DateTimeOffset.Now,
        Text = sttResult.Text,
        Language = sttResult.Language,
        SttModel = _config.Whisper.ModelPath,
        DurationSeconds = sttResult.DurationSeconds,
        PostProcessed = false
    };

    var historyPath = await HistoryWriter.WriteAsync(historyEntry, _dataRoot!);
    historyStopwatch.Stop();

    AppLogger.LogInformation("History file created", new
    {
        Path = historyPath,
        Duration_Ms = historyStopwatch.ElapsedMilliseconds
    });
}
catch (Exception ex)
{
    AppLogger.LogWarning("History write failed", new { Error = ex.Message });
    // Do not block on history failure
}

// 3. Show flyout notification
e2eStopwatch.Stop();
AppLogger.LogInformation("E2E dictation completed", new
{
    E2E_Latency_Ms = e2eStopwatch.ElapsedMilliseconds,
    TranscriptLength = sttResult.Text.Length
});

var flyoutStopwatch = Stopwatch.StartNew();
ShowFlyout("Transkript im Clipboard", FlyoutType.Success);
flyoutStopwatch.Stop();

AppLogger.LogInformation("Flyout displayed", new
{
    Flyout_Display_Latency_Ms = flyoutStopwatch.ElapsedMilliseconds
});

// 4. Return to idle
_stateMachine.TransitionTo(AppState.Idle);
```

**New fields:**
```csharp
private ClipboardWriter? _clipboardWriter;
private HistoryWriter? _historyWriter;
```

**Initialization in OnStartup:**
```csharp
_clipboardWriter = new ClipboardWriter();
_historyWriter = new HistoryWriter();
```

---

## Out of Scope (Deferred)

- Model download wizard (Iteration 5)
- Settings UI for data root (Iteration 6)
- Post-processing with LLM (Iteration 7)
- Performance profiling/optimization (Iteration 8)
- Parallel clipboard/history writes (optimization, defer to Iteration 8)

---

## Testing Strategy

### Unit Tests

1. **SlugGeneratorTests.cs**
   - Test normalization rules
   - Test truncation
   - Test edge cases (empty, special chars)
   - Test German umlauts

2. **HistoryWriterTests.cs**
   - Test markdown generation
   - Test directory creation
   - Test duplicate filename handling
   - Test file content format

3. **ClipboardWriterTests.cs**
   - Mock clipboard service
   - Test retry logic
   - Test exception handling

### Integration Tests

- E2E dictation flow with mock Whisper CLI
- Verify clipboard contains transcript
- Verify history file exists with correct format
- Verify flyout displays

### Manual Tests

- Clipboard locked by another app → retry behavior
- History directory write-protected → graceful degradation
- Flyout does not steal focus while typing
- p95 latency measurement (100 dictations)

---

## Definition of Done

- [ ] All 7 user stories (US-030 through US-036) implemented
- [ ] Acceptance criteria from Gherkin scenarios satisfied
- [ ] SlugGenerator unit tests pass
- [ ] HistoryWriter unit tests pass
- [ ] ClipboardWriter retry logic tested
- [ ] Flyout UI displays correctly (manual test)
- [ ] Flyout does not steal focus (manual test)
- [ ] p95 latency ≤ 2.5s (manual measurement documented)
- [ ] Flyout latency ≤ 0.5s (logged automatically)
- [ ] E2E logging includes all operations with timings
- [ ] Traceability matrix updated
- [ ] Changelog entry added
- [ ] Commit message references US-030..036
- [ ] No regressions (Iteration 1-3 tests still pass)

---

## Performance Targets

| Metric | Target | Verification |
|--------|--------|--------------|
| E2E Latency (p95) | ≤ 2.5s | Manual measurement (100 samples) |
| Flyout Display | ≤ 0.5s | Logged automatically |
| Clipboard Write | < 100ms | Logged automatically |
| History Write | < 200ms | Logged automatically |

---

## Risk Mitigation

**Risk:** p95 latency exceeds 2.5s target
**Mitigation:** Profile bottlenecks, optimize in Iteration 8 if needed

**Risk:** Clipboard locking causes UX issues
**Mitigation:** Retry logic + graceful degradation (history still saved)

**Risk:** Flyout steals focus from typing
**Mitigation:** Use WPF NoActivate flag, test manually

---

**Iteration Start:** 2025-11-17
**Target Completion:** Iteration 4 complete with E2E flow working
