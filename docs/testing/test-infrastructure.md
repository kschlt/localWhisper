# Testing Infrastructure & Strategy

**Purpose:** Testing approach for Claude Code environment (Linux/cloud-based development)
**Status:** Normative (must follow these patterns)
**Last Updated:** 2025-11-17
**Owner:** QA Lead / Solution Architect

---

## Challenge: Testing Windows App in Claude Code

**Context:**
- This is a **Windows desktop application** (WPF, Win32 APIs)
- Development environment: **Claude Code** (Linux-based, cloud)
- **Cannot run Windows UI tests directly** in Claude Code

**Solution:** Multi-tier testing strategy with emphasis on **contract testing** and **local verification**

---

## Testing Strategy Overview

```
┌─────────────────────────────────────────────────────────────┐
│                  TESTING PYRAMID (Adapted)                   │
└─────────────────────────────────────────────────────────────┘

              ┌─────────────────┐
              │  Manual Tests   │  ← Run on Windows VM (final verification)
              │   (5%)          │
              ├─────────────────┤
              │ Contract Tests  │  ← Verify interfaces without Windows
              │   (10%)         │
              ├─────────────────┤
              │ Integration     │  ← Mock Windows APIs, test logic
              │ Tests (25%)     │
              ├─────────────────┤
              │                 │
              │  Unit Tests     │  ← Pure .NET logic, no Windows dependencies
              │    (60%)        │
              └─────────────────┘

      CAN RUN IN CLAUDE CODE  │  NEEDS WINDOWS
      ├───────────────────────┼──────────────────┤
      │ Unit                  │                  │
      │ Integration (mocks)   │                  │
      │ Contract              │  Manual UI tests │
      └───────────────────────┴──────────────────┘
```

---

## 1. Unit Tests (60% - Run in Claude Code ✅)

**What:** Test pure .NET logic without Windows dependencies

**Examples:**
- `SlugGenerator` - text normalization rules
- `ConfigManager` - TOML parsing/validation
- `StateMachine` - state transitions
- `WhisperCLIAdapter` - JSON parsing (not process execution)
- `HistoryWriter` - file path generation (not file I/O)

**Approach:**
```csharp
// CAN run in Claude Code (Linux)
[Fact]
public void SlugGenerator_NormalizesTextCorrectly()
{
    var input = "Let me check on that and get back";
    var expected = "let-me-check-on-that-and-get";

    var result = SlugGenerator.Generate(input);

    result.Should().Be(expected);
}
```

**How to Run:**
```bash
dotnet test --filter Category=Unit
```

✅ **Works in Claude Code** - no Windows APIs required

---

## 2. Integration Tests with Mocks (25% - Run in Claude Code ✅)

**What:** Test component interactions using mocked Windows APIs

**Strategy:** Use **abstraction interfaces** for Windows-specific operations

### Abstraction Pattern

```csharp
// Interface (platform-agnostic)
public interface IClipboardService
{
    void SetText(string text);
    string GetText();
}

// Real implementation (Windows-only)
public class WindowsClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        Clipboard.SetText(text); // WPF API
    }

    public string GetText()
    {
        return Clipboard.GetText();
    }
}

// Mock implementation (cross-platform)
public class MockClipboardService : IClipboardService
{
    private string _clipboardContent = "";

    public void SetText(string text)
    {
        _clipboardContent = text;
    }

    public string GetText()
    {
        return _clipboardContent;
    }
}
```

### Integration Test Example

```csharp
[Fact]
public async Task DictationFlow_WritesToClipboard()
{
    // Arrange: Use mocks for Windows APIs
    var mockClipboard = new MockClipboardService();
    var mockAudio = new MockAudioRecorder();
    var mockSTT = new MockWhisperAdapter();

    var orchestrator = new DictationOrchestrator(mockClipboard, mockAudio, mockSTT);

    // Act
    await orchestrator.StartRecordingAsync();
    await Task.Delay(100); // Simulate recording
    var result = await orchestrator.StopRecordingAsync();

    // Assert
    mockClipboard.GetText().Should().NotBeEmpty();
}
```

**How to Run:**
```bash
dotnet test --filter Category=Integration
```

✅ **Works in Claude Code** - uses mocks, not real Windows APIs

---

