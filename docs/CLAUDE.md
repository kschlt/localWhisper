# Documentation Directory

Complete documentation foundation for Dictate-to-Clipboard.

---

## Structure

```
/docs/
  /meta/          ← AI guidance (start here for new sessions)
  /overview/      ← Product summary, glossary
  /specification/ ← Requirements (UC/FR/NFR), user stories
  /architecture/  ← System design, ADRs, contracts
  /iterations/    ← 8-iteration roadmap
  /testing/       ← BDD scenarios, test strategy
  /changelog/     ← Version history
```

---

## Reading Order (New Sessions)

**Quick start (~15 min):**
1. `meta/claude-integration-guide.md` - AI workflow
2. `specification/user-stories-gherkin.md` - Find @Iter-{N} section
3. `iterations/iteration-plan.md` - Roadmap overview

**Full context (~50 min):**
1. `meta/how-to-use-this-repo.md` - Orientation
2. `overview/product-summary.md` - Product context
3. `specification/functional-requirements.md` - FR-010 to FR-024
4. `architecture/architecture-overview.md` - System design
5. `adr/0000-index.md` - All architecture decisions
6. `iterations/iteration-plan.md` - Implementation roadmap

---

## Common Tasks

**Find a requirement:**
```bash
Grep: "FR-012" in specification/functional-requirements.md
```

**Load iteration:**
```bash
Read: specification/user-stories-gherkin.md  # @Iter-3 section
Read: iterations/iteration-plan.md          # Dependencies
```

**Check architecture:**
```bash
Read: architecture/architecture-overview.md
Read: adr/{number}-{topic}.md
```

**Find tests:**
```bash
Grep: "@Iter-3" in specification/user-stories-gherkin.md
Read: testing/test-strategy.md
```

---

## Principles

- **Stable IDs:** UC/FR/NFR/ADR/US don't change once assigned
- **Traceability:** UC → FR → US → Code (maintain in `specification/traceability-matrix.md`)
- **Sequential:** Implement iterations 1 → 8 in order
