# Iteration Execution Guide

**Audience:** Claude Code sessions executing implementation iterations
**Purpose:** Step-by-step protocol for implementing each iteration
**Status:** Prescriptive process guide

---

## Overview

This guide provides a **repeatable process** for executing any iteration in the implementation plan. Each iteration builds on the previous one and delivers a vertical slice of functionality.

**Key principles:**
- Work sequentially (Iteration 1 → 2 → 3 → ...)
- Complete Definition of Done before moving forward
- Maintain traceability at every step
- Test continuously (BDD scenarios + manual verification)
- Update documentation as you discover details

---

## Pre-Iteration Checklist

Before starting Iteration N:

```
[ ] All previous iterations (1 to N-1) are complete and merged
[ ] Read: docs/iterations/iteration-{N}-*.md completely
[ ] Read: docs/iterations/dependency-graph.yaml (check prerequisites)
[ ] Load: All referenced FR-###, NFR-###, UC-### from specification/
[ ] Load: All referenced ADR-#### from adr/
[ ] Understand: Interface contracts (if applicable) from architecture/interface-contracts.md
[ ] Review: BDD scenarios tagged @Iter-{N} in testing/bdd-feature-seeds.md
[ ] Confirm: No blocking dependencies remain
```

**Estimated time:** 15-30 minutes

---

## Iteration Execution Process

### Phase 1: Analysis & Planning (15-30 min)

**Step 1.1: Extract User Stories**
```markdown
Read: docs/iterations/iteration-{N}-*.md
List: All US-### in the iteration (e.g., US-001, US-002, US-003)
For each story:
  - Note the description
  - Extract acceptance criteria (AC: or Fit:)
  - Identify measurable targets (if any)
  - Note referenced requirements (FR/NFR/UC)
```

**Step 1.2: Map Requirements**
```markdown
Create a working note:

US-001: Hotkey toggles state
  - FR-010: Hotkey registration
  - UC-001: Quick dictation
  - AC: KeyDown → State=Recording (log present)
       KeyUp → State=Idle (log present)
  - Tests: @Iter-1 @FR-010

US-002: Tray icon shows status
  - FR-010: Hotkey registration
  - AC: Icon changes when state changes
  ...
```

**Step 1.3: Identify Technical Tasks**
```markdown
Break each US-### into implementable tasks:

US-001:
  - [ ] Create HotkeyManager class
  - [ ] Register global hotkey via Win32 API
  - [ ] Implement state machine (Idle ↔ Recording)
  - [ ] Add logging for state transitions
  - [ ] Wire up to main app loop
  - [ ] Handle hotkey conflicts (error dialog)

US-002:
  - [ ] Create TrayIconManager class
  - [ ] Load icon assets (Idle, Recording)
  - [ ] Subscribe to state change events
  - [ ] Update icon on state change
```

**Step 1.4: Check Architecture Constraints**
```markdown
Review relevant ADRs:
- Platform (.NET 8 + WPF)
- Coding patterns (MVVM? Service pattern?)
- Error handling strategy (FR-021)
- Logging format (FR-023)
```

---

### Phase 2: Implementation (2-6 hours, varies by iteration)

**Step 2.1: Set Up Project Structure (if first iteration)**
```bash
# If starting Iteration 1
mkdir -p src/{Core,Services,UI,Adapters}
mkdir -p tests/{Unit,Integration,Features}
# Create solution/project files
# Set up logging framework
# Configure dependency injection (if used)
```

**Step 2.2: Implement User Stories (TDD approach)**

For each US-### in priority order:

```
1. Write failing test (BDD scenario or unit test)
2. Implement minimum code to pass test
3. Refactor for clarity and maintainability
4. Add logging for key operations
5. Verify acceptance criteria
6. Commit with message: "feat(iter-{N}): [US-###] Description"
```

**Example flow for US-001:**
```bash
# 1. Write BDD scenario (already provided in bdd-feature-seeds.md)
# Implement step definitions in tests/StepDefinitions/

# 2. Implement HotkeyManager
# src/Services/HotkeyManager.cs
class HotkeyManager {
  RegisterHotkey()
  UnregisterHotkey()
  OnHotkeyDown event
  OnHotkeyUp event
}

# 3. Implement StateMachine
# src/Core/StateMachine.cs
enum AppState { Idle, Recording, Processing }
class StateMachine {
  Transition(AppState newState)
  CurrentState property
}

# 4. Wire up in main app
# src/App.xaml.cs
hotkeyManager.OnHotkeyDown += () => stateMachine.Transition(AppState.Recording);
hotkeyManager.OnHotkeyUp += () => stateMachine.Transition(AppState.Idle);

# 5. Add logging
logger.LogInformation("State transition: {From} -> {To}", oldState, newState);

# 6. Test manually and with BDD
dotnet test --filter "Category=Iter-1"

# 7. Commit
git add .
git commit -m "feat(iter-1): [US-001] Hotkey toggles state

- Implements HotkeyManager with Win32 global hotkey
- State machine transitions Idle ↔ Recording
- Logging added for state transitions
- Satisfies FR-010, AC verified

Tests: @Iter-1 @FR-010 passing"
```

