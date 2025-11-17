# Traceability Index

**Purpose:** Quick lookup for tracing requirements to implementations and tests
**Audience:** AI agents, developers, auditors
**Status:** Living document (updated during iterations)

---

## Traceability Chain

```
UC (Use Case)
  ├─ FR (Functional Requirement)
  │   ├─ US (User Story / Implementation)
  │   │   ├─ Code Module
  │   │   └─ BDD Scenario
  │   └─ ADR (Architecture Decision)
  └─ NFR (Non-Functional Requirement)
      └─ Metrics / Tests
```

---

## Use Cases → Requirements → Stories

### UC-001: Quick Dictation (Hold-to-Talk)

**Requirements:**
- FR-010: Hotkey (Hold-to-Talk)
- FR-011: Audio recording (WASAPI, WAV 16kHz mono)
- FR-012: STT with Whisper (CLI)
- FR-013: Clipboard write
- FR-014: History file
- FR-015: Toast/Status (Custom Flyout per ADR-0005)
- FR-024: Slug & filename generation

**User Stories:**
- US-001: Hotkey toggles state (Iter-1)
- US-010: Audio recording (Iter-2)
- US-020: STT via whisper.cpp (Iter-3)
- US-030: Clipboard write (Iter-4)
- US-031: History file with front-matter (Iter-4)
- US-032: Custom flyout (Iter-4)
- US-036: Slug generation (Iter-4)

**Non-Functional:**
- NFR-001: Performance (p95 ≤ 2.5s from KeyUp to Clipboard)
- NFR-004: Usability (Flyout ≤ 0.5s after Clipboard write)

**ADRs:**
- ADR-0001: Platform (.NET 8 + WPF)
- ADR-0002: CLI subprocesses for STT/LLM
- ADR-0003: Storage layout (Data Root)
- ADR-0005: Custom flyout (not Windows toast)

**Tests:**
- `features/DictateToClipboard.feature` (@UC-001 @FR-010 @FR-012 @FR-013 @FR-014 @FR-015 @FR-024)

**Status:** Implementation spans Iterations 1-4, 8

---

### UC-002: First-Run Installation (No Admin)

**Requirements:**
- FR-016: First-run wizard (3 steps)
- FR-017: Model management (path, hash check)
- FR-020: Settings (Hotkey, Data Root, Autostart toggle removed, etc.)

**User Stories:**
- US-040: Wizard Step 1 - Data Root selection (Iter-5)
- US-041: Wizard Step 2 - Model check with SHA-256 (Iter-5)
- US-042: Wizard Step 3 - Hotkey selection (Iter-5)
- US-044: Wizard completion < 2 min (Iter-5)
- US-050: Settings - Hotkey change (Iter-6)
- US-051: Settings - Data Root change (Iter-6)
- US-052: Settings - Language/Format (Iter-6)
- US-053: Settings - Model check/reload (Iter-6)

**Non-Functional:**
- NFR-002: Portability (no admin rights, self-contained)
- NFR-004: Usability (wizard < 2 min)
- NFR-006: Observability (log all config choices)

**ADRs:**
- ADR-0001: Platform (.NET 8 + WPF)
- ADR-0003: Storage layout
- ~~ADR-0004: Autostart via shortcut~~ (REMOVED - autostart out of scope)

**Tests:**
- `features/FirstRunWizard.feature` (@UC-002 @FR-016 @FR-017)

**Status:** Implementation in Iterations 5-6

---

### UC-003: Data Root Missing/Moved (Repair Flow)

**Requirements:**
- FR-016: First-run wizard (includes repair path)
- FR-021: Error cases (dialogs + logs)
- FR-024: Slug & filename (validation on repair)

**User Stories:**
- US-043: Repair flow when Data Root missing (Iter-5)
- US-046: Error on write-protected folder (Iter-5)
- US-071: Error handling for all edge cases (Iter-8)

