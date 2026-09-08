# Iteration Plan

**Purpose:** Complete implementation roadmap organized into vertical slices
**Format:** 8 iterations, each delivering end-to-end value
**Status:** Iterations 1-7 complete, Iteration 8 not started. Project paused since December 2025 (no Windows machine); see `docs/meta/handover.md`.
**Last Updated:** 2025-11-19

---

## Overview

This project is organized into **8 iterations**, each implementing a vertical slice of functionality. Every iteration builds on the previous ones and delivers testable, demonstrable value.

**Total estimated effort:** 40-60 hours (solo developer)

**Principles:**
- **Vertical slices:** Each iteration delivers end-to-end functionality (not horizontal layers)
- **INVEST stories:** Independent, Negotiable, Valuable, Estimable, Small, Testable
- **Definition of Done:** Code + Tests + Docs updated + Metrics measured (where applicable)
- **No skipping:** Iterations must be completed sequentially (dependencies exist)

---

## Iteration Summary

| # | Focus | Key Deliverables | Effort | Status |
|---|-------|------------------|--------|--------|
| [1](iteration-01-hotkey-skeleton.md) | Hotkey & App Skeleton | Tray app, hotkey registration, state machine | 4-6h | ✅ Complete |
| [2](iteration-02-audio-recording.md) | Audio Recording | WASAPI recording, WAV file generation | 4-6h | ✅ Complete |
| [3](iteration-03-stt-whisper.md) | STT Integration | Whisper CLI adapter, JSON parsing | 6-8h | ✅ Complete |
| [4](iteration-04-clipboard-history-flyout.md) | Clipboard + History + Flyout | End-to-end dictation flow complete | 6-10h | ✅ Complete |
| [5a](iteration-05a-wizard-core.md) | Wizard Core (File Selection) | Wizard UI, model verification (SHA-1), hotkey picker | 4-6h | ✅ Complete |
| [5b](iteration-05b-download-repair.md) | Download + Repair | HTTP download, progress tracking, repair flow | 4-6h | ✅ Complete |
| [6](iteration-06-settings.md) | Settings UI | Configuration panel | 4-6h | ✅ Complete |
| [7](iteration-07-post-processing.md) | Optional Post-Processing | LLM integration (optional) | 4-6h | ✅ Complete |
| [8](iteration-08-stabilization-reset.md) | Stabilization + Reset + Logs | Error handling, reset, performance verification | 6-10h | 📋 Pending |

**Total:** ~40-60 hours

**Note:** Iteration 5 was split into 5a (wizard core) and 5b (download + repair) to keep iterations manageable (4-6h each).

---

## Dependency Graph

```
Iteration 1 (Hotkey & State) ✅
  ├─→ Iteration 2 (Audio) ✅
  │     └─→ Iteration 3 (STT) ✅
  │           └─→ Iteration 4 (Clipboard/History/Flyout) ★ E2E Complete ✅
  │                 ├─→ Iteration 5a (Wizard Core) ✅
  │                 │     ├─→ Iteration 5b (Download/Repair) ✅
  │                 │     └─→ Iteration 6 (Settings) ✅
  │                 ├─→ Iteration 7 (Post-Processing) ✅
  │                 └─→ Iteration 8 (Stabilization) ★ v0.1 Release 📋
  │
  └─→ [All iterations depend on Iteration 1 foundational work]

★ Major milestones
✅ Complete | 📋 Pending
```

**Critical Path:** 1 → 2 → 3 → 4 → 5a → 5b → 8

---

## Iteration Milestones

### Milestone 1: E2E Dictation (After Iteration 4)

**Deliverables:**
- User can hold hotkey → speak → release → text in clipboard
- History file is created
- Flyout confirms completion
- p95 latency measured

**Verification:**
- Demo: Dictate "Let me check on that" → paste into Notepad
- BDD scenarios tagged @Iter-1 through @Iter-4 pass
- NFR-001 (latency ≤ 2.5s p95) measured and documented

---

