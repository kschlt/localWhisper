# Claude Integration Guide

**Audience:** Claude Code AI agents
**Purpose:** Specific instructions for AI-driven development workflows
**Status:** Prescriptive guidance

---

## AI Agent Role & Constraints

You are acting as a **software engineer implementing a well-specified product**. Your job is to:

✅ **DO:**
- Read requirements and acceptance criteria carefully
- Implement exactly what is specified in the iteration
- Write tests that match BDD scenarios
- Update traceability when adding code
- Ask clarifying questions if acceptance criteria are ambiguous
- Log decisions that affect architecture (propose ADR if needed)

❌ **DO NOT:**
- Change requirement scope without explicit approval
- Skip iterations or reorder them
- Add features not in the current iteration
- Modify NFR targets (e.g., p95 latency) without discussion
- Assume implementation details not specified in contracts

---

## Efficient Context Loading

### Minimal Context Loading (Quick tasks)

For small, isolated tasks within a known iteration:
```
1. Read: iterations/iteration-{N}-*.md
2. Read: Referenced FR-### and UC-### from specification/
3. Read: Referenced ADR-#### from adr/
4. Implement
```

### Full Context Loading (New session start)

When starting fresh or switching iterations:
```
1. Read: meta/how-to-use-this-repo.md (orientation)
2. Read: overview/product-summary.md (product context)
3. Read: architecture/architecture-overview.md (system map)
4. Read: architecture/runtime-flows.md (end-to-end flow)
5. Read: iterations/iteration-{N}-*.md (current work)
6. Read: Referenced specifications and ADRs as needed
```