**Non-Functional:**
- NFR-003: Reliability (no crashes, stable fallback)
- NFR-006: Observability (log errors with context)

**ADRs:**
- ADR-0003: Storage layout

**Tests:**
- `features/RepairAndReset.feature` (@UC-003 @FR-016 @FR-021)

**Status:** Implementation in Iterations 5, 8

---

### UC-004: Reset/Uninstall

**Requirements:**
- FR-019: Reset/Deinstallieren (remove Data Root, manual EXE deletion)

**User Stories:**
- US-070: Reset removes Data Root after confirmation (Iter-8)

**Non-Functional:**
- NFR-003: Reliability (clean removal, no orphaned files)

**ADRs:**
- ADR-0003: Storage layout

**Tests:**
- `features/RepairAndReset.feature` (@UC-004 @FR-019)

**Status:** Implementation in Iteration 8

---

## Requirements → Code Modules (To Be Updated During Implementation)

| Requirement | Description | Status | Code Module | Iteration | Story |
|-------------|-------------|--------|-------------|-----------|-------|
| FR-010 | Hotkey registration | Planned | TBD: `src/Services/HotkeyManager.cs` | Iter-1 | US-001 |
| FR-011 | Audio recording | Planned | TBD: `src/Services/AudioRecorder.cs` | Iter-2 | US-010 |
| FR-012 | STT (CLI) | Planned | TBD: `src/Adapters/WhisperCLIAdapter.cs` | Iter-3 | US-020 |
| FR-013 | Clipboard write | Planned | TBD: `src/Services/ClipboardService.cs` | Iter-4 | US-030 |
| FR-014 | History file | Planned | TBD: `src/Services/HistoryWriter.cs` | Iter-4 | US-031 |
| FR-015 | Custom flyout | Planned | TBD: `src/UI/FlyoutNotification.cs` | Iter-4 | US-032 |
| FR-016 | Wizard | Planned | TBD: `src/UI/WizardWindow.xaml.cs` | Iter-5 | US-040..042 |
| FR-017 | Model management | Planned | TBD: `src/Services/ModelManager.cs` | Iter-5 | US-041 |
| FR-019 | Reset/Uninstall | Planned | TBD: `src/Services/ResetService.cs` | Iter-8 | US-070 |
| FR-020 | Settings | Planned | TBD: `src/UI/SettingsWindow.xaml.cs` | Iter-6 | US-050..053 |
| FR-021 | Error dialogs | Planned | TBD: `src/UI/ErrorDialogs.cs` | Iter-8 | US-071 |
| FR-022 | Post-processing | Planned | TBD: `src/Adapters/PostProcessorAdapter.cs` | Iter-7 | US-060 |
| FR-023 | Logging | Planned | TBD: `src/Core/Logger.cs` | Iter-1..8 | All |
| FR-024 | Slug generation | Planned | TBD: `src/Core/SlugGenerator.cs` | Iter-4 | US-036 |

**Note:** This table will be updated during implementation as actual code modules are created.

---

## ADRs → Requirements

| ADR | Title | Affected Requirements | Iterations |
|-----|-------|----------------------|------------|
| ADR-0001 | Platform: .NET 8 + WPF | FR-010, FR-015, FR-016..020, FR-021, FR-023; NFR-001, NFR-002, NFR-004, NFR-006 | All |
| ADR-0002 | CLI Subprocesses (STT/LLM) | FR-012, FR-022; NFR-001, NFR-003, NFR-006 | Iter-3, 7 |
| ADR-0003 | Storage Layout & Data Root | FR-014, FR-017, FR-019, FR-024; NFR-002, NFR-003, NFR-006 | Iter-4, 5, 8 |
| ~~ADR-0004~~ | ~~Autostart via Shortcut~~ | ~~FR-018~~ (REMOVED) | N/A |
| ADR-0005 | Custom Flyout (not Windows Toast) | FR-015; NFR-004 | Iter-4 |

