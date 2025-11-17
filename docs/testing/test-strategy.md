# Test Strategy

**Purpose:** Define testing approach, tools, and coverage for the project
**Audience:** Developers, QA, AI agents
**Status:** Stable (v0.1)
**Last Updated:** 2025-09-17

---

## Testing Pyramid

```
           ┌─────────────────┐
           │  Manual Tests   │  ← Performance, usability, exploratory
           │                 │
           ├─────────────────┤
           │  BDD/E2E Tests  │  ← Acceptance criteria, user scenarios
           │                 │
           ├─────────────────┤
           │ Integration     │  ← Component interactions, CLI adapters
           │ Tests           │
           ├─────────────────┤
           │                 │
           │  Unit Tests     │  ← Individual components, algorithms
           │                 │
           └─────────────────┘
```

**Distribution (target):**
- Unit tests: ~60% of test effort
- Integration tests: ~25% of test effort
- BDD/E2E tests: ~10% of test effort
- Manual tests: ~5% of test effort

---

## Testing Levels

### 1. Unit Tests

**Scope:** Individual classes, methods, and algorithms

**Tools:**
- xUnit or NUnit (.NET)
- FluentAssertions (readable assertions)
- Moq (mocking dependencies)

**Examples:**

**SlugGenerator:**
```csharp
[Fact]
public void GenerateSlug_WithNormalText_ProducesValidSlug()
{
    var input = "Let me check on that and get back to you";
    var expected = "let-me-check-on-that-and-get";

    var result = SlugGenerator.Generate(input);

    result.Should().Be(expected);
}

[Fact]
public void GenerateSlug_WithSpecialCharacters_RemovesInvalidChars()
{
    var input = "Meeting @ 3:00 PM";
    var expected = "meeting-at-3-00-pm";

    var result = SlugGenerator.Generate(input);

    result.Should().Be(expected);
}
```

**ConfigManager:**
```csharp
[Fact]
public void LoadConfig_WithValidTOML_ReturnsConfig()
{
    var toml = @"
[app]
language = ""de""

[hotkey]
modifiers = [""Ctrl"", ""Shift""]
key = ""D""
";
    File.WriteAllText(testConfigPath, toml);

    var config = ConfigManager.Load(testConfigPath);

    config.App.Language.Should().Be("de");
    config.Hotkey.Modifiers.Should().Contain(new[] { "Ctrl", "Shift" });
}
```

**StateMachine:**
```csharp
[Fact]
public void Transition_FromIdleToRecording_Succeeds()
{
    var sm = new StateMachine();
    sm.CurrentState.Should().Be(AppState.Idle);

    sm.Transition(AppState.Recording);

    sm.CurrentState.Should().Be(AppState.Recording);
}

[Fact]
public void Transition_FromRecordingToIdle_ThrowsException()
{
    var sm = new StateMachine();
    sm.Transition(AppState.Recording);

    Action act = () => sm.Transition(AppState.Idle);

    act.Should().Throw<InvalidStateTransitionException>();
}
```

**Coverage Target:** 80% code coverage for core logic (excluding UI code-behind)

---

### 2. Integration Tests

**Scope:** Component interactions, external dependencies (filesystem, CLI tools)

**Tools:**
- xUnit with fixtures for setup/teardown
- Testcontainers (if database is added later)
- Mock CLI executables

**Examples:**

**WhisperCLIAdapter with Mock CLI:**
```csharp
[Fact]
public async Task Transcribe_WithValidWAV_ReturnsTranscript()
{
    // Arrange: Use mock-whisper.exe that outputs fixed JSON
    var adapter = new WhisperCLIAdapter(mockWhisperPath, testModelPath);
    var wavPath = Path.Combine(testDataPath, "sample-5s.wav");

    // Act
    var result = await adapter.TranscribeAsync(wavPath, "de");

    // Assert
    result.Text.Should().NotBeEmpty();
    result.Language.Should().Be("de");
    result.DurationSec.Should().BeApproximately(5.0, 0.5);
}

[Fact]
public async Task Transcribe_WithMissingModel_ThrowsSTTException()
{
    var adapter = new WhisperCLIAdapter(whisperPath, "nonexistent-model.gguf");
    var wavPath = Path.Combine(testDataPath, "sample-5s.wav");

    Func<Task> act = async () => await adapter.TranscribeAsync(wavPath, "de");

    await act.Should().ThrowAsync<STTException>()
        .WithMessage("*Model not found*");
}
```

