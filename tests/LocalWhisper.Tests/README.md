# LocalWhisper Testing Strategy

**Version:** 1.0
**Last Updated:** 2025-12-04
**Status:** Active

---

## Testing Philosophy

> **"Test behavior, not implementation. Test business logic, not UI rendering."**

This project follows **pragmatic TDD** with a clear separation between:
1. **Unit Tests** - Fast, isolated, reliable tests of business logic
2. **Integration Tests** - Tests of multi-component interactions
3. **UI Tests** - Manual or automated UI validation (future)

---

## Test Architecture

```
tests/LocalWhisper.Tests/
├── Unit/                          ← Fast, isolated business logic tests
│   ├── Core/                      ← Core domain logic (Model, Services)
│   │   ├── ModelValidatorTests.cs      ✅ GOOD EXAMPLE
│   │   ├── AudioRecorderTests.cs       ✅ GOOD EXAMPLE
│   │   └── ModelDownloaderTests.cs     ✅ GOOD EXAMPLE
│   ├── Services/                  ← Service layer tests
│   └── StateMachine/              ← State machine tests
│       └── StateMachineTests.cs        ✅ GOOD EXAMPLE
│
├── Integration/                   ← Multi-component tests
│   ├── PostProcessingIntegrationTests.cs  ✅ GOOD EXAMPLE
│   └── E2EWorkflowTests.cs        ← End-to-end scenarios (no UI)
│
├── UI/                            ← WPF UI tests (DISABLED for v0.1)
│   ├── README.md                  ← "See Manual Testing" placeholder
│   └── [Skipped until MVVM refactor]
│
└── README.md                      ← This file
```

---

## Test Categories

### ✅ Unit Tests (Priority 1 - KEEP)
**Characteristics:**
- Test **one class** in isolation
- No dependencies on WPF, file system, network (or mock them)
- Fast (< 100ms per test)
- Reliable (no flakiness)
- Can run in parallel

**Examples:**
```csharp
// ✅ GOOD: Tests business logic
[Fact]
public async Task ValidateAsync_MatchingHash_ReturnsTrue()
{
    var validator = new ModelValidator();
    var hash = ComputeHash(testFile);
    var result = await validator.ValidateAsync(testFile, hash);
    result.Should().BeTrue();
}

// ✅ GOOD: Tests state transitions
[Fact]
public void StateMachine_IdleToRecording_IsValid()
{
    var sm = new AppStateMachine();
    sm.TransitionTo(AppState.Recording);
    sm.CurrentState.Should().Be(AppState.Recording);
}
```

**Current Unit Tests:**
- `ModelValidatorTests.cs` - SHA-1 hash validation ✅
- `AudioRecorderTests.cs` - WASAPI audio recording ✅
- `ModelDownloaderTests.cs` - HTTP download with retries ✅
- `StateMachinePostProcessingTests.cs` - State transitions ✅

**Target:** 60+ tests, 100% core logic coverage

---

### ✅ Integration Tests (Priority 2 - KEEP)
**Characteristics:**
- Test **multiple components** working together
- May use real file system, but no UI
- Moderate speed (< 1s per test)
- Use real implementations, not mocks

**Examples:**
```csharp
// ✅ GOOD: Tests end-to-end workflow without UI
[Fact]
public async Task E2E_PostProcessing_TransformsText()
{
    var config = new AppConfig { PostProcessing = { Enabled = true } };
    var processor = new PostProcessor(config);
    var result = await processor.ProcessAsync("raw text");
    result.Should().Contain("formatted");
}
```

**Current Integration Tests:**
- `PostProcessingIntegrationTests.cs` - LLM post-processing ✅

**Target:** 10-15 tests for critical paths

---

### ⚠️ UI Tests (Priority 3 - SKIP for v0.1)
**Status:** **DISABLED** - Covered by manual testing

**Why Skipped:**
1. **Architectural Issue:** Tests currently test `SettingsWindow` directly (WPF)
2. **Fragile:** WPF threading causes crashes in xUnit
3. **Slow:** Creating windows takes seconds
4. **Wrong Layer:** Should test ViewModels, not Views

**Current UI Tests (DISABLED):**
- `ModelVerificationTests.cs` - SettingsWindow model verification ❌ SKIP
- `DataRootChangeTests.cs` - SettingsWindow data root changes ❌ SKIP
- `FileFormatChangeTests.cs` - SettingsWindow format selection ❌ SKIP
- `HotkeyChangeTests.cs` - SettingsWindow hotkey changes ❌ SKIP
- `LanguageChangeTests.cs` - SettingsWindow language selection ❌ SKIP
- `RestartLogicTests.cs` - SettingsWindow restart logic ❌ SKIP
- `SettingsPersistenceTests.cs` - SettingsWindow save/load ❌ SKIP
- `SettingsWindowTests.cs` - SettingsWindow initialization ❌ SKIP

**Coverage:** Manual testing via `docs/testing/manual-test-script-iter6.md`

**Future (v1.0+):** Refactor to MVVM, test ViewModels instead

---

## Test Naming Convention

```
MethodName_Scenario_ExpectedBehavior
```

**Examples:**
```csharp
ValidateAsync_MatchingHash_ReturnsTrue()          ✅ Clear
ValidateAsync_FileNotFound_ThrowsException()      ✅ Clear
SaveConfig_ValidPath_PersistsToFile()             ✅ Clear

Test1()                                            ❌ Unclear
ValidateTest()                                     ❌ Unclear
```

---

## Test Organization

