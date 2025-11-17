# Testing

BDD scenarios, test strategy, and test infrastructure.

---

## Files

- `test-strategy.md` - Overall testing approach
- BDD scenarios: `../specification/user-stories-gherkin.md` (Gherkin with tags)
- `test-infrastructure.md` - Test project setup, mocking

---

## Test Approach

**BDD (Primary):**
- All user stories have Gherkin scenarios (Given/When/Then)
- Tagged by @Iter-{N}, @UC-###, @FR-###
- Implement with SpecFlow (or similar)

**Test Pyramid:**
- Unit tests (Core: StateMachine, SlugGenerator, ConfigManager)
- Integration tests (CLI adapters, file I/O)
- Manual tests (E2E flow, error matrix, performance)

---

## Running Tests

```bash
# Run BDD scenarios for iteration
dotnet test --filter "Category=Iter-3"

# Run all tests (regression)
dotnet test
```

---

## BDD Scenario Tags

Find scenarios by tag:
```bash
Grep: "@Iter-3" in ../specification/user-stories-gherkin.md
Grep: "@FR-012" in ../specification/user-stories-gherkin.md
```

**Tag format:**
- `@Iter-{N}` - Iteration number
- `@UC-{ID}` - Use case
- `@FR-{ID}` - Functional requirement
- `@NFR-{ID}` - Non-functional requirement

---

## Performance Testing

**When to measure:**
- NFR-001 (E2E latency p95 ≤ 2.5s): Iteration 4, 8
- NFR-004 (Flyout ≤ 0.5s): Iteration 4
- NFR-004 (Wizard < 2 min): Iteration 5

**How:**
1. Run 100 samples (or timed wizard run)
2. Calculate p50, p95, p99
3. Document in `../changelog/v0.1-planned.md`

---

## Error Matrix (Iteration 8)

Test all error scenarios - no crashes allowed:
- Microphone access denied
- Model file missing/corrupted
- Hotkey conflict
- Disk full (tmp/ and history/)
- STT timeout
- Invalid config

**All must show user-friendly dialog and keep app stable (FR-021).**

---

## Test Coverage

- Core components: 80%+ (StateMachine, ConfigManager, SlugGenerator)
- Services: 70%+ (HotkeyManager, AudioRecorder, HistoryWriter)
- Adapters: 60%+ (WhisperCLIAdapter, PostProcessorCLIAdapter)
- All US-### acceptance criteria covered by BDD scenarios
