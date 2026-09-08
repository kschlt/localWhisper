# LocalWhisper

Portable Windows tray app for offline hold-to-talk dictation. .NET 8 + WPF, whisper.cpp
via CLI subprocess. Spec-driven: every feature traces UC → FR → US → code → test.

Project state and known gaps: `docs/status.md`. Resuming after a pause: `docs/meta/handover.md`.
Do not put status information in this file; it belongs in those two.

## Build and test

```powershell
dotnet build LocalWhisper.sln -c Release
dotnet test  LocalWhisper.sln -c Release --filter "Category!=WpfIntegration"
dotnet publish src/LocalWhisper/LocalWhisper.csproj -c Release -r win-x64 --self-contained -o publish
```

- WPF only builds on Windows. In a Linux session (Claude Code on the web) you cannot
  compile or run tests; CI on `windows-latest` is the verification. Keep changes small
  enough that a green CI run is meaningful, and re-read your diff before pushing.
- Tests that open real WPF windows carry `[Trait("Category", "WpfIntegration")]` and are
  excluded on CI. Do not change that filter; do not add new window-level tests.
- Releases: push a `v*` tag, `.github/workflows/release.yml` builds and publishes the EXE.
  Only the owner tags.

## Where things are

| Need | File |
|---|---|
| Solution layout | `docs/architecture/project-structure.md` |
| Requirements (UC/FR/NFR) | `docs/specification/` |
| Gherkin scenarios per iteration | `docs/specification/user-stories-gherkin.md` (`@Iter-N` tags) |
| Architecture, ADRs | `docs/architecture/architecture-overview.md`, `docs/adr/0000-index.md` |
| CLI contracts (whisper-cli, llama-cli) | `docs/architecture/interface-contracts.md` |
| Roadmap and per-iteration DoD | `docs/iterations/` |
| Traceability | `docs/specification/traceability-matrix.md` |
| Placeholders (PH-###) | `docs/meta/placeholders-tracker.md` |
| Detailed agent workflow, context loading | `docs/meta/claude-integration-guide.md` |
| UI colours and icons | `docs/ui/` |

## Rules

**Test-first.** Write or update tests from the Gherkin scenario before touching
implementation. Review your own tests as a second person (spec match, edge cases,
not asserting wrong behaviour), then implement, then review the implementation the same
way. Do not stop to ask for that review; do both roles yourself.

**Stay in scope.** Implement what the referenced US/FR says, nothing more. Changing a
requirement means updating the spec file in the same change. Changing a CLI invocation or
JSON shape means updating `docs/architecture/interface-contracts.md` in the same change.

**Constraints from ADRs.** .NET 8 + WPF only (ADR-0001). STT and LLM as CLI subprocesses,
never FFI (ADR-0002). One data root folder (ADR-0003). Custom flyout, not Windows toast
(ADR-0005).

**Logging.** Structured logging via `AppLogger` for state transitions, errors and timings.
No debug-only timers or step-by-step logging left in committed code.

**Language.** UI strings in German, code and docs in English.

## Commits and traceability

- Conventional prefix plus IDs: `feat(iter-6): [US-050] Settings window`, `fix(stt): ... See: US-020`.
- When adding behaviour: update `docs/specification/traceability-matrix.md` and add a
  line to `docs/changelog/v0.1-planned.md`.
- When resolving a placeholder: mark it in `docs/meta/placeholders-tracker.md` and remove
  the `TODO(PH-###)` comment.

## When unclear

Ambiguous acceptance criterion: read the referenced FR, then the ADR, then ask.
Conflict between docs: the traceability matrix decides; flag it if still unresolved.