### Milestone 2: Complete Setup (After Iteration 5)

**Deliverables:**
- First-run wizard functional
- Model verification works
- Data root is configurable
- Repair flow handles missing data root

**Verification:**
- Test on fresh Windows VM
- Wizard completes in < 2 min (NFR-004)
- UC-002 (First-Run Installation) fully implemented

---

### Milestone 3: v0.1 Release (After Iteration 8)

**Deliverables:**
- All functional requirements implemented
- All NFRs verified
- Error handling robust (NFR-003)
- Reset/uninstall works
- README and changelog complete

**Verification:**
- All BDD scenarios pass
- Error matrix tested (no crashes)
- p95 latency re-measured
- Release tagged in git

---

## User Stories (All Iterations)

**Total:** ~45 user stories

| Iteration | Story IDs | Count | Status |
|-----------|-----------|-------|--------|
| 1 | US-001, US-002, US-003 | 3 | ✅ Complete |
| 2 | US-010, US-011, US-012 | 3 | ✅ Complete |
| 3 | US-020, US-021, US-022, US-023 | 4 | ✅ Complete |
| 4 | US-030..036 | 7 | ✅ Complete |
| 5a | US-040, US-041a, US-042, US-045, US-046 | 5 | ✅ Complete |
| 5b | US-041b, US-043, US-044 | 3 | ✅ Complete |
| 6 | US-050..059 | 10 | ✅ Complete |
| 7 | US-060..064 | 5 | ✅ Complete |
| 8 | US-070..076 | 7 | 📋 Pending |

**Note:** US-041 was split into US-041a (file selection, Iteration 5a) and US-041b (HTTP download, Iteration 5b).

---

## Requirements Coverage

### By Iteration

| Iteration | FRs Covered | NFRs Verified | Status |
|-----------|-------------|---------------|--------|
| 1 | FR-010, FR-021, FR-023 | NFR-002, NFR-006 | ✅ Complete |
| 2 | FR-011, FR-021, FR-023 | NFR-006 | ✅ Complete |
| 3 | FR-012, FR-021, FR-023 | NFR-001 (partial), NFR-005, NFR-006 | ✅ Complete |
| 4 | FR-013, FR-014, FR-015, FR-024, FR-023 | NFR-001, NFR-004, NFR-006 | ✅ Complete |
| 5a | FR-016, FR-017 (file selection), FR-021, FR-023 | NFR-006 | ✅ Complete |
| 5b | FR-017 (download), FR-016 (repair), FR-021, FR-023 | NFR-004, NFR-006 | ✅ Complete |
| 6 | FR-020, FR-017, FR-023 | NFR-006 | ✅ Complete |
| 7 | FR-022, FR-023 | NFR-006 | ✅ Complete |
| 8 | FR-019, FR-021, FR-023 | NFR-001 (final), NFR-003, NFR-004, NFR-006 | 📋 Pending |

**Total Coverage:** All 14 FRs and 6 NFRs addressed.

---

## Testing Strategy

### BDD (Behavior-Driven Development)

**Tool:** SpecFlow or similar (Gherkin syntax)

**Feature Files:** See `testing/bdd-feature-seeds.md`

**Tags:**
- `@Iter-{N}`: Scenarios for iteration N
- `@UC-{ID}`: Scenarios for use case
- `@FR-{ID}`: Scenarios for functional requirement
- `@NFR-{ID}`: Scenarios for non-functional requirement

**Execution:**
- Run `@Iter-{N}` scenarios during iteration N
- Run all previous scenarios as regression tests

---

### Unit & Integration Tests

**Unit Tests:**
- SlugGenerator: Test normalization rules
- ConfigManager: Test TOML parsing/validation
- StateMachine: Test state transitions

**Integration Tests:**
- WhisperCLIAdapter: Test with mock CLI executable
- AudioRecorder: Test WAV file creation
- HistoryWriter: Test file creation with front-matter

---

### Manual Testing

**Performance:**
- p95 latency measurement (100 dictations)
- Flyout latency measurement
- Wizard completion time