**Step 2.3: Handle Error Cases**

For each user story, implement error paths per FR-021:
```csharp
try {
    hotkeyManager.RegisterHotkey(config.Hotkey);
} catch (HotkeyAlreadyRegisteredException ex) {
    logger.LogError(ex, "Hotkey conflict: {Hotkey}", config.Hotkey);
    ShowDialog("Hotkey bereits belegt", "Bitte wählen Sie einen anderen Hotkey in den Einstellungen.");
    // App remains stable
}
```

**Step 2.4: Add Observability**

Per FR-023 and NFR-006:
```csharp
// Structured logging with context
logger.LogInformation("Hotkey registered: {Hotkey}, Modifiers: {Modifiers}",
    hotkey, modifiers);

logger.LogInformation("State transition: {From} -> {To}, Timestamp: {Ts}",
    AppState.Idle, AppState.Recording, DateTime.UtcNow);

// Performance tracking (for NFR-001, NFR-004)
var sw = Stopwatch.StartNew();
// ... operation ...
logger.LogInformation("Operation_Duration_Ms: {Ms}, Operation: {Name}",
    sw.ElapsedMilliseconds, "HotkeyRegistration");
```

---

### Phase 3: Testing (1-2 hours)

**Step 3.1: Run BDD Scenarios**
```bash
# Find scenarios for this iteration
grep "@Iter-{N}" docs/testing/bdd-feature-seeds.md

# Run tests
dotnet test --filter "Category=Iter-{N}"

# Verify all scenarios pass
```

**Step 3.2: Manual Verification**
```markdown
Test each acceptance criterion manually:

US-001 AC: "KeyDown → State=Recording"
  [ ] Press hotkey → Tray icon changes to recording
  [ ] Log shows: "State transition: Idle -> Recording"
  [ ] Release hotkey → Icon changes back
  [ ] Log shows: "State transition: Recording -> Idle"

US-003 AC: "Hotkey conflict shows dialog"
  [ ] Start app with hotkey already registered by another app
  [ ] Dialog appears with clear message
  [ ] App does not crash
  [ ] Log shows error with context
```

**Step 3.3: Performance Testing (if NFR applies)**
```bash
# For iterations with NFR-001 or NFR-004 targets
# Run profiler or manual timing
# Measure p50, p95, p99
# Document in changelog

# Example:
echo "Measuring latency for Iteration 4..."
# Run 100 dictations, collect timings
# Calculate percentiles
# Verify p95 ≤ 2.5s
```

**Step 3.4: Regression Testing**
```bash
# Run all previous iteration tests
dotnet test --filter "Category=Iter-1|Category=Iter-2|...|Category=Iter-{N}"

# Ensure no breakage
```

---

### Phase 4: Documentation & Traceability (30 min)

**Step 4.1: Update Traceability Matrix**
```markdown
Edit: docs/specification/traceability-matrix.md

Add entries for newly implemented code:

| FR-010 | Hotkey registration | Implemented | src/Services/HotkeyManager.cs | Iter-1 | US-001 |
| FR-010 | State machine       | Implemented | src/Core/StateMachine.cs      | Iter-1 | US-001 |
```

**Step 4.2: Update Changelog**
```markdown
Edit: docs/changelog/v0.1-planned.md

## Iteration 1 (2025-09-18)

**Implemented:**
- US-001: Hotkey toggles state (FR-010, UC-001)
- US-002: Tray icon reflects state (FR-010)
- US-003: Hotkey conflict error dialog (FR-021)

**Tests:**
- BDD: @Iter-1 @FR-010 scenarios passing
- Manual: All acceptance criteria verified

**Metrics:**
- N/A (no performance targets this iteration)

**Notes:**
- Hotkey uses Win32 RegisterHotKey API
- State machine is simple enum with transition logging
```

**Step 4.3: Update Documentation (if needed)**
```markdown
IF you discovered ambiguities or made decisions:
  - Add TODO in relevant spec file, OR
  - Propose new ADR if architectural, OR
  - Add note in iteration file under "Implementation notes"

Example:
<!-- In docs/iterations/iteration-01-hotkey-skeleton.md -->

### Implementation Notes (added 2025-09-18)
- Hotkey conflict detection uses `ERROR_HOTKEY_ALREADY_REGISTERED` from Win32
- State machine is currently in-memory only (no persistence needed per spec)
```