## 3. Contract Tests (10% - Run in Claude Code ✅)

**What:** Verify external interfaces (CLI contracts, file formats) without full E2E

**Purpose:** Ensure CLI adapters and file parsers work correctly

### CLI Contract Test

```csharp
[Fact]
public async Task WhisperCLI_ParsesValidJSON()
{
    // Arrange: Sample JSON that whisper-cli.exe would produce
    var sampleJSON = @"{
        ""text"": ""This is a test transcript."",
        ""language"": ""en"",
        ""duration_sec"": 5.0,
        ""segments"": [{""start"": 0.0, ""end"": 5.0, ""text"": ""This is a test transcript.""}],
        ""meta"": {""model"": ""whisper-small"", ""processing_time_sec"": 0.5}
    }";

    File.WriteAllText("test_output.json", sampleJSON);

    // Act: Adapter parses JSON
    var adapter = new WhisperCLIAdapter();
    var result = adapter.ParseResult("test_output.json");

    // Assert: Verify all fields parsed correctly
    result.Text.Should().Be("This is a test transcript.");
    result.Language.Should().Be("en");
    result.DurationSec.Should().Be(5.0);
    result.Segments.Should().HaveCount(1);
}
```

### File Format Contract Test

```csharp
[Fact]
public void HistoryWriter_CreatesValidMarkdown()
{
    // Arrange
    var writer = new HistoryWriter("/tmp/test_history");
    var metadata = new TranscriptMetadata
    {
        Created = DateTime.Parse("2025-09-17T14:30:22Z"),
        Lang = "en",
        Model = "whisper-small",
        DurationSec = 4.8
    };

    // Act
    var filePath = writer.Write("This is a test transcript.", metadata);

    // Assert: Verify file structure
    File.Exists(filePath).Should().BeTrue();
    var content = File.ReadAllText(filePath);

    content.Should().Contain("created: 2025-09-17T14:30:22Z");
    content.Should().Contain("lang: en");
    content.Should().Contain("# Diktat – 17.09.2025 14:30");
    content.Should().Contain("This is a test transcript.");
}
```

**How to Run:**
```bash
dotnet test --filter Category=Contract
```

✅ **Works in Claude Code** - tests file formats and JSON parsing, not Windows UI

---

## 4. BDD/Gherkin Tests (Acceptance Criteria)

**What:** Executable specifications in Gherkin format

**Strategy:** Use **SpecFlow** (or similar) with **mock implementations**

### Gherkin Scenario (Example)

```gherkin
@Iter-1 @Unit @CanRunInClaudeCode
Scenario: Hotkey down transitions state to Recording
  Given the app state is "Idle"
  When the hotkey down event occurs
  Then the app state should be "Recording"
  And a log entry should be created with "State transition: Idle -> Recording"
```

### Step Definition (With Mocks)

```csharp
[Binding]
public class HotkeySteps
{
    private StateMachine _stateMachine;
    private List<string> _logEntries = new List<string>();

    [Given(@"the app state is ""(.*)""")]
    public void GivenAppState(string state)
    {
        _stateMachine = new StateMachine();
        // State machine starts in Idle by default
    }

    [When(@"the hotkey down event occurs")]
    public void WhenHotkeyDown()
    {
        // Simulate hotkey down (no real Windows hotkey)
        _stateMachine.Transition(AppState.Recording);
    }

    [Then(@"the app state should be ""(.*)""")]
    public void ThenAppStateShouldBe(string expectedState)
    {
        Enum.Parse<AppState>(expectedState).Should().Be(_stateMachine.CurrentState);
    }

    [Then(@"a log entry should be created with ""(.*)""")]
    public void ThenLogEntryShouldContain(string expectedLog)
    {
        // In real implementation, verify logger was called
        // For now, verify via state machine event
        _stateMachine.StateChanged += (s, e) =>
        {
            _logEntries.Add($"State transition: {e.OldState} -> {e.NewState}");
        };

        _logEntries.Should().Contain(log => log.Contains(expectedLog));
    }
}
```

**How to Run:**
```bash
dotnet test --filter Category=BDD
```

✅ **Works in Claude Code** - uses mocks, not real hotkey registration

---

## 5. Manual Tests on Windows (Final Verification)

**What:** E2E tests that require real Windows environment

