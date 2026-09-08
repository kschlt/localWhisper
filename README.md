# LocalWhisper

[![.NET Build and Test](https://github.com/kschlt/localWhisper/actions/workflows/dotnet-build-test.yml/badge.svg)](https://github.com/kschlt/localWhisper/actions/workflows/dotnet-build-test.yml)

**Hold a hotkey, speak, release: the transcript is in your clipboard.**
A small, portable Windows tray app for offline dictation, built on
[whisper.cpp](https://github.com/ggerganov/whisper.cpp).

> **Project status: working prototype, not maintained.**
> The app runs end-to-end (hotkey, recording, transcription, clipboard, history, flyout,
> first-run wizard, settings, optional LLM post-processing). It was built and manually
> tested in November/December 2025 and has not been developed since, because the author
> no longer has a Windows machine. It is published as-is; see [Status](#status) for what
> works, what is rough, and what was never done. Forks and PRs are welcome, but do not
> expect fast responses.

---

## What it does

- **Zero-friction dictation:** press and hold the hotkey (default `Ctrl+Shift+A`), talk,
  release. A short flyout confirms the result, and the text is already in your clipboard.
- **Offline and private:** speech recognition runs locally via `whisper-cli.exe`.
  No cloud, no account, no telemetry.
- **Portable:** a single self-contained `LocalWhisper.exe`, no installer, no admin rights.
  All data lives in one folder you choose (`%LOCALAPPDATA%\LocalWhisper` by default).
- **History:** every dictation is saved as a timestamped Markdown file.
- **Optional post-processing:** clean up punctuation and formatting with a local LLM
  (`llama-cli.exe` from llama.cpp). Off by default.

The UI language is **German**. The code and documentation are in English.

## Who it is for

People who dictate short texts many times a day (chat replies, notes, ticket comments)
and want that to be one hotkey away, without sending audio anywhere.
Realistically: it is also a portfolio piece showing a spec-driven, test-first way of
building a small desktop app with an AI coding agent. See [How it was built](#how-it-was-built).

## Getting it running

There is no installer. You need three things on a Windows 10/11 x64 machine:

1. **LocalWhisper.exe**: download the latest pre-release from the
   [Releases](https://github.com/kschlt/localWhisper/releases) page, or build it yourself
   (see below) if none is listed. Windows SmartScreen will warn because the binary is
   not code-signed.
2. **whisper-cli.exe** and its DLLs from a
   [whisper.cpp release](https://github.com/ggerganov/whisper.cpp/releases).
3. **A Whisper model** (`ggml-small.bin` is a good start) from
   [Hugging Face](https://huggingface.co/ggerganov/whisper.cpp/tree/main).

Then follow the step-by-step **[Manual Setup Guide](docs/MANUAL_SETUP_GUIDE.md)**.
Note: after the first-run wizard you currently still have to enter the path to
`whisper-cli.exe` in `config.toml` by hand. The guide shows exactly where.

### Building from source

```powershell
git clone https://github.com/kschlt/localWhisper.git
cd localWhisper
dotnet build LocalWhisper.sln -c Release
dotnet test  LocalWhisper.sln -c Release --filter "Category!=WpfIntegration"
dotnet publish src/LocalWhisper/LocalWhisper.csproj -c Release -r win-x64 --self-contained -o publish
```

Requires the .NET 8 SDK on Windows (WPF does not build on Linux/macOS).
`publish/LocalWhisper.exe` is the portable binary. CI runs the same commands on every push
and attaches the EXE as a workflow artifact.

## Status

The project was planned as 8 iterations. Iterations 1 to 7 are implemented; iteration 8
(stabilisation, reset function, performance verification) was never started.

| Area | State |
|---|---|
| Global hotkey, hold-to-talk with key-up detection | Works |
| WASAPI recording to 16 kHz WAV | Works |
| Transcription via `whisper-cli` (JSON output) | Works, corrected against real whisper.cpp output in Dec 2025 |
| Clipboard, Markdown history, flyout notification | Works |
| First-run wizard (data folder, model, hotkey) | Works, but does not ask for the whisper-cli path (manual `config.toml` edit) |
| Model download and SHA-1 verification | Implemented, lightly tested |
| Settings window | Works; the "Settings" button inside error dialogs is still a placeholder |
| LLM post-processing via `llama-cli` | Implemented; needs the CUDA or CPU build of llama.cpp and its DLLs |
| Reset / repair flow, p95 latency target of 2.5 s | Not verified (iteration 8) |
| Automated tests | ~230 xUnit tests; ~70 WPF window tests are excluded on CI because they need an interactive desktop |
| Code signing, auto-update, autostart, GPU acceleration for Whisper | Not planned for v0.1 |

Known rough edges are listed in [docs/changelog/v0.1-planned.md](docs/changelog/v0.1-planned.md).

## How it was built

This repository is an experiment in **specification-first development with an AI coding
agent** (Claude Code). The workflow was: write use cases, functional and non-functional
requirements with IDs, architecture decision records and Gherkin scenarios first, then
implement iteration by iteration with tests written before code. Almost all commits were
authored by the agent in web sessions and reviewed by the owner; the branch names still
carry the session IDs.

If you want to read the specification rather than the code:

- [Product summary](docs/overview/product-summary.md)
- [Use cases](docs/specification/use-cases.md), [functional requirements](docs/specification/functional-requirements.md), [NFRs](docs/specification/non-functional-requirements.md)
- [Architecture overview](docs/architecture/architecture-overview.md) and [ADR index](docs/adr/0000-index.md)
- [Interface contracts](docs/architecture/interface-contracts.md) (whisper-cli / llama-cli invocation and JSON)
- [Iteration plan](docs/iterations/iteration-plan.md) and [Gherkin user stories](docs/specification/user-stories-gherkin.md)
- [Test strategy](docs/testing/test-strategy.md) and [test README](tests/LocalWhisper.Tests/README.md)
- [Agent guidance](docs/meta/claude-integration-guide.md) and [handover notes](docs/meta/handover.md) for picking the project up again

## Tech stack

.NET 8, WPF, [NAudio](https://github.com/naudio/NAudio) (WASAPI), [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) (tray),
[Tomlyn](https://github.com/xoofx/Tomlyn) (config), [Serilog](https://serilog.net/) (logging),
xUnit + FluentAssertions + Moq (tests). STT and LLM run as CLI subprocesses
([ADR-0002](docs/adr/0002-cli-subprocesses.md)), not as in-process bindings.

## License

Not yet decided. Until a `LICENSE` file is added, all rights are reserved by the author;
you may read and build the code, but ask before redistributing.