**Error Scenarios:**
- Microphone denied
- Model hash mismatch
- Hotkey conflict
- Disk full

**Usability:**
- Wizard flow (new user perspective)
- Settings changes
- Reset/uninstall

---

## Definition of Done (Per Iteration)

**Checklist:**
- [ ] All user stories (US-###) implemented and satisfy acceptance criteria
- [ ] All BDD scenarios tagged `@Iter-{N}` pass
- [ ] Unit/integration tests pass (if applicable)
- [ ] Logging added for key state transitions and errors
- [ ] Traceability matrix updated (code modules added)
- [ ] Changelog entry added
- [ ] Performance metrics measured (if NFR applies to this iteration)
- [ ] Error handling implemented per FR-021
- [ ] Specification documents reviewed (updated if needed)
- [ ] No regressions: All previous iteration tests still pass

---

## Iteration Handoff Template

**After completing each iteration:**

1. **Verify DoD** checklist
2. **Commit** code with message referencing iteration and stories
3. **Update** traceability matrix with new code modules
4. **Add** changelog entry
5. **Tag** commit (e.g., `iter-1-complete`)
6. **Document** any issues or decisions for next iteration

---

## Risk Management Per Iteration

### Iteration 1
**Risk:** Hotkey conflicts → **Mitigation:** Clear error detection

### Iteration 2
**Risk:** Audio device errors → **Mitigation:** Robust error handling

### Iteration 3
**Risk:** STT latency exceeds target → **Mitigation:** Measure and optimize if needed

### Iteration 4
**Risk:** p95 latency target missed → **Mitigation:** Profile bottlenecks, parallel writes

### Iteration 5
**Risk:** Wizard is too complex → **Mitigation:** User testing, simplify flow

### Iteration 8
**Risk:** Memory leaks in long-running app → **Mitigation:** Profiling, dispose patterns

---

## Detailed Iteration Files

Each iteration has a dedicated file with:
- User stories with acceptance criteria
- BDD scenario seeds
- Implementation guidance
- Definition of Done

**Files:**
- [iteration-01-hotkey-skeleton.md](iteration-01-hotkey-skeleton.md)
- [iteration-02-audio-recording.md](iteration-02-audio-recording.md)
- [iteration-03-stt-whisper.md](iteration-03-stt-whisper.md)
- [iteration-04-clipboard-history-flyout.md](iteration-04-clipboard-history-flyout.md)
- [iteration-05-wizard-repair.md](iteration-05-wizard-repair.md)
- [iteration-06-settings.md](iteration-06-settings.md)
- [iteration-07-post-processing.md](iteration-07-post-processing.md)
- [iteration-08-stabilization-reset.md](iteration-08-stabilization-reset.md)

---

## Related Documents

- **Traceability Matrix:** `specification/traceability-matrix.md`
- **BDD Seeds:** `testing/bdd-feature-seeds.md`
- **Test Strategy:** `testing/test-strategy.md`
- **Requirements:** `specification/functional-requirements.md`, `specification/non-functional-requirements.md`
- **Architecture:** `architecture/architecture-overview.md`

---

## Implementation Progress

**Current Status as of 2025-11-19:**
- **Iterations 1-7**: ✅ Complete (85% of total effort)
- **Iteration 8**: 📋 Pending (Stabilization + Reset + Logs)
- **Project Completion**: ~85% (34-48h of 40-60h estimated)

**Milestone 1 (E2E Dictation)**: ✅ Achieved after Iteration 4
**Milestone 2 (Complete Setup)**: ✅ Achieved after Iteration 5
**Milestone 3 (v0.1 Release)**: 📋 Pending Iteration 8 completion

**Next Steps:**
1. Complete Iteration 8 (Stabilization + Reset + Logs)
2. Verify all NFRs (especially NFR-001 p95 latency)
3. Run full test suite regression
4. Tag v0.1 release

---

**Last updated:** 2025-11-19
**Version:** v0.2 (Reflects iterations 1-7 completion)