**HistoryWriter:**
```csharp
[Fact]
public void Write_WithTranscript_CreatesFileWithCorrectStructure()
{
    var writer = new HistoryWriter(testDataRoot);
    var transcript = "Let me check on that and get back to you.";
    var metadata = new TranscriptMetadata
    {
        Created = DateTime.Parse("2025-09-17T14:30:22Z"),
        Lang = "en",
        Model = "whisper-small",
        DurationSec = 4.8
    };

    var filePath = writer.Write(transcript, metadata);

    File.Exists(filePath).Should().BeTrue();
    var content = File.ReadAllText(filePath);
    content.Should().Contain("created: 2025-09-17T14:30:22Z");
    content.Should().Contain("# Diktat – 17.09.2025 14:30");
    content.Should().Contain(transcript);
}
```

**Coverage Target:** All adapters and service classes

---

### 3. BDD / E2E Tests

**Scope:** User scenarios, acceptance criteria, end-to-end flows

**Tools:**
- SpecFlow (.NET Gherkin framework)
- Selenium WebDriver or Windows UI Automation (for UI interactions)

**Feature Files:** See `testing/bdd-feature-seeds.md`

**Example Step Definitions:**

```csharp
[Binding]
public class DictationSteps
{
    private readonly ScenarioContext context;
    private readonly AppTestHarness app;

    public DictationSteps(ScenarioContext context)
    {
        this.context = context;
        this.app = new AppTestHarness(); // Test double for app
    }

    [Given(@"die App läuft und ein Modell ist konfiguriert")]
    public void GivenAppIsRunning()
    {
        app.Start();
        app.EnsureModelConfigured();
    }

    [When(@"ich den Hotkey gedrückt halte, spreche und loslasse")]
    public void WhenIDictate()
    {
        app.PressHotkey();
        Thread.Sleep(5000); // Simulate speaking for 5s
        app.ReleaseHotkey();
    }

    [Then(@"steht der transkribierte Text im System-Clipboard")]
    public void ThenTranscriptIsInClipboard()
    {
        var clipboardText = Clipboard.GetText();
        clipboardText.Should().NotBeEmpty();
        clipboardText.Should().Contain("check"); // Assuming test audio
    }

    [Then(@"eine History-Datei wurde unter dem datumsbasierten Pfad erstellt")]
    public void ThenHistoryFileExists()
    {
        var expectedPath = app.GetExpectedHistoryFilePath();
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Then(@"ein Flyout ist sichtbar")]
    public void ThenFlyoutIsVisible()
    {
        app.FlyoutIsVisible().Should().BeTrue();
    }
}
```

**Coverage Target:** All use cases (UC-001 through UC-004)

---

### 4. Manual Tests

**Scope:** Performance, usability, exploratory testing

**When:**
- Iteration 4: p95 latency measurement
- Iteration 5: Wizard usability testing
- Iteration 8: Error scenario testing, long-running stability

**Examples:**

**Performance Test (NFR-001):**
1. Prepare 100 test audio files (3s, 5s, 10s mix)
2. For each file:
   - Trigger dictation (hotkey down → up)
   - Measure time from hotkey release to clipboard write
   - Record latency
3. Calculate p50, p95, p99
4. Verify: p95 ≤ 2.5s

**Wizard Usability Test (NFR-004):**
1. Start with fresh Windows VM (no app installed)
2. Time: Start wizard → app tray icon visible
3. Target: < 2 minutes

**Error Matrix Test (NFR-003):**
| Scenario | Expected Behavior | Pass/Fail |
|----------|-------------------|-----------|
| Microphone denied | Error dialog, no crash | |
| Model missing | Error dialog, wizard repair | |
| Hotkey conflict | Warning, suggest alternative | |
| Disk full | Clipboard OK, history fails with warning | |
| STT timeout | Timeout dialog, no hang | |

