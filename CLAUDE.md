# LocalWhisper

**Working Name** (replaceable before v1.0)

**Portable Windows desktop app for offline speech-to-text dictation**

Platform: .NET 8 + WPF | Whisper CLI | Offline-first
Status: **Iterations 1-7 Complete (85%), Iteration 8 Pending**

**Project Structure:** `docs/architecture/project-structure.md`
**Icon Style Guide:** `docs/ui/icon-style-guide.md`
**Placeholders Tracker:** `docs/meta/placeholders-tracker.md`

---

## Quick Start

**New session? Start here:**
1. `docs/meta/claude-integration-guide.md` - AI workflow & context loading
2. `docs/iterations/iteration-plan.md` - 8-iteration roadmap
3. `docs/specification/user-stories-gherkin.md` - BDD scenarios by iteration
4. `docs/architecture/project-structure.md` - Solution & folder structure
5. `docs/ui/icon-style-guide.md` - Icon specifications

**Current Status (2025-11-19):**
- Iterations 1-7 are **fully implemented** and tested
- Iteration 8 (Stabilization + Reset + Logs) is the final remaining task
- All core functionality is working: hotkey, recording, STT, clipboard, history, flyout, wizard, settings, post-processing
- 13 previously excluded tests have been re-enabled with full test infrastructure
- Project is ~85% complete (34-48h of 40-60h estimated effort)

**Continuing implementation:**
- Focus on Iteration 8: Stabilization, reset functionality, final logging improvements
- Verify all NFRs, especially NFR-001 (p95 latency ≤ 2.5s)
- Run full test suite regression
- Prepare v0.1 release

**Manual testing:**
- Execute test scripts from `docs/testing/manual-test-script-iter{N}.md`

---

## Core Workflow

```
User holds hotkey → Audio recorded → Whisper STT → Clipboard + History + Flyout
```

**8 Iterations (sequential):**
1. Hotkey & skeleton (4-6h) ✅ **Complete**
2. Audio recording (4-6h) ✅ **Complete**
3. STT integration (6-8h) ✅ **Complete**
4. Clipboard + History + Flyout (6-10h) ✅ **Complete** ★ E2E works
5. First-run wizard (8-12h) ✅ **Complete** (5a: Wizard + 5b: Download/Repair)
6. Settings UI (4-6h) ✅ **Complete**
7. Post-processing (4-6h) ✅ **Complete**
8. Stabilization + reset (6-10h) 📋 **Pending** ★ v0.1 release

---

## ID System (Use in commits)

| Prefix | Example | Where |
|--------|---------|-------|
| `UC-###` | UC-001 (Quick dictation) | `docs/specification/use-cases.md` |
| `FR-###` | FR-010 (Hotkey registration) | `docs/specification/functional-requirements.md` |
| `NFR-###` | NFR-001 (p95 ≤ 2.5s latency) | `docs/specification/non-functional-requirements.md` |
| `ADR-####` | ADR-0001 (Platform: .NET + WPF) | `docs/adr/` |
| `US-###` | US-001 (Hotkey toggles state) | `docs/specification/user-stories-gherkin.md` |

**Traceability:** UC → FR → US → Code (update `traceability-matrix.md`)

---

## Critical Constraints (from ADRs)

- Platform: .NET 8 + WPF (no cross-platform)
- STT via CLI subprocess, NOT FFI (ADR-0002)
- Single data root folder (ADR-0003)
- Custom flyout, NOT Windows toast (ADR-0005)

**See:** `docs/adr/0000-index.md` for all decisions

---

## Implementation Rules

**Test-Driven Development (TDD) - MANDATORY:**
1. **Write tests FIRST** based on Gherkin scenarios and acceptance criteria
2. **Switch to Senior Dev/Architect role** - Review your own tests (four-eyes principle):
   - Verify tests match specifications exactly
   - Check tests cover both happy path AND edge cases
   - Ensure tests don't validate incorrect behavior
   - Identify missing test cases, performance issues, edge cases
   - Provide detailed feedback (as senior dev to yourself)
