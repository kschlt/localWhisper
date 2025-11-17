# Implementation Iterations

8 sequential iterations, each delivering end-to-end value.

---

## Files

- `iteration-plan.md` - Full roadmap, dependencies, milestones
- `dependency-graph.yaml` - Story-level dependencies
- User stories: `../specification/user-stories-gherkin.md` (tagged by @Iter-{N})

---

## Roadmap

| # | Focus | Effort | Milestone |
|---|-------|--------|-----------|
| 1 | Hotkey & skeleton | 4-6h | **← START HERE** |
| 2 | Audio recording | 4-6h | |
| 3 | STT integration | 6-8h | |
| 4 | Clipboard + History + Flyout | 6-10h | ★ E2E complete |
| 5 | Wizard + repair | 8-12h | |
| 6 | Settings UI | 4-6h | |
| 7 | Post-processing | 4-6h | |
| 8 | Stabilization + reset | 6-10h | ★ v0.1 release |

**Must work sequentially:** 1 → 2 → 3 → ... → 8

---

## Iteration Workflow

**1. Load context:**
```bash
Read: ../specification/user-stories-gherkin.md  # Find @Iter-{N} section
Read: iteration-plan.md                          # Check dependencies
```

**2. For each user story:**
- Read Gherkin scenario (Given/When/Then)
- Load referenced FR-###, NFR-###, ADR-####
- Write failing test (BDD step definitions)
- Implement feature to pass test
- Add structured logging
- Commit with message: `feat(iter-{N}): [US-###] Description`

**3. Verify DoD:**
- [ ] All US-### acceptance criteria satisfied
- [ ] BDD scenarios `@Iter-{N}` pass
- [ ] No regressions (previous tests pass)
- [ ] Logging added for key operations
- [ ] Traceability matrix updated
- [ ] Changelog entry added
- [ ] Performance measured (if NFR applies)

**4. Commit iteration:**
```bash
git commit -m "feat(iter-{N}): Complete iteration {N}

Implemented: US-###, US-###, ...
Tests: @Iter-{N} passing
DoD: [x] Code [x] Tests [x] Logs [x] Docs

Satisfies: FR-###, NFR-###, UC-###"
```

---

## Definition of Done (Every Iteration)

- [ ] All US-### acceptance criteria satisfied
- [ ] BDD scenarios `@Iter-{N}` pass
- [ ] No regressions (previous iteration tests pass)
- [ ] Logging added (state transitions, errors, metrics)
- [ ] Traceability matrix updated (code modules added)
- [ ] Changelog entry added
- [ ] Performance metrics measured (if NFR applies: Iter 4, 5, 8)
- [ ] Error handling per FR-021 (no crashes)

**See:** `iteration-plan.md` for detailed DoD per iteration

---

## Key Milestones

**After Iteration 4:** E2E dictation works (hold → speak → paste)
**After Iteration 8:** v0.1 ready for release

---

## Finding Iteration Content

```bash
# Find user stories for iteration
Grep: "@Iter-3" in ../specification/user-stories-gherkin.md

# Check dependencies
Read: dependency-graph.yaml

# Load iteration overview
Read: iteration-plan.md
```

---

## Common Issues

**Ambiguous AC?** → Check referenced FR-###, then ADR-####, then ask user

**Missing detail?** → Check if intentionally flexible, document decision in commit

**Performance missed?** → Profile, optimize, document actual results in changelog
