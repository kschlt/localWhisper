# Architecture Decision Records (Index)

**Purpose:** Index of all architecture and design decisions for the project
**Format:** Each ADR is numbered sequentially and captures a significant decision
**Status:** Living document

---

## What is an ADR?

An Architecture Decision Record (ADR) captures an important architectural decision made along with its context and consequences. ADRs help future developers understand **why** decisions were made, not just **what** was decided.

**When to create an ADR:**
- Platform or technology choices (e.g., .NET vs. Rust)
- Architectural patterns (e.g., CLI subprocess vs. FFI)
- Significant trade-offs (e.g., portability vs. performance)
- Data format or storage decisions
- Integration approaches with external systems

**When NOT to create an ADR:**
- Minor implementation details (use code comments)
- Temporary workarounds (use TODO comments)
- Bug fixes (use commit messages)

---

## ADR Lifecycle

**States:**
- **Proposed:** Decision is under consideration
- **Accepted:** Decision is approved and should be followed
- **Deprecated:** Decision is no longer recommended but existing code may still use it
- **Superseded:** Decision is replaced by a newer ADR (reference the superseding ADR)

---

## Current ADRs

| Number | Title | Status | Date | Affected Requirements |
|--------|-------|--------|------|----------------------|
| [ADR-0001](0001-platform-dotnet-wpf.md) | Platform: .NET 8 + WPF | Accepted | 2025-09-17 | FR-010, FR-015, FR-016..020, FR-021, FR-023; NFR-001, NFR-002, NFR-004, NFR-006 |
| [ADR-0002](0002-cli-subprocesses.md) | STT/LLM Integration via CLI Subprocesses | Accepted | 2025-09-17 | FR-012, FR-022; NFR-001, NFR-003, NFR-006 |
| [ADR-0003](0003-storage-layout.md) | Storage Layout & Data Root | Accepted | 2025-09-17 | FR-014, FR-017, FR-019, FR-024; NFR-002, NFR-003, NFR-006 |
| [~~ADR-0004~~](0004-autostart-removed.md) | ~~Autostart via Shortcut~~ | **Removed** | 2025-09-17 | ~~FR-018~~ (out of scope for v0.1) |
| [ADR-0005](0005-custom-flyout.md) | Custom Flyout Notification | Accepted | 2025-09-17 | FR-015; NFR-004 |

---

## Creating a New ADR

**Process:**
1. Create new file: `adr/####-title-in-kebab-case.md` (next sequential number)
2. Use template below
3. Update this index with new entry
4. Reference ADR in related requirements and code

**Template:**

```markdown
# ADR-####: Title of Decision

**Status:** Proposed | Accepted | Deprecated | Superseded
**Date:** YYYY-MM-DD
**Affected Requirements:** (List FR/NFR IDs)

---

## Context

What is the issue or problem we are trying to solve?
What are the constraints or forcing functions?

## Options Considered

### Option A: [Name]
- Description
- Pros
- Cons

### Option B: [Name]
- Description
- Pros
- Cons

### Option C: [Name]
- Description
- Pros
- Cons

## Decision

We choose **Option X** because [rationale].

## Consequences

**Positive:**
- Benefit 1
- Benefit 2

**Negative:**
- Trade-off 1
- Trade-off 2

**Mitigations:**
- How we address negative consequences

## Related

- ADR-#### (related decision)
- FR-### (affected requirement)
- NFR-### (affected quality attribute)
```

---

## Deprecated / Superseded ADRs

| Number | Title | Status | Reason | Superseded By |
|--------|-------|--------|--------|---------------|
| ADR-0004 | Autostart via Shortcut | Removed | Out of scope for v0.1 | N/A (deferred to future version) |

---

## Decision Themes

**Platform & Technology:**
- ADR-0001: .NET 8 + WPF

**Integration & Boundaries:**
- ADR-0002: CLI Subprocesses for STT/LLM

**Data & Storage:**
- ADR-0003: Storage Layout & Data Root

**User Experience:**
- ADR-0005: Custom Flyout Notification

---

## Related Documents

- **Architecture Overview:** `architecture/architecture-overview.md`
- **Requirements:** `specification/functional-requirements.md`, `specification/non-functional-requirements.md`
- **Traceability Matrix:** `specification/traceability-matrix.md`

---

**Last updated:** 2025-09-17
**Next ADR:** ADR-0006 (TBD as needed during implementation)
