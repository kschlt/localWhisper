# Dictate-to-Clipboard

**Status:** Documentation Complete, Implementation Pending
**Version:** v0.1 (Planned)
**Platform:** Windows Desktop (Portable)

---

## Quick Summary

A portable Windows desktop app for fast offline speech-to-text dictation:

**Hold hotkey → speak → release → transcript in clipboard**

- ✅ Offline & private (local Whisper STT)
- ✅ Portable (no admin rights, single EXE)
- ✅ Searchable history (auto-saved Markdown files)
- ✅ Zero friction (no UI switching)

---

## Current State

This repository contains **complete documentation** for the Dictate-to-Clipboard project:

**✅ Documentation Complete:**
- Product specification (use cases, requirements, data structures)
- Solution architecture (components, flows, ADRs)
- Implementation plan (8 iterations with user stories)
- Test strategy (BDD seeds, acceptance criteria)
- AI guidance (meta-docs for Claude Code sessions)

**⏳ Implementation:** Ready to begin (Iteration 1)

---

## Documentation Structure

```
/docs/
  /meta/               ← Start here! AI agent guidance
    how-to-use-this-repo.md
    claude-integration-guide.md
    iteration-execution-guide.md
    traceability-index.md

  /overview/           ← Product summary & glossary
    product-summary.md
    glossary.md

  /specification/      ← Requirements (UC, FR, NFR)
    use-cases.md
    functional-requirements.md
    non-functional-requirements.md
    data-structures.md
    traceability-matrix.md

  /architecture/       ← System design & ADRs
    architecture-overview.md
    interface-contracts.md
    runtime-flows.md
    risk-register.md

  /adr/                ← Architecture decisions
    0000-index.md
    0001-platform-dotnet-wpf.md
    0002-cli-subprocesses.md
    0003-storage-layout.md
    0004-autostart-removed.md
    0005-custom-flyout.md

  /iterations/         ← Implementation roadmap
    iteration-plan.md
    dependency-graph.yaml

  /testing/            ← Test strategy & BDD seeds
    test-strategy.md
    bdd-feature-seeds.md

  /changelog/          ← Version history
    v0.1-planned.md
```

---

## Getting Started (For AI Agents)

**If you are a Claude Code session:**

1. **Read:** `docs/meta/how-to-use-this-repo.md` (5 min orientation)
2. **Load context:** See `docs/meta/claude-integration-guide.md`
3. **Start implementing:** Begin with `docs/iterations/iteration-plan.md`

**Key principle:** All requirements are specified. Your job is to implement exactly what is documented, starting from Iteration 1.

---

## Implementation Roadmap

| Iteration | Focus | Effort | Status |
|-----------|-------|--------|--------|
| 1 | Hotkey & App Skeleton | 4-6h | Planned |
| 2 | Audio Recording | 4-6h | Planned |
| 3 | STT with Whisper | 6-8h | Planned |
| 4 | Clipboard + History + Flyout | 6-10h | Planned |
| 5 | First-Run Wizard + Repair | 8-12h | Planned |
| 6 | Settings UI | 4-6h | Planned |
| 7 | Optional Post-Processing | 4-6h | Planned |
| 8 | Stabilization + Reset | 6-10h | Planned |

**Total:** ~40-60 hours

**See:** `docs/iterations/iteration-plan.md` for detailed roadmap.

---

## Key Design Decisions (ADRs)

- **ADR-0001:** Platform = .NET 8 + WPF (portable, fast development)
- **ADR-0002:** STT/LLM via CLI subprocesses (robust, debuggable)
- **ADR-0003:** Single data root folder (easy backup/migration)
- **ADR-0005:** Custom flyout notification (reliable, fast)

**See:** `docs/adr/0000-index.md` for all decisions.

---

## Requirements Coverage

- **Use Cases:** 4 (UC-001 through UC-004)
- **Functional Requirements:** 14 (FR-010 through FR-024)
- **Non-Functional Requirements:** 6 (NFR-001 through NFR-006)
- **User Stories:** ~30 across 8 iterations
- **BDD Scenarios:** Full coverage in `docs/testing/bdd-feature-seeds.md`