---

## Test Data

**Location:** `tests/TestData/`

**Contents:**
- `sample-3s.wav`: 3-second audio ("Hello world")
- `sample-5s.wav`: 5-second audio ("Let me check on that")
- `sample-10s.wav`: 10-second audio (longer test)
- `mock-whisper.exe`: Returns fixed JSON after delay
- `config-valid.toml`: Valid configuration
- `config-invalid.toml`: Invalid configuration (for error tests)

---

## CI/CD Integration

**Build Pipeline:**
1. Restore dependencies (`dotnet restore`)
2. Build (`dotnet build`)
3. Run unit tests (`dotnet test --filter Category=Unit`)
4. Run integration tests (`dotnet test --filter Category=Integration`)
5. Run BDD tests (`dotnet test --filter Category=BDD`)
6. Generate coverage report (Coverlet)
7. Fail build if coverage < 70%

**Manual Verification (Pre-Release):**
- Performance tests
- Wizard usability
- Error matrix
- Long-running stability (24-hour soak test)

---

## Test Coverage Targets

| Component | Unit | Integration | BDD | Total |
|-----------|------|-------------|-----|-------|
| Core (SlugGenerator, ConfigManager, etc.) | 90% | N/A | N/A | 90% |
| Services (AudioRecorder, ClipboardService, etc.) | 70% | 80% | N/A | 80% |
| Adapters (WhisperCLI, PostProcessor) | 50% | 90% | N/A | 85% |
| UI (Wizard, Settings, Flyout) | 30% | N/A | 100% | 80% |
| **Overall** | **60%** | **80%** | **100%** | **80%** |

---

## Test Execution Schedule

**During Iterations:**
- Run unit tests continuously (TDD approach)
- Run integration tests after implementing adapters/services
- Run BDD scenarios for current iteration before marking complete

**Before Each Iteration:**
- Run all previous iteration tests (regression)

**Before Release (Iteration 8):**
- Run full test suite (unit + integration + BDD)
- Execute manual performance tests
- Execute error matrix
- Soak test (24-hour run with periodic dictations)

---

## Mocking Strategy

**Mock External Dependencies:**
- **Whisper CLI:** Use `mock-whisper.exe` that outputs fixed JSON
- **LLM CLI:** Use `mock-llm.exe` that echoes input with minor formatting
- **Filesystem:** Use in-memory filesystem or temp directories
- **Clipboard:** Mock clipboard service for unit tests (real clipboard for E2E)
- **Windows APIs:** Mock `RegisterHotKey` and WASAPI for unit tests

**Example Mock CLI:**
```csharp
// mock-whisper.exe (simple C# console app)
static void Main(string[] args)
{
    Thread.Sleep(100); // Simulate processing
    var json = @"{
        ""text"": ""This is a test transcript."",
        ""language"": ""en"",
        ""duration_sec"": 5.0
    }";
    Console.WriteLine(json);
    Environment.Exit(0);
}
```

---

## Troubleshooting Failed Tests

**Common Issues:**

**Issue:** Unit test fails with "File not found"
- **Cause:** Test data path is incorrect
- **Fix:** Use `Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "file.wav")`

**Issue:** Integration test hangs
- **Cause:** CLI process doesn't exit
- **Fix:** Ensure mock CLI exits cleanly; check timeout in adapter

**Issue:** BDD test fails intermittently
- **Cause:** Timing issues (race conditions)
- **Fix:** Add explicit waits or use `Eventually()` assertions

**Issue:** Coverage drops unexpectedly
- **Cause:** New code added without tests
- **Fix:** Add unit tests for new methods; aim for 80% coverage

---

## Related Documents

- **BDD Feature Seeds:** `testing/bdd-feature-seeds.md`
- **Iteration Plan:** `iterations/iteration-plan.md`
- **Requirements:** `specification/functional-requirements.md`, `specification/non-functional-requirements.md`

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial test strategy)