3. **Switch back to Implementation role** - Fix tests based on your own review feedback
4. **Implement** code to make tests pass
5. **Switch to Senior Dev role again** - Review your own implementation:
   - Check code quality, patterns, error handling
   - Verify implementation matches specifications
   - Identify bugs, security issues, performance problems
   - Provide detailed feedback on improvements needed
6. **Switch back to Implementation role** - Fix issues from your code review
7. **Refactor** while keeping tests green
8. **Never** write tests after implementation (prevents testing wrong behavior)

**CRITICAL:** Never stop and wait for user feedback - play both roles yourself in sequence and continue until complete!

**DO:**
- Work sequentially (Iteration 1 → 2 → 3 → ... → 8)
- Follow acceptance criteria exactly (no scope creep)
- Add structured logging (state transitions, errors, metrics)
- Update traceability matrix when adding code
- Reference US-###, FR-### in commits
- Write tests BEFORE implementation (TDD)

**DON'T:**
- Skip iterations or implement ahead
- Change requirements without updating specs
- Modify CLI contracts without updating `docs/architecture/interface-contracts.md`
- Break the ID traceability chain
- Write tests after implementation (always test-first!)

---

## Definition of Done (Every Iteration)

- [ ] All US-### acceptance criteria satisfied
- [ ] BDD scenarios `@Iter-{N}` pass
- [ ] No regressions (previous tests pass)
- [ ] Logging added for key operations
- [ ] Traceability matrix updated
- [ ] Changelog entry added
- [ ] Performance measured (if NFR applies)

**See:** Each iteration file has detailed DoD checklist

---

## Performance Targets

| Metric | Target | When |
|--------|--------|------|
| E2E latency (hotkey → clipboard) | p95 ≤ 2.5s | Iter 4, 8 |
| Flyout display | ≤ 0.5s | Iter 4 |
| Wizard completion | < 2 min | Iter 5 |

---

## Quick Reference

**Find a requirement:**
```bash
Grep: "FR-012" in docs/specification/functional-requirements.md
```

**Load iteration context:**
```bash
Read: docs/specification/user-stories-gherkin.md  # Find @Iter-{N} section
Read: docs/iterations/iteration-plan.md          # Overview & dependencies
```

**Find BDD scenarios:**
```bash
Grep: "@Iter-3" in docs/specification/user-stories-gherkin.md
```

**Check architecture:**
```bash
Read: docs/architecture/architecture-overview.md  # Components & flow
Read: docs/adr/{relevant}.md                       # Specific decisions
```

---

## When Unclear

1. **Ambiguous AC?** → Check referenced FR-###, then ADR-####, then ask user
2. **Missing detail?** → Check if intentionally flexible, document decision in commit
3. **Conflict?** → Check traceability matrix for priority, flag if unresolved

---

## Current Implementation Status

**Branch:** `claude/project-audit-1763549486`

**Completed Iterations (1-7):**
- ✅ Iteration 1: Hotkey & App Skeleton
- ✅ Iteration 2: Audio Recording (WASAPI)
- ✅ Iteration 3: STT Integration (Whisper CLI)
- ✅ Iteration 4: Clipboard + History + Flyout
- ✅ Iteration 5a: Wizard Core (File Selection, Model Verification)
- ✅ Iteration 5b: Download + Repair (HTTP Download, Repair Flow)
- ✅ Iteration 6: Settings UI (Configuration Panel)
- ✅ Iteration 7: Post-Processing (Optional LLM Integration)

**Pending:**
- 📋 Iteration 8: Stabilization + Reset + Logs

**Next Steps:**
1. Complete Iteration 8 (error handling, reset functionality, performance verification)
2. Verify all NFRs (especially p95 latency ≤ 2.5s)
3. Run full test suite regression
4. Tag v0.1 release

**Test Infrastructure:**
- All 13 previously excluded tests re-enabled (2025-11-19)
- Comprehensive test coverage for iterations 1-7
- TDD methodology followed with specifications as authority

**Last Updated:** 2025-11-19
