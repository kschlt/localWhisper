# LocalWhisper

**Working Name** (replaceable before v1.0)

**Portable Windows desktop app for offline speech-to-text dictation**

Platform: .NET 8 + WPF | Whisper CLI | Offline-first
Status: **Documentation complete, ready for Iteration 1**

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

**Continuing implementation:**
- Check current iteration in `docs/iterations/` (e.g., `iteration-01-hotkey-skeleton.md`)
- Load relevant FR/NFR/ADR as needed
- Update `docs/specification/traceability-matrix.md` after adding code
- Track placeholders in `docs/meta/placeholders-tracker.md`

**Manual testing:**
- Execute test scripts from `docs/testing/manual-test-script-iter{N}.md`

---

## Core Workflow

```
User holds hotkey → Audio recorded → Whisper STT → Clipboard + History + Flyout
```

**8 Iterations (sequential):**
1. Hotkey & skeleton (4-6h) ← **START HERE**
2. Audio recording (4-6h)
3. STT integration (6-8h)
4. Clipboard + History + Flyout (6-10h) ★ E2E complete
5. First-run wizard (8-12h)
6. Settings UI (4-6h)
7. Post-processing (4-6h)
8. Stabilization + reset (6-10h) ★ v0.1 release

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

**DO:**
- Work sequentially (Iteration 1 → 2 → 3 → ... → 8)
- Follow acceptance criteria exactly (no scope creep)
- Add structured logging (state transitions, errors, metrics)
- Update traceability matrix when adding code
- Reference US-###, FR-### in commits

**DON'T:**
- Skip iterations or implement ahead
- Change requirements without updating specs
- Modify CLI contracts without updating `docs/architecture/interface-contracts.md`
- Break the ID traceability chain

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

**Current Branch:** claude/add-cloudmd-files-01Y6TNZNHQP2sCvptAYcR3Rr
**Next:** Start Iteration 1 (Hotkey & skeleton)