**All requirements are traceable** to iterations and tests.

---

## Performance Targets

| Metric | Target | Verification |
|--------|--------|--------------|
| E2E Latency (hotkey → clipboard) | p95 ≤ 2.5s | Iteration 4, 8 |
| Flyout Display | ≤ 0.5s | Iteration 4 |
| Wizard Completion | < 2 min | Iteration 5 |
| Memory Footprint (idle) | < 150 MB | Iteration 8 |
| Crashes in Error Matrix | 0 | Iteration 8 |

---

## Technology Stack

- **Platform:** .NET 8 (C#)
- **UI:** WPF (Windows Presentation Foundation)
- **Audio:** WASAPI (via NAudio or P/Invoke)
- **STT:** Whisper (CLI subprocess)
- **Config:** TOML (via Tomlyn)
- **Logging:** Serilog or NLog
- **Testing:** xUnit, SpecFlow (BDD)

---

## Repository Guidelines

### For Developers

**Branching:**
- Implementation work happens on `claude/restructure-docs-architecture-01WktUKRshLh5fjn8noP9SQ2`
- Tag iterations: `iter-1-complete`, `iter-2-complete`, etc.

**Commits:**
- Reference IDs: `feat(iter-1): [US-001] Hotkey toggles state`
- Link to docs: `See: docs/iterations/iteration-01-hotkey-skeleton.md`

**Definition of Done:**
- See iteration files for detailed DoD checklists
- All tests pass, docs updated, traceability maintained

### For AI Agents (Claude Code)

**Start here:** `docs/meta/claude-integration-guide.md`

**Key files to read before implementing:**
1. `docs/overview/product-summary.md` (product context)
2. `docs/architecture/architecture-overview.md` (system design)
3. `docs/iterations/iteration-plan.md` (roadmap)
4. `docs/iterations/iteration-01-hotkey-skeleton.md` (first iteration)

**Workflow:**
1. Load iteration context
2. Read referenced FR/NFR/UC docs
3. Implement user stories
4. Write tests (BDD + unit)
5. Update traceability matrix
6. Commit with proper references

---

## Known Limitations (v0.1)

**Out of scope for v0.1:**
- ❌ Autostart (deferred; manual workaround available)
- ❌ Code signing (SmartScreen warning expected)
- ❌ Auto-update mechanism
- ❌ GPU acceleration
- ❌ Built-in history search UI
- ❌ "Insert at cursor" functionality

**Future versions:** See `docs/changelog/v0.1-planned.md` for roadmap.

---

## Contributing

This is currently a solo developer project with AI assistance (Claude Code).

**For humans:** Contact project owner before contributing.

**For AI agents:** Follow the guidelines in `docs/meta/claude-integration-guide.md`.

---

## License

[TBD - To be determined by project owner]

---

## Support & Feedback

- **Issues:** Report at [repository issue tracker]
- **Questions:** See `docs/meta/how-to-use-this-repo.md` for troubleshooting

---

## Quick Links

**Documentation:**
- [Product Summary](docs/overview/product-summary.md)
- [Use Cases](docs/specification/use-cases.md)
- [Requirements](docs/specification/functional-requirements.md)
- [Architecture Overview](docs/architecture/architecture-overview.md)
- [ADR Index](docs/adr/0000-index.md)
- [Iteration Plan](docs/iterations/iteration-plan.md)
- [Test Strategy](docs/testing/test-strategy.md)

**For AI Agents:**
- [How to Use This Repo](docs/meta/how-to-use-this-repo.md)
- [Claude Integration Guide](docs/meta/claude-integration-guide.md)
- [Iteration Execution Guide](docs/meta/iteration-execution-guide.md)

---

**Last Updated:** 2025-09-17
**Documentation Version:** v0.1 (Complete)
**Implementation Status:** Ready to begin