**Estimated tokens:** 15,000-25,000 for full context (fits comfortably in Claude's window)

### Progressive Context Loading

For exploration or debugging:
```
1. Start with: overview/glossary.md + specification/use-cases.md
2. Expand to: Specific FR-### or NFR-### files
3. Add: Relevant ADR-#### if design question arises
4. Check: testing/bdd-feature-seeds.md for test expectations
```

---

## Iteration Execution Protocol

### Step 1: Load Iteration Context
```bash
# Read the iteration file
Read: docs/iterations/iteration-{N}-*.md

# Check dependencies
Read: docs/iterations/dependency-graph.yaml
# Verify: all prerequisite iterations are complete

# Load referenced requirements
Grep: "FR-###|NFR-###|UC-###" in iteration file
Read: Each referenced requirement file
```

### Step 2: Understand Success Criteria
```bash
# Extract acceptance criteria from user stories
For each US-### in iteration:
  - Identify "AC:" or "Fit:" sections
  - Note any measurable targets (p95, file size, etc.)
  - Identify BDD scenario tags (@Iter-N, @UC-###, @FR-###)
```

### Step 3: Check Architecture Decisions
```bash
# Load relevant ADRs
Read: docs/adr/{referenced in iteration}.md

# Understand constraints
Note: Platform, interface contracts, data formats
```

### Step 4: Implement
```bash
# Create/modify code
# Follow .NET 8 + WPF conventions
# Respect interface contracts (CLI JSON, file paths, etc.)
# Add logging with context (state, paths, IDs where relevant)
```

### Step 5: Test
```bash
# Implement BDD scenarios
Read: docs/testing/bdd-feature-seeds.md
# Find scenarios tagged with @Iter-{N}
# Implement with SpecFlow or similar

# Run tests
# Verify all acceptance criteria pass
```

### Step 6: Verify Definition of Done
```bash
# Check DoD from iteration file
✓ Code implements all US-### acceptance criteria
✓ Tests pass (BDD scenarios + unit tests)
✓ Logging added for key state transitions
✓ Traceability matrix updated (if new code modules added)
✓ Changelog entry added
✓ Referenced specifications unchanged (or updated if needed)
✓ Metrics measured (if NFR-### applies)
```

### Step 7: Commit & Document
```bash
git add .
git commit -m "feat(iter-{N}): [US-###] Brief description

- Implements US-### with AC: ...
- Satisfies FR-###, NFR-###
- Tests: BDD scenario @Iter-{N} @UC-###
- See: docs/iterations/iteration-{N}-*.md

DoD: [x] Code [x] Tests [x] Logs [x] Docs"
```

---

## File Reading Strategies

### Strategy 1: Needle Search
When you know exactly what you need:
```bash
Grep: "FR-012" in docs/specification/functional-requirements.md
Grep: "ADR-0002" in docs/adr/0002-cli-subprocesses.md
```

### Strategy 2: Structured Navigation
When exploring a topic:
```bash
Read: docs/specification/use-cases.md  # Get UC-### list
Read: docs/specification/traceability-matrix.md  # Map UC → FR → US
Read: docs/iterations/iteration-plan.md  # See which iteration implements it
```

### Strategy 3: Dependency Following
When implementing a story:
```bash
Read: docs/iterations/iteration-03-stt-whisper.md  # Current work
# Story US-020 references FR-012 and ADR-0002
Read: docs/specification/functional-requirements.md (section FR-012)
Read: docs/adr/0002-cli-subprocesses.md
Read: docs/architecture/interface-contracts.md  # CLI JSON format
```

---

## Handling Ambiguity

### Scenario: Acceptance criteria unclear

```
1. Read the linked FR-### or NFR-### for more detail
2. Check if ADR-#### provides rationale
3. Look for "Fit:" criteria or measurable targets
4. If still unclear: ASK via AskUserQuestion tool
   - Quote the ambiguous text
   - Propose 2-3 interpretations
   - Ask for clarification
```

### Scenario: Missing implementation detail

```
1. Check if it's intentionally left as implementation choice
2. Review ADR-#### constraints (e.g., "must use CLI", "must be portable")
3. If critical and unspecified: propose an approach and note it in commit
4. If it affects user experience: ASK before implementing
```

### Scenario: Conflicting requirements

```
1. Check traceability matrix for priority
2. Review NFR-### to see if one overrides
3. Check ADR-#### for guidance on trade-offs
4. If unresolved: FLAG for human review (add TODO in docs)
```

---

## Traceability Updates

### When code is added:
```markdown
<!-- In docs/specification/traceability-matrix.md -->

| FR-010 | Hotkey registration | Implemented | `src/HotkeyManager.cs` | Iter-1 | US-001 |
```

### When a requirement changes:
```markdown
<!-- In docs/specification/functional-requirements.md -->

**FR-012 — STT with Whisper (CLI)**
~~Was: Whisper via FFI~~
**Updated 2025-09-18:** Now via CLI subprocess (see ADR-0002)
```

### When a new ADR is created:
```markdown
<!-- In docs/adr/0000-index.md -->

- **ADR-0006** (2025-09-20): Timeout handling strategy (Iter-3)
```

---

## Performance & Metrics

### NFR-001: Latency (p95 ≤ 2.5s)

**When to measure:**
- Iteration 4 (Clipboard + History integration)
- Iteration 8 (Stabilization)

**How to measure:**
```csharp
// Add telemetry in code
var sw = Stopwatch.StartNew();
// ... hotkey up to clipboard write ...
sw.Stop();
logger.LogInformation("E2E_Latency_Ms: {Ms}", sw.ElapsedMilliseconds);
```

**How to report:**
```markdown
<!-- In docs/changelog/v0.1-planned.md -->

**NFR-001 Measurement (Iter-4):**
- p50: 1.2s
- p95: 2.1s ✓
- p99: 3.4s (acceptable outlier)
```

### NFR-004: Flyout latency (≤ 0.5s)

**Measure:** Time from clipboard write to flyout visible
**Report:** Same pattern as above

---

## BDD Test Execution

### Locating tests:
```bash
Read: docs/testing/bdd-feature-seeds.md
# Find scenarios tagged @Iter-{N}
```

### Implementing tests:
```gherkin
@Iter-3 @UC-001 @FR-012
Scenario: STT produces JSON mapped to v1 contract
  Given eine 5s WAV Probe liegt vor
  When whisper-cli.exe ausgeführt wird
  Then existiert stt_result.json
  And der Adapter liefert ein Objekt mit "text","lang","duration_sec"
```

**Map to code:**
```csharp
// tests/Features/STT.feature (SpecFlow)
// tests/StepDefinitions/STTSteps.cs

[Given(@"eine 5s WAV Probe liegt vor")]
public void GivenWavProbe() { /* ... */ }

[When(@"whisper-cli.exe ausgeführt wird")]
public void WhenWhisperCalled() { /* ... */ }

[Then(@"existiert stt_result.json")]
public void ThenJsonExists() { /* ... */ }
```

---

## Common Patterns

### Pattern: Loading iteration context
```
Read: docs/iterations/iteration-{N}-*.md
Extract: US-### list
For each US-###:
  Extract: FR/NFR/UC references
  Read: docs/specification/{relevant}.md
  Read: docs/adr/{relevant}.md (if referenced)
```

### Pattern: Verifying a contract
```
Read: docs/architecture/interface-contracts.md
Find: CLI contract (e.g., Whisper JSON output)
Implement: Parser/validator in code
Test: With sample JSON from docs
```

### Pattern: Checking DoD
```
Read: docs/iterations/iteration-{N}-*.md (find "DoD:" section)
Checklist: Each item
Verify: Tests pass, logs present, docs updated
```

---

## Error Handling Guidance

### Source of truth:
- `specification/functional-requirements.md` → **FR-021** (Error dialogs)
- `architecture/risk-register.md` → Mitigation strategies

### Implementation pattern:
```csharp
try {
    // Risky operation
} catch (MicrophoneAccessException ex) {
    // FR-021: User-friendly message
    logger.LogError(ex, "Microphone access denied");
    ShowDialog("Mikrofon nicht verfügbar. Bitte überprüfen Sie...");
    // App stays stable (NFR-003)
}
```

---

## Questions to Ask Yourself

Before implementing:
- ✓ Have I read the iteration file completely?
- ✓ Do I understand all acceptance criteria?
- ✓ Have I checked the interface contracts?
- ✓ Do I know which ADRs constrain my design?
- ✓ Are there NFR targets I need to measure?

Before committing:
- ✓ Do all BDD scenarios pass?
- ✓ Have I added logging for key transitions?
- ✓ Is the traceability matrix updated?
- ✓ Does my commit message reference the correct IDs?
- ✓ Have I checked the DoD checklist?

---

## Anti-Patterns (Avoid These)

❌ **Implementing ahead:**
- Don't implement Iteration 5 before Iteration 4 is complete
- Don't add features from later iterations "while you're in there"

❌ **Ignoring contracts:**
- Don't change CLI JSON format without updating `interface-contracts.md`
- Don't modify data file structure without checking ADR-0003

❌ **Skipping tests:**
- Don't mark iteration complete without BDD scenarios passing
- Don't skip measurable NFR verification (p95, file size, etc.)

❌ **Weak traceability:**
- Don't commit without referencing US-### or FR-###
- Don't create new components without updating traceability matrix

---

## Summary Checklist for AI Agents

**Starting a session:**
- [ ] Read `meta/how-to-use-this-repo.md`
- [ ] Load iteration context (current iteration file)
- [ ] Check dependency graph
- [ ] Load referenced specs and ADRs

**During implementation:**
- [ ] Follow acceptance criteria exactly
- [ ] Respect interface contracts
- [ ] Add logging with context
- [ ] Write tests (BDD + unit)

**Before committing:**
- [ ] Verify DoD checklist
- [ ] Update traceability matrix
- [ ] Add changelog entry
- [ ] Reference IDs in commit message

**When uncertain:**
- [ ] Check ADRs for constraints
- [ ] Review traceability matrix
- [ ] Ask clarifying questions
- [ ] Propose approach and document reasoning

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial guidance)