### Arrange-Act-Assert Pattern
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var validator = new ModelValidator();
    var testFile = CreateTestFile("content");
    var expectedHash = ComputeHash("content");

    // Act - Execute the method under test
    var result = validator.ValidateModel(testFile, expectedHash);

    // Assert - Verify the outcome
    result.IsValid.Should().BeTrue();
    result.Message.Should().Contain("matches");
}
```

### Use FluentAssertions
```csharp
// ✅ GOOD: Readable, clear failure messages
result.Should().BeTrue();
list.Should().HaveCount(3);
config.Whisper.ModelPath.Should().Be(expectedPath);

// ❌ BAD: Poor failure messages
Assert.True(result);
Assert.Equal(3, list.Count);
```

---

## Mocking Strategy

### When to Mock
- External dependencies (HTTP, file system)
- Slow operations (database, network)
- Non-deterministic behavior (random, time)

### When NOT to Mock
- Simple data classes (DTOs, configs)
- Your own business logic (test it for real!)
- Value objects

**Example:**
```csharp
// ✅ GOOD: Mock external HTTP client
var mockHttp = new Mock<HttpClient>();
mockHttp.Setup(c => c.GetAsync(url)).ReturnsAsync(response);

// ✅ GOOD: Use real config object
var config = new AppConfig { Language = "de" };

// ❌ BAD: Mocking your own logic defeats the purpose
var mockValidator = new Mock<ModelValidator>();
mockValidator.Setup(v => v.Validate()).Returns(true);  // What are we testing?
```

---

## Running Tests

### Run All Unit Tests (Fast)
```bash
dotnet test --filter "Category!=WpfIntegration"
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~ModelValidatorTests"
```

### Run Single Test
```bash
dotnet test --filter "FullyQualifiedName~ValidateAsync_MatchingHash_ReturnsTrue"
```

### Run with Coverage (Future)
```bash
dotnet test /p:CollectCoverage=true /p:CoverageReportsFormat=lcov
```

---

## Test Requirements

### Every Test Must:
- ✅ Have a clear name describing scenario and outcome
- ✅ Follow Arrange-Act-Assert structure
- ✅ Test ONE thing (single responsibility)
- ✅ Be independent (no shared state)
- ✅ Be fast (< 100ms for unit tests)
- ✅ Be reliable (no random failures)

### Every Test Must NOT:
- ❌ Test implementation details (private methods, internal state)
- ❌ Depend on test execution order
- ❌ Create real windows or UI components (use ViewModels)
- ❌ Use `Thread.Sleep()` or arbitrary timeouts
- ❌ Access production databases or external APIs

---

## Definition of Done

A feature is **not complete** until:
1. ✅ Unit tests written **before** implementation (TDD)
2. ✅ All tests pass
3. ✅ Code coverage ≥ 80% for business logic
4. ✅ Integration test covers happy path
5. ✅ Manual UI test script updated (if UI changes)

---

## Current Test Status (v0.1)

| Category | Count | Status | Coverage |
|----------|-------|--------|----------|
| **Unit Tests** | 60+ | ✅ Passing | ~80% core logic |
| **Integration Tests** | 6 | ✅ Passing | Critical paths |
| **UI Tests** | 56 | ⚠️ Skipped | Manual testing |
| **Total** | 66 | ✅ CI Green | Shipping v0.1 |

**Test Execution Time:** ~10 seconds (excluding UI tests)

---

## Refactoring Roadmap (v1.0+)

### Phase 1: MVVM Refactor
**Goal:** Separate business logic from UI

**Before:**
```csharp
// ❌ BAD: Testing WPF window directly
var window = new SettingsWindow(config);
window.ModelPathText.Text.Should().Contain(path);
```

**After:**
```csharp
// ✅ GOOD: Testing ViewModel (pure C#)
var viewModel = new SettingsViewModel(config);
viewModel.ModelPath.Should().Be(path);
viewModel.HasErrors.Should().BeFalse();
```

### Phase 2: Re-enable UI Test Scenarios as ViewModel Tests
- Extract `SettingsViewModel` from `SettingsWindow`
- Move validation logic to ViewModel
- Rewrite 56 UI tests as ViewModel tests (fast, reliable)

### Phase 3: Minimal E2E UI Tests
- Use UI automation (WinAppDriver or Appium)
- Test 3-5 critical user journeys only
- Run separately from unit/integration tests

**Timeline:** Post v0.1 release

---

## Resources

- **FluentAssertions Docs:** https://fluentassertions.com/
- **xUnit Best Practices:** https://xunit.net/docs/best-practices
- **MVVM Pattern:** See `docs/architecture/mvvm-pattern.md` (TODO)
- **Manual Test Scripts:** `docs/testing/manual-test-script-iter*.md`

---

## FAQ

### Q: Why are UI tests disabled?
**A:** They test WPF windows directly (integration tests), which is fragile and slow. We use manual testing for v0.1, will refactor to MVVM for v1.0.

### Q: How do we ensure UI quality without tests?
**A:** Manual testing using documented test scripts. UI bugs are less critical than logic bugs for v0.1.

### Q: When will UI tests be re-enabled?
**A:** After MVVM refactor (v1.0+), we'll test ViewModels (fast) instead of Views (slow).

### Q: What's the test coverage target?
**A:** 80% coverage for core business logic. 100% for critical paths (STT, audio, config).

### Q: Can I run UI tests locally?
**A:** Yes, remove `--filter "Category!=WpfIntegration"`, but expect crashes/hangs. Not recommended.

---

## Contact

Questions about testing strategy? See:
- `CLAUDE.md` - Project guidelines
- `docs/testing/` - Manual test scripts
- This README - Testing architecture

**Last Review:** 2025-12-04 (Initial version for v0.1 release)
