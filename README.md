# LocalWhisper

[![.NET Build and Test](https://github.com/kschlt/localWhisper/actions/workflows/dotnet-build-test.yml/badge.svg)](https://github.com/kschlt/localWhisper/actions/workflows/dotnet-build-test.yml)

**Hold a hotkey, speak, release: the transcript is in your clipboard.**
A small, portable Windows tray app for offline dictation, built on
[whisper.cpp](https://github.com/ggerganov/whisper.cpp).

> **Status: working prototype, not actively maintained.** It runs end-to-end but has
> rough edges, and it is published as-is. Details, known issues and roadmap:
> **[docs/status.md](docs/status.md)**.

## What it does

- **One hotkey:** press and hold (default `Ctrl+Shift+A`), talk, release. A small flyout
  confirms the result and the text is already in your clipboard.
- **Offline and private:** speech recognition runs locally via `whisper-cli.exe`.
  No cloud, no account, no telemetry.
- **Portable:** a single self-contained `LocalWhisper.exe`, no installer, no admin rights.
  All data lives in one folder (`%LOCALAPPDATA%\LocalWhisper` by default).
- **History:** every dictation is saved as a timestamped Markdown file.
- **Optional clean-up:** punctuation and formatting via a local LLM
  (`llama-cli.exe` from llama.cpp). Off by default.

The UI is in **German**. Code and documentation are in English.

## Getting started

You need three things on a Windows 10/11 x64 machine:

1. **LocalWhisper.exe**: the latest pre-release from
   [Releases](https://github.com/kschlt/localWhisper/releases), or build it yourself
   (below). Windows SmartScreen will warn because the binary is not code-signed.
2. **whisper-cli.exe** and its DLLs from a
   [whisper.cpp release](https://github.com/ggerganov/whisper.cpp/releases).
3. **A Whisper model**, for example `ggml-small.bin` from
   [Hugging Face](https://huggingface.co/ggerganov/whisper.cpp/tree/main).

Then follow the **[Manual Setup Guide](docs/MANUAL_SETUP_GUIDE.md)**. It covers folder
layout, the first-run wizard, the one `config.toml` edit that is still needed, and
troubleshooting.

## Building from source

Requires the .NET 8 SDK on Windows (WPF does not build on Linux or macOS).

```powershell
git clone https://github.com/kschlt/localWhisper.git
cd localWhisper
dotnet build LocalWhisper.sln -c Release
dotnet test  LocalWhisper.sln -c Release --filter "Category!=WpfIntegration"
dotnet publish src/LocalWhisper/LocalWhisper.csproj -c Release -r win-x64 --self-contained -o publish
```

`publish\LocalWhisper.exe` is the portable binary. CI runs the same steps on every push
and attaches the EXE as a workflow artifact.

## Documentation

- [Project status, known issues, roadmap](docs/status.md)
- [Manual setup guide](docs/MANUAL_SETUP_GUIDE.md)
- [Product summary](docs/overview/product-summary.md) and [use cases](docs/specification/use-cases.md)
- [Architecture overview](docs/architecture/architecture-overview.md), [ADRs](docs/adr/0000-index.md),
  [CLI contracts](docs/architecture/interface-contracts.md)
- [Changelog](docs/changelog/v0.1-planned.md)

Tech stack: .NET 8, WPF, NAudio (WASAPI), H.NotifyIcon, Tomlyn, Serilog, xUnit.
STT and LLM run as CLI subprocesses, not in-process bindings ([ADR-0002](docs/adr/0002-cli-subprocesses.md)).

## License

Not yet decided. Until a `LICENSE` file is added, all rights are reserved by the author;
you may read and build the code, but ask before redistributing.