---

## Stories → Tests (BDD Scenarios)

| Story | Feature File | Scenario Tag | Status |
|-------|--------------|--------------|--------|
| US-001 | `features/DictateToClipboard.feature` | @Iter-1 @FR-010 | Seed provided |
| US-010 | `features/AudioRecording.feature` | @Iter-2 @FR-011 | Seed provided |
| US-020 | `features/STT.feature` | @Iter-3 @FR-012 | Seed provided |
| US-030, US-031, US-032 | `features/DictateToClipboard.feature` | @Iter-4 @FR-013 @FR-014 @FR-015 | Seed provided |
| US-040, US-041, US-042 | `features/FirstRunWizard.feature` | @Iter-5 @FR-016 @FR-017 | Seed provided |
| US-043, US-046 | `features/RepairAndReset.feature` | @Iter-5 @UC-003 | Seed provided |
| US-050..053 | `features/Settings.feature` | @Iter-6 @FR-020 | Seed TBD |
| US-060..062 | `features/PostProcessing.feature` | @Iter-7 @FR-022 | Seed TBD |
| US-070, US-071, US-072 | `features/RepairAndReset.feature`, `features/ErrorCases.feature` | @Iter-8 @FR-019 @FR-021 @FR-023 | Seed provided |

**Note:** "Seed provided" means outline is in `testing/bdd-feature-seeds.md`; full step definitions TBD during iteration.

---

## NFRs → Measurement Points

| NFR | Description | Measurement Point | Target | Verification Iteration |
|-----|-------------|-------------------|--------|------------------------|
| NFR-001 | Latency (E2E) | KeyUp → Clipboard write | p95 ≤ 2.5s | Iter-4, Iter-8 |
| NFR-004 | Flyout latency | Clipboard write → Flyout visible | ≤ 0.5s | Iter-4 |
| NFR-002 | Portability | Runs without admin, self-contained | Binary check, startup test | Iter-1, Iter-5 |
| NFR-003 | Reliability | No crashes on errors | Error injection tests | Iter-8 |
| NFR-005 | Privacy | No network calls (except model download) | Network trace | Iter-3 |
| NFR-006 | Observability | Logs for all key operations | Log inspection | All iterations |

---

## Quick Lookup: ID → Location

| ID | Type | File |
|----|------|------|
| UC-001 | Use Case | `specification/use-cases.md` |
| UC-002 | Use Case | `specification/use-cases.md` |
| UC-003 | Use Case | `specification/use-cases.md` |
| UC-004 | Use Case | `specification/use-cases.md` |
| FR-010 | Functional Req | `specification/functional-requirements.md` |
| FR-011 | Functional Req | `specification/functional-requirements.md` |
| ... | ... | ... |
| NFR-001 | Non-Functional Req | `specification/non-functional-requirements.md` |
| NFR-002 | Non-Functional Req | `specification/non-functional-requirements.md` |
| ... | ... | ... |
| ADR-0001 | Decision | `adr/0001-platform-dotnet-wpf.md` |
| ADR-0002 | Decision | `adr/0002-cli-subprocesses.md` |
| ... | ... | ... |
| US-001 | User Story | `iterations/iteration-01-hotkey-skeleton.md` |
| US-010 | User Story | `iterations/iteration-02-audio-recording.md` |
| ... | ... | ... |

---

## Maintenance Instructions

**When implementing a story:**
1. Update "Code Module" column in Requirements → Code Modules table
2. Update "Status" to "Implemented"
3. Add test results to Stories → Tests table

**When creating a new ADR:**
1. Add entry to ADRs → Requirements table
2. Update affected requirement files to reference new ADR
3. Add to Quick Lookup table

**When a requirement changes:**
1. Update the requirement file
2. Update this traceability index
3. Check if any stories/tests need updating

---

**Last updated:** 2025-09-17 (Initial structure)
**Next update:** After Iteration 1 completion
