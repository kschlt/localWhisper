# How to Use This Repository

**Audience:** Future Claude Code sessions, AI agents
**Purpose:** Guide for navigating and utilizing this documentation system
**Status:** Living document

---

## Repository Purpose

This repository contains the **complete documentation foundation** for the **Dictate-to-Clipboard** Windows desktop application. All implementation work should be guided by these documents.

**What this repo contains:**
- Product specification with stable IDs (UC/FR/NFR)
- Solution architecture with ADRs (Architecture Decision Records)
- Iteration-by-iteration implementation plan
- BDD test seeds and traceability matrices
- Meta-guidance for AI-driven development

**What this repo does NOT contain (yet):**
- Source code (to be created during iterations)
- Compiled binaries
- Test execution results

---

## Document Organization

```
/docs/
  /meta/               ← You are here! Start here for orientation
  /overview/           ← Product summary, glossary
  /specification/      ← UC, FR, NFR, data structures, traceability
  /architecture/       ← System design, components, contracts, risks
  /adr/                ← Architecture Decision Records (numbered)
  /iterations/         ← Implementation plan (8 iterations)
  /testing/            ← BDD seeds, test strategy
  /changelog/          ← Version history and planning
```

---

## Reading Order for New Sessions

### 1. **Orientation** (5 minutes)
Start here:
- `meta/how-to-use-this-repo.md` (this file)
- `overview/product-summary.md`
- `overview/glossary.md`

### 2. **Requirements Understanding** (15 minutes)
Read in order:
- `specification/use-cases.md` — What the user needs
- `specification/functional-requirements.md` — What the system must do
- `specification/non-functional-requirements.md` — Quality attributes
- `specification/data-structures.md` — File formats, paths

### 3. **Solution Understanding** (15 minutes)
Read in order:
- `architecture/architecture-overview.md` — Component map
- `architecture/runtime-flows.md` — How things work end-to-end
- `architecture/interface-contracts.md` — CLI contracts (Whisper, LLM)
- `adr/0000-index.md` — Decision summary, then read relevant ADRs

### 4. **Implementation Planning** (10 minutes)
- `iterations/iteration-plan.md` — Full roadmap
- `iterations/iteration-{NN}-*.md` — Details for specific iteration
- `iterations/dependency-graph.yaml` — Story dependencies

### 5. **Test Strategy** (5 minutes)
- `testing/test-strategy.md`
- `testing/bdd-feature-seeds.md`

**Total orientation time: ~50 minutes**

---

## ID System & Traceability

All requirements and decisions use **stable IDs**:

| Prefix | Meaning | Example |
|--------|---------|---------|
| `UC-###` | Use Case | `UC-001` (Quick dictation) |
| `FR-###` | Functional Requirement | `FR-010` (Hotkey registration) |
| `NFR-###` | Non-Functional Requirement | `NFR-001` (Performance/latency) |
| `ADR-####` | Architecture Decision Record | `ADR-0001` (Platform choice) |
| `US-###` | User Story (implementation) | `US-001` (Hotkey toggles state) |

**Traceability chain:**
```
UC-001 (Use case)
  ├─ FR-010, FR-011, FR-012, FR-013, FR-014, FR-015 (Requirements)
  │   ├─ ADR-0001, ADR-0002 (Decisions)
  │   └─ US-001, US-010, US-020, US-030, US-031 (Stories)
  │       └─ @Iter-1, @Iter-2, @Iter-3, @Iter-4 (Implementation)
  └─ NFR-001, NFR-004 (Quality attributes)
```

See `specification/traceability-matrix.md` for full mapping.

---

## Working with Iterations

**Current state:** All documentation is complete; implementation starts at **Iteration 1**.

Each iteration follows this pattern:
1. **Read** the iteration file (e.g., `iterations/iteration-01-hotkey-skeleton.md`)
2. **Identify** the user stories (US-###) and their acceptance criteria
3. **Check** dependencies in `dependency-graph.yaml`
4. **Review** linked requirements (FR/NFR) and ADRs
5. **Implement** code to satisfy acceptance criteria
6. **Write** tests (BDD scenarios provided as seeds)
7. **Verify** Definition of Done checklist
8. **Update** changelog and traceability matrix

**Do NOT skip iterations.** They build on each other.

---

## Modifying Documentation

### When to update docs:

✅ **DO update** when:
- A requirement changes scope or acceptance criteria
- A new ADR is created (add next number in sequence)
- An iteration reveals missing details (add TODO or new story)
- Tests uncover ambiguities in contracts

❌ **DO NOT update** when:
- Implementation details change (code comments suffice)
- Refactoring doesn't affect external contracts
- Bug fixes don't change requirements

### How to update:

1. **Identify** which document(s) need changes
2. **Edit** with clear version tracking (date, reason)
3. **Update** traceability matrix if IDs change
4. **Commit** with message format: `docs: [ID] Description`

Example: `docs: [FR-012] Add timeout parameter to STT contract`

---

## Key Principles

1. **Traceability first:** Every implementation decision traces back to a requirement or ADR
2. **Stable IDs:** Once assigned, IDs should not change (deprecate and create new if needed)
3. **AI-friendly structure:** Short sections, scannable lists, clear hierarchy
4. **Definition of Done:** Each iteration has explicit DoD checklist
5. **Vertical slices:** Each iteration delivers end-to-end value

---

## Quick Reference Links

- **Glossary:** `overview/glossary.md`
- **All ADRs:** `adr/0000-index.md`
- **Full iteration plan:** `iterations/iteration-plan.md`
- **Traceability:** `specification/traceability-matrix.md`
- **Test strategy:** `testing/test-strategy.md`

---

## Questions?

If this documentation is unclear or incomplete:
1. Check `meta/claude-integration-guide.md` for AI-specific workflows
2. Check `meta/iteration-execution-guide.md` for step-by-step process
3. Add a TODO in the relevant document
4. Flag for human review if architectural clarity is needed

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial structure)