**Step 4.4: Verify Specification Alignment**
```markdown
Double-check:
[ ] No FR-### was changed without updating specification/functional-requirements.md
[ ] No NFR-### target was violated
[ ] No UC-### flow was altered
[ ] All ADR-#### constraints were respected
```

---

### Phase 5: Definition of Done (15 min)

**Step 5.1: Check DoD Checklist**

From the iteration file, verify:
```
[ ] Code implements all US-### acceptance criteria
[ ] All BDD scenarios tagged @Iter-{N} pass
[ ] All previous iteration tests still pass (no regression)
[ ] Logging added for state transitions, errors, and key operations
[ ] Traceability matrix updated with new code modules
[ ] Changelog entry added
[ ] Performance metrics measured (if NFR-### applies)
[ ] Error handling implemented per FR-021
[ ] Specification documents reviewed (updated if needed)
```

**Step 5.2: Code Review (self)**
```markdown
Review your own code:
[ ] Follows .NET conventions and project style
[ ] No hardcoded paths or magic numbers
[ ] Error messages are user-friendly (German, if applicable)
[ ] Logging uses structured format (key-value pairs)
[ ] No secrets or temp data committed
[ ] Tests are readable and maintainable
```

**Step 5.3: Commit & Push**
```bash
git add .
git commit -m "feat(iter-{N}): Complete iteration {N} - {Brief title}

Implemented:
- US-001: ...
- US-002: ...

Tests: @Iter-{N} scenarios passing
DoD: [x] Code [x] Tests [x] Logs [x] Docs [x] Traceability

Satisfies: FR-010, FR-021, UC-001
See: docs/iterations/iteration-{N}-*.md"

git push origin <branch-name>
```

---

## Post-Iteration Checklist

After completing Iteration N:

```
[ ] All US-### in iteration are implemented and tested
[ ] Definition of Done is satisfied
[ ] Traceability matrix is updated
[ ] Changelog entry is added
[ ] No TODO items left unresolved (or documented for later)
[ ] Code is committed and pushed
[ ] Ready to start Iteration N+1
```

---

## Handling Common Issues

### Issue: Acceptance criterion is ambiguous

**Solution:**
1. Check the referenced FR-### for more detail
2. Check ADR-#### for guidance
3. Look for "Fit:" criteria with measurable targets
4. If still unclear: ASK (via AskUserQuestion tool)
5. Document the clarification in iteration file

### Issue: Technical blocker (e.g., library doesn't support feature)

**Solution:**
1. Check if ADR-#### constrains the approach
2. Evaluate alternative implementations
3. If it changes architecture: propose new ADR
4. If it changes requirements: flag for review
5. Document workaround or decision

### Issue: Test is flaky or environment-specific

**Solution:**
1. Add retry logic if appropriate (e.g., timing-based tests)
2. Document environmental prerequisites in test
3. Use mocks/stubs for external dependencies
4. If unsolvable: document as known limitation

### Issue: Performance target (NFR) is missed

**Solution:**
1. Profile to find bottleneck
2. Check if assumption was wrong (document in ADR or spec)
3. Optimize critical path
4. If target is unrealistic: flag for NFR-### adjustment
5. Document actual performance in changelog

---

## Tips for Efficient Execution

**Batch related tasks:**
- Read all iteration context at once (avoid thrashing)
- Implement related US-### together (e.g., all error handling)
- Run tests in bulk after each feature

**Use templates:**
- Copy BDD scenario structure from seeds
- Use consistent commit message format
- Use logging template from FR-023 examples

**Stay focused:**
- Don't implement features from future iterations
- Don't refactor code outside the current iteration scope
- Don't add "nice to have" features not in the plan

**Ask early:**
- If an AC is unclear, ask immediately
- If a dependency is missing, flag it
- If performance seems off, measure and report

---

## Iteration Time Estimates

Based on complexity:

| Iteration | Focus | Estimated Time |
|-----------|-------|----------------|
| 1 | Hotkey & skeleton | 4-6 hours |
| 2 | Audio recording | 4-6 hours |
| 3 | STT integration | 6-8 hours |
| 4 | Clipboard + History + Flyout | 6-10 hours |
| 5 | Wizard + Model check | 8-12 hours |
| 6 | Settings UI | 4-6 hours |
| 7 | Post-processing | 4-6 hours |
| 8 | Stabilization + Reset | 6-10 hours |

**Total:** ~40-60 hours of implementation time

---

## Summary: Quick Reference

**Before starting:**
→ Read iteration file, dependencies, specs, ADRs

**During implementation:**
→ Follow TDD, add logging, handle errors, commit frequently

**During testing:**
→ Run BDD scenarios, verify AC manually, check performance

**Before finishing:**
→ Update traceability, changelog, DoD checklist, commit

**Ready for next iteration:**
→ All DoD items checked, code pushed

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial process)
