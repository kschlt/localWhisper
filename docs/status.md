# Project Status

**Last updated:** 2026-09-08
**One line:** Working prototype. Runs end-to-end on Windows, rough in places, not actively developed.

This is the single place for "what works, what does not". The README only links here,
so update this file (not the README) when something changes.

---

## Timeline

| When | What |
|---|---|
| Sep 2025 | Specification written (use cases, requirements, ADRs, iteration plan) |
| Nov 2025 | Iterations 1-7 implemented |
| Dec 2025 | First manual end-to-end test on Windows; fixes for hotkey, whisper-cli format, tray icon |
| Sep 2026 | Those fixes merged, tests aligned, CI builds the portable EXE, repo tidied |
| Since | Paused. The author has no Windows machine, so nothing newer has been run on Windows. |

The original plan had 8 iterations. Iteration 8 (stabilisation, reset flow, performance
verification) was never started. See [iteration plan](iterations/iteration-plan.md) for
the roadmap as designed and [changelog](changelog/v0.1-planned.md) for what each
iteration delivered.

## Feature state

| Feature | State | Notes |
|---|---|---|
| Global hotkey, hold-to-talk | Works | Key-up detected via low-level keyboard hook |
| Recording (WASAPI, 16 kHz WAV) | Works | |
| Transcription via `whisper-cli` | Works | Corrected to real whisper.cpp JSON in Dec 2025 |
| Clipboard + flyout notification | Works | |
| Markdown history files | Works | One file per dictation in the data folder |
| First-run wizard | Works, incomplete | Does **not** ask for the `whisper-cli.exe` path; edit `config.toml` by hand ([setup guide](MANUAL_SETUP_GUIDE.md), step 8) |
| Model download + SHA-1 check | Implemented | Lightly tested |
| Settings window | Works | "Settings" button inside error dialogs is a placeholder (shows a message box) |
| LLM post-processing via `llama-cli` | Implemented | Needs matching llama.cpp DLLs (CUDA or CPU build); off by default |
| Reset / repair flow | Not verified | Part of iteration 8 |
| Latency target p95 ≤ 2.5 s (NFR-001) | Not measured | Part of iteration 8 |
| Code signing, auto-update, autostart, Whisper GPU acceleration | Out of scope for v0.1 | |

## Testing state

- ~230 xUnit tests. CI runs them on `windows-latest` for every pull request and merge to `main` and attaches a portable
  `LocalWhisper.exe` as an artifact.
- ~70 tests that open real WPF windows are tagged `Category=WpfIntegration` and excluded on
  CI; they need an interactive desktop. See the [test README](../tests/LocalWhisper.Tests/README.md).
- No automated end-to-end test with a real `whisper-cli`. "Works" above means: verified by
  hand in December 2025.

## Known issues

1. Wizard lacks the whisper-cli path step (see above). Biggest usability gap.
2. Error dialog "Settings" button is a placeholder (PH-001 in the [placeholders tracker](meta/placeholders-tracker.md)).
3. Windows SmartScreen warns on first start because the EXE is not code-signed.
4. Post-processing fails with exit code `-1073741515` if the llama.cpp DLLs are missing
   ([setup guide, troubleshooting](MANUAL_SETUP_GUIDE.md#troubleshooting)).

## Picking it up again

Developer-facing notes, prioritized gaps and a ready-made agent prompt: [handover](meta/handover.md).
