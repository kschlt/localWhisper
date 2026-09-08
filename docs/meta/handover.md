# Handover Notes

**Written:** 2026-09-08
**Purpose:** Everything a future session (human or agent) needs to pick this project up
again, plus a ready-to-paste prompt.

---

## Where the project stands

- Iterations 1-7 are implemented. Iteration 8 (stabilisation, reset flow, NFR-001 latency
  verification, release) was never started.
- The last real-world testing happened on 2025-12-04/05 on a Windows machine. The fixes
  from that session (hold-to-talk key-up detection, correct `whisper-cli` arguments and
  JSON parsing, tray icon visibility, wizard fixes) were merged into `main` in September
  2026 together with matching test updates.
- Since then the owner has no Windows machine. Nothing after 2025-12-05 has been run on
  Windows; it has only been compiled and unit-tested on the GitHub `windows-latest` runner.
- CI (`.github/workflows/dotnet-build-test.yml`) builds, runs the non-WPF tests and
  uploads a portable `LocalWhisper.exe` artifact on every push.
- Pushing a tag `v*` runs `.github/workflows/release.yml`, which builds the EXE, zips it
  with the setup guide and creates a GitHub (pre-)release.

## Known gaps, in priority order

1. The wizard does not ask for the `whisper-cli.exe` path; users must edit `config.toml`
   (see `docs/MANUAL_SETUP_GUIDE.md`, step 8). Adding a wizard step or a settings field
   for `paths.whisper_cli_path` is the single biggest usability win.
2. `ErrorDialog`'s "Settings" button is still the PH-001 placeholder (shows a message box).
3. ~70 WPF window tests (`Category=WpfIntegration`) are excluded on CI; they need an
   interactive desktop. `tests/LocalWhisper.Tests/README.md` explains the split.
4. Iteration 8 as specified: reset function, error matrix, p95 latency ≤ 2.5 s
   (`docs/specification/non-functional-requirements.md`, NFR-001).
5. No license file. No code signing (SmartScreen warning).

## What can be done without a Windows machine

- Anything in `docs/`.
- Code changes verified by CI only (compile + non-WPF tests). Keep them small.
- Cutting a pre-release: tag `main` and let the release workflow build it.

## What needs a Windows machine

- Any claim that "it works": run `publish/LocalWhisper.exe`, go through
  `docs/MANUAL_SETUP_GUIDE.md`, dictate a sentence, check clipboard, history file, flyout.
- Running the `WpfIntegration` tests: `dotnet test LocalWhisper.sln` without the filter.
- Measuring NFR-001.

## Prompt for the next agent session

Paste this into a fresh Claude Code session on this repository:

```
Read CLAUDE.md, README.md and docs/meta/handover.md first.

Context: LocalWhisper is a finished-enough prototype that is not actively developed.
The owner currently has no Windows machine, so anything you change can only be verified
by the GitHub Actions CI (windows-latest: build + non-WPF unit tests + portable publish).

Task: <fill in, e.g. "add a wizard step for the whisper-cli path (known gap 1)">.

Rules:
- Test-first, as described in CLAUDE.md. Keep the change small enough that CI is
  meaningful verification.
- Do not touch the WpfIntegration test filter.
- Update docs/changelog/v0.1-planned.md and the traceability matrix if you add behaviour.
- Push to a branch, make sure CI is green, then open a PR against main. Do not tag a
  release; the owner does that.
```