**When:** After all automated tests pass in Claude Code

**Where:** Windows VM (or local Windows machine)

### Manual Test Scenarios

| Test | Steps | Expected |
|------|-------|----------|
| **Hotkey E2E** | 1. Start app<br>2. Press Ctrl+Shift+D | Tray icon shows recording |
| **Full Dictation** | 1. Hold hotkey<br>2. Speak "Hello world"<br>3. Release | Text in clipboard, history file created |
| **Wizard** | 1. Fresh install<br>2. Complete wizard | App starts, config exists |
| **Error: Mic Denied** | 1. Deny mic permissions<br>2. Press hotkey | Error dialog, no crash |

**How to Run:**
1. Deploy app to Windows VM
2. Run manual test checklist
3. Document results

❌ **Cannot run in Claude Code** - requires Windows

---

## AppTestHarness (For Windows E2E Tests)

**Note:** This is for **Windows-based testing only** (not Claude Code)

```csharp
public class AppTestHarness : IDisposable
{
    private Process _appProcess;
    private string _testDataRoot;

    // Lifecycle
    public void Start()
    {
        _testDataRoot = Path.Combine(Path.GetTempPath(), $"AppTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataRoot);

        _appProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "SpeechClipboard.exe",
            Arguments = $"--test-mode --data-root \"{_testDataRoot}\"",
            UseShellExecute = false
        });

        Thread.Sleep(2000); // Wait for app startup
    }

    public void Stop()
    {
        _appProcess?.Kill();
        _appProcess?.WaitForExit();
        Directory.Delete(_testDataRoot, true);
    }

    // Hotkey simulation (Windows-only)
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public void PressHotkey()
    {
        // Simulate Ctrl+Shift+D
        keybd_event(0x11, 0, 0, UIntPtr.Zero); // Ctrl down
        keybd_event(0x10, 0, 0, UIntPtr.Zero); // Shift down
        keybd_event(0x44, 0, 0, UIntPtr.Zero); // D down
    }

    public void ReleaseHotkey()
    {
        keybd_event(0x44, 0, 2, UIntPtr.Zero); // D up
        keybd_event(0x10, 0, 2, UIntPtr.Zero); // Shift up
        keybd_event(0x11, 0, 2, UIntPtr.Zero); // Ctrl up
    }

    // State inspection
    public string GetClipboardText()
    {
        return Clipboard.GetText();
    }

    public string GetLastHistoryFilePath()
    {
        var historyDir = Path.Combine(_testDataRoot, "history");
        return Directory.GetFiles(historyDir, "*.md", SearchOption.AllDirectories)
            .OrderByDescending(File.GetCreationTime)
            .FirstOrDefault();
    }
}
```

---

## Mock CLI Executables

### mock-whisper.exe (For Integration Tests)

**Purpose:** Simulate whisper-cli.exe behavior without real STT

**Implementation:**

```csharp
// mock-whisper/Program.cs
class Program
{
    static void Main(string[] args)
    {
        // Parse arguments (simple implementation)
        var outputFile = args.FirstOrDefault(a => args[Array.IndexOf(args, a) - 1] == "--output-file");

        // Simulate processing delay
        var delay = int.Parse(Environment.GetEnvironmentVariable("MOCK_WHISPER_DELAY_MS") ?? "100");
        Thread.Sleep(delay);

        // Generate mock output
        var json = @"{
            ""text"": ""This is a test transcript."",
            ""language"": ""en"",
            ""duration_sec"": 5.0,
            ""segments"": [{""start"": 0.0, ""end"": 5.0, ""text"": ""This is a test transcript.""}],
            ""meta"": {""model"": ""whisper-small"", ""processing_time_sec"": 0.5}
        }";

        File.WriteAllText(outputFile, json);

        // Exit code (override via env var)
        var exitCode = int.Parse(Environment.GetEnvironmentVariable("MOCK_WHISPER_EXIT_CODE") ?? "0");
        Environment.Exit(exitCode);
    }
}
```

**Build:**
```bash
dotnet publish -c Release -r win-x64 --self-contained -o tests/Mocks/
```

