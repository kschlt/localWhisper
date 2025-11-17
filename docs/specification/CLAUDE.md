# Requirements Specification

All requirements with stable IDs and user stories.

---

## Files

- `use-cases.md` - UC-001 to UC-004 (what users need)
- `functional-requirements.md` - FR-010 to FR-024 (what system does)
- `non-functional-requirements.md` - NFR-001 to NFR-006 (quality attributes)
- `user-stories-gherkin.md` - All user stories with BDD scenarios (tagged by @Iter-{N})
- `data-structures.md` - Config format, history structure, file paths
- `traceability-matrix.md` - UC → FR → US → Code mapping

---

## Traceability Chain

```
UC-001 (Quick dictation)
  ├─ FR-010 (Hotkey) → US-001 (Hotkey toggles state) → HotkeyManager.cs
  ├─ FR-011 (Audio) → US-010 (Record on hold) → AudioRecorder.cs
  ├─ FR-012 (STT) → US-020 (Invoke Whisper) → WhisperCLIAdapter.cs
  └─ FR-013 (Clipboard) → US-030 (Write clipboard) → ClipboardService.cs
```

**Always update `traceability-matrix.md` when adding code.**

---

## When Implementing

**Before coding:**
1. Read user story in `user-stories-gherkin.md` (find @Iter-{N} section)
2. Extract acceptance criteria (Given/When/Then)
3. Load referenced FR-### from `functional-requirements.md`
4. Check NFR-### targets if measurable (e.g., NFR-001: p95 ≤ 2.5s)

**While coding:**
- Satisfy each acceptance criterion
- Add structured logging for state transitions
- Handle errors per FR-021 (user-friendly dialogs, no crashes)

**After coding:**
- Update `traceability-matrix.md` (add code module)
- Reference US-###, FR-### in commit message

---

## Key Requirements

**Performance (NFR-001):**
- E2E latency p95 ≤ 2.5s (measure in Iteration 4, 8)

**Error Handling (FR-021):**
- All errors caught and logged
- User-friendly dialogs (no stack traces)
- App stays stable (no crashes)

**Logging (FR-023):**
- Structured format (key-value pairs)
- Log state transitions, errors, performance metrics

---

## Finding Requirements

```bash
# Find specific requirement
Grep: "FR-012" in functional-requirements.md

# Find which iteration implements it
Read: traceability-matrix.md

# Find BDD scenarios
Grep: "@FR-012" in user-stories-gherkin.md
```

---

## When to Update

**Update docs if:**
- Requirement scope changes
- Tests reveal ambiguities
- New edge cases discovered

**Don't update for:**
- Code refactoring (no external behavior change)
- Implementation details (use code comments)
