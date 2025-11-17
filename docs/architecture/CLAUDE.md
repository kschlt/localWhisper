# Architecture & Design

System design, components, and architectural decisions.

---

## Files

- `architecture-overview.md` - Components, layers, technology stack
- `runtime-flows.md` - End-to-end sequence diagrams
- `interface-contracts.md` - CLI contracts (Whisper, LLM)
- `implementation-details.md` - Specific implementation guidance
- `risk-register.md` - Risks and mitigations
- **ADRs:** `../adr/*.md` - Architecture Decision Records

---

## System at a Glance

**Layers:**
```
Presentation (Tray, Wizard, Settings, Flyout)
    ↓
Services (Hotkey, Audio, Clipboard, History)
    ↓
Adapters (Whisper CLI, LLM CLI)
    ↓
Core (StateMachine, Config, Logger)
    ↓
Infrastructure (Filesystem, Win32)
```

**E2E Flow:**
```
Hold hotkey → Record audio → Whisper STT → Clipboard + History + Flyout
```

---

## Critical Constraints (from ADRs)

**Must follow these:**
- Platform: .NET 8 + WPF (ADR-0001)
- STT via CLI subprocess, NOT FFI (ADR-0002)
- Single data root folder (ADR-0003)
- Custom flyout, NOT Windows toast (ADR-0005)

**See:** `../adr/0000-index.md` for all decisions

---

## CLI Contracts (ADR-0002)

**Whisper CLI:**
```bash
whisper-cli.exe --model <path> --language <lang> --input <wav> --output <json>
```
Returns: `{"text": "...", "language": "...", "duration_sec": ...}`

**LLM CLI (optional):**
```bash
llm-cli.exe --mode format --input - --output -
```
Stdin: raw transcript → Stdout: formatted text

**See:** `interface-contracts.md` for full specs

---

## Data Root Structure (ADR-0003)

```
<DATA_ROOT>/
  config/config.toml
  models/whisper-*.bin
  history/YYYY/YYYY-MM/YYYY-MM-DD/*.md
  logs/app.log
  tmp/*.wav
```

---

## When Implementing

**Before coding a component:**
1. Check relevant ADR (e.g., ADR-0002 for CLI adapters)
2. Check `interface-contracts.md` if working with external tools
3. Check `runtime-flows.md` for sequence diagrams

**While coding:**
- Follow layer separation (don't mix presentation + business logic)
- Use async/await for I/O operations (audio, CLI, file writes)
- Add structured logging (see FR-023)

**Common queries:**
```bash
# Check ADR
Read: ../adr/0000-index.md  # Find relevant ADR number
Read: ../adr/00XX-*.md      # Read specific decision

# Check contracts
Read: interface-contracts.md  # CLI JSON formats, exit codes

# Check flows
Read: runtime-flows.md  # E2E sequence diagrams
```

---

## Design Principles

- **Separation of concerns:** UI doesn't contain business logic
- **Fail-safe:** Critical path (clipboard) succeeds even if secondary ops fail
- **Explicit state:** All transitions via StateMachine, logged
- **Testability:** Services use interfaces (mockable)