**Usage in Tests:**
```csharp
[Fact]
public async Task WhisperAdapter_CallsMockCLI()
{
    // Arrange
    var adapter = new WhisperCLIAdapter("tests/Mocks/mock-whisper.exe", "model.gguf");

    // Act
    var result = await adapter.TranscribeAsync("sample.wav", "en");

    // Assert
    result.Text.Should().Be("This is a test transcript.");
}
```

---

## CI/CD Strategy for Claude Code

### GitHub Actions Workflow (Runs in Linux)

```yaml
name: .NET Build & Test

on: [push, pull_request]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET 8
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Unit Tests
      run: dotnet test --no-build --filter Category=Unit

    - name: Run Integration Tests (Mocks)
      run: dotnet test --no-build --filter Category=Integration

    - name: Run Contract Tests
      run: dotnet test --no-build --filter Category=Contract

    - name: Generate Coverage Report
      run: dotnet test --no-build --collect:"XPlat Code Coverage"

    - name: Check Coverage Threshold
      run: |
        COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' coverage.xml | head -1)
        if (( $(echo "$COVERAGE < 0.70" | bc -l) )); then
          echo "Coverage below 70%: $COVERAGE"
          exit 1
        fi
```

✅ **Runs in GitHub Actions (Linux)** - no Windows required for CI

---

## Test Execution Workflow

### During Development (in Claude Code)

```bash
# 1. Write Gherkin scenario (acceptance criteria)
# 2. Implement step definitions (test code)
# 3. Run tests (they fail - RED)
dotnet test --filter Category=Unit

# 4. Implement feature code
# 5. Run tests again (they pass - GREEN)
dotnet test --filter Category=Unit

# 6. Refactor if needed
# 7. Commit when tests pass
git add .
git commit -m "feat: [US-001] Hotkey toggles state (tests passing)"
```

### Before Iteration Complete

```bash
# Run all tests (unit + integration + contract)
dotnet test

# Verify coverage
dotnet test --collect:"XPlat Code Coverage"

# Check no regressions (run previous iteration tests)
dotnet test --filter "Category=Iter-1|Category=Iter-2"
```

### Final Verification (on Windows)

1. Deploy to Windows VM
2. Run manual test checklist
3. Run E2E tests with AppTestHarness (if time permits)
4. Document results

---

## Summary: What Can/Cannot Run in Claude Code

| Test Type | Can Run in Claude Code? | Approach |
|-----------|-------------------------|----------|
| **Unit Tests** | ✅ Yes | Pure .NET logic, no Windows APIs |
| **Integration (Mocked)** | ✅ Yes | Mock Windows APIs (IClipboardService, etc.) |
| **Contract Tests** | ✅ Yes | Test JSON parsing, file formats |
| **BDD (Mocked)** | ✅ Yes | Use mocks for Windows dependencies |
| **E2E UI Tests** | ❌ No | Requires Windows VM (manual testing) |
| **Performance Tests** | ⚠️ Partial | Measure logic, not UI responsiveness |

**Recommendation:**
- **Develop & test 90% in Claude Code** (unit, integration, contract, BDD with mocks)
- **Final 10% verification on Windows** (manual E2E, UI testing, hotkey registration)

---

## Test Data Location

```
tests/
  ├── TestData/
  │   ├── sample-3s.wav         # 3-second audio sample
  │   ├── sample-5s.wav         # 5-second audio sample
  │   ├── config-valid.toml     # Valid configuration
  │   ├── config-invalid.toml   # Invalid configuration
  │   └── stt_result.json       # Sample Whisper output
  │
  ├── Mocks/
  │   ├── mock-whisper.exe      # Mock Whisper CLI
  │   └── mock-llm.exe          # Mock LLM CLI
  │
  └── Fixtures/
      └── BaseTestFixture.cs    # Reusable test setup/teardown
```

---

## Questions for User

1. **Do you have access to a Windows VM for final verification?**
   - If yes: We can use AppTestHarness for E2E tests
   - If no: We'll rely on mocked tests + manual verification later

2. **Preferred CI/CD platform?**
   - GitHub Actions (recommended, runs on Linux)
   - Azure DevOps
   - Other?

3. **Coverage threshold?**
   - My recommendation: 70% minimum (pragmatic for v0.1)
   - Higher? Lower?

---

**Last Updated:** 2025-11-17
**Approved By:** Solution Architect (AI Agent)
**Status:** Ready for Implementation
