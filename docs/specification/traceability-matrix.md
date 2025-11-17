# Traceability Matrix

**Purpose:** Map relationships between Use Cases, Requirements, User Stories, ADRs, and Implementation
**Status:** Living document (updated during implementation)
**Last Updated:** 2025-09-17

---

## Use Cases to Functional Requirements

| Use Case | Description | Functional Requirements |
|----------|-------------|-------------------------|
| UC-001 | Quick Dictation | FR-010, FR-011, FR-012, FR-013, FR-014, FR-015, FR-024 |
| UC-002 | First-Run Installation | FR-016, FR-017, FR-020 |
| UC-003 | Data Root Missing/Moved | FR-016 (repair), FR-021, FR-024 |
| UC-004 | Reset/Uninstall | FR-019 |

---

## Use Cases to Non-Functional Requirements

| Use Case | Description | Non-Functional Requirements |
|----------|-------------|----------------------------|
| UC-001 | Quick Dictation | NFR-001 (Latency), NFR-004 (Flyout), NFR-005 (Privacy) |
| UC-002 | First-Run Installation | NFR-002 (Portability), NFR-004 (Wizard time), NFR-006 (Logs) |
| UC-003 | Data Root Missing/Moved | NFR-003 (Reliability), NFR-006 (Logs) |
| UC-004 | Reset/Uninstall | NFR-003 (Reliability) |

---

## Functional Requirements to User Stories

| FR ID | FR Title | User Stories | Iteration(s) |
|-------|----------|--------------|--------------|
| FR-010 | Hotkey (Hold-to-Talk) | US-001, US-002, US-003 | 1 |
| FR-011 | Audio Recording | US-010, US-011, US-012 | 2 |
| FR-012 | STT with Whisper (CLI) | US-020, US-021, US-022, US-023 | 3 |
| FR-013 | Clipboard Write | US-030 | 4 |
| FR-014 | History File | US-031 | 4 |
| FR-015 | Custom Flyout | US-032, US-034 | 4 |
| FR-016 | First-Run Wizard | US-040, US-041, US-042, US-043, US-044, US-046 | 5 |
| FR-017 | Model Management | US-041, US-053 | 5, 6 |
| ~~FR-018~~ | ~~Autostart~~ | ~~(removed)~~ | N/A |
| FR-019 | Reset/Uninstall | US-070 | 8 |
| FR-020 | Settings | US-050, US-051, US-052, US-053 | 6 |
| FR-021 | Error Dialogs | US-003, US-012, US-022, US-046, US-071 | 1, 2, 3, 5, 8 |
| FR-022 | Post-Processing | US-060, US-061, US-062 | 7 |
| FR-023 | Logging | US-072 (+ all stories implicitly) | 1-8 |
| FR-024 | Slug Generation | US-036 | 4 |

---

## Functional Requirements to Code Modules

**Note:** This section will be populated during implementation.

| FR ID | FR Title | Code Modules | Status |
|-------|----------|--------------|--------|
| FR-010 | Hotkey (Hold-to-Talk) | TBD: `src/Services/HotkeyManager.cs` | Planned |
| FR-011 | Audio Recording | TBD: `src/Services/AudioRecorder.cs` | Planned |
| FR-012 | STT with Whisper (CLI) | TBD: `src/Adapters/WhisperCLIAdapter.cs` | Planned |
| FR-013 | Clipboard Write | TBD: `src/Services/ClipboardService.cs` | Planned |
| FR-014 | History File | TBD: `src/Services/HistoryWriter.cs` | Planned |
| FR-015 | Custom Flyout | TBD: `src/UI/FlyoutNotification.xaml.cs` | Planned |
| FR-016 | First-Run Wizard | TBD: `src/UI/WizardWindow.xaml.cs` | Planned |
| FR-017 | Model Management | TBD: `src/Services/ModelManager.cs` | Planned |
| FR-019 | Reset/Uninstall | TBD: `src/Services/ResetService.cs` | Planned |
| FR-020 | Settings | TBD: `src/UI/SettingsWindow.xaml.cs` | Planned |
| FR-021 | Error Dialogs | TBD: `src/UI/ErrorDialogs.cs` | Planned |
| FR-022 | Post-Processing | TBD: `src/Adapters/PostProcessorAdapter.cs` | Planned |
| FR-023 | Logging | TBD: `src/Core/AppLogger.cs` | Planned |
| FR-024 | Slug Generation | TBD: `src/Core/SlugGenerator.cs` | Planned |

---

## ADRs to Requirements

| ADR | Title | Affected Requirements |
|-----|-------|----------------------|
| ADR-0001 | Platform: .NET 8 + WPF | FR-010, FR-011, FR-015, FR-016..020, FR-021, FR-023; NFR-001, NFR-002, NFR-004, NFR-006 |
| ADR-0002 | CLI Subprocesses for STT/LLM | FR-012, FR-022; NFR-001, NFR-003, NFR-006 |
| ADR-0003 | Storage Layout & Data Root | FR-014, FR-017, FR-019, FR-024; NFR-002, NFR-003, NFR-006 |
| ~~ADR-0004~~ | ~~Autostart via Shortcut~~ | ~~FR-018~~ (removed) |
| ADR-0005 | Custom Flyout (not Windows Toast) | FR-015; NFR-004 |

---

## User Stories to BDD Scenarios

| User Story | BDD Feature File | Scenario Tags | Status |
|------------|------------------|---------------|--------|
| US-001 | `features/DictateToClipboard.feature` | @Iter-1 @UC-001 @FR-010 | Seed provided |
| US-002 | `features/DictateToClipboard.feature` | @Iter-1 @FR-010 | Seed provided |
| US-003 | `features/ErrorCases.feature` | @Iter-1 @FR-021 | Seed provided |
| US-010 | `features/AudioRecording.feature` | @Iter-2 @FR-011 | Seed TBD |
| US-011 | `features/AudioRecording.feature` | @Iter-2 @FR-011 | Seed TBD |
| US-012 | `features/ErrorCases.feature` | @Iter-2 @FR-021 | Seed TBD |
| US-020 | `features/STT.feature` | @Iter-3 @UC-001 @FR-012 | Seed provided |
| US-021 | `features/STT.feature` | @Iter-3 @FR-012 | Seed provided |
| US-022 | `features/ErrorCases.feature` | @Iter-3 @FR-021 | Seed TBD |
| US-023 | `features/ModelCheck.feature` | @Iter-3 @FR-017 | Seed TBD |
| US-030 | `features/DictateToClipboard.feature` | @Iter-4 @FR-013 | Seed provided |
| US-031 | `features/DictateToClipboard.feature` | @Iter-4 @FR-014 @FR-024 | Seed provided |
| US-032 | `features/DictateToClipboard.feature` | @Iter-4 @FR-015 | Seed provided |
| US-033 | Performance test (not BDD) | @Iter-4 @NFR-001 | Manual measurement |
| US-034 | Performance test (not BDD) | @Iter-4 @NFR-004 | Manual measurement |
| US-035 | Log inspection (not BDD) | @Iter-4 @FR-023 | Manual verification |
| US-036 | `features/SlugGeneration.feature` | @Iter-4 @FR-024 | Seed TBD |
| US-040 | `features/FirstRunWizard.feature` | @Iter-5 @UC-002 @FR-016 | Seed provided |
| US-041 | `features/FirstRunWizard.feature` | @Iter-5 @FR-017 | Seed provided |
| US-042 | `features/FirstRunWizard.feature` | @Iter-5 @FR-016 | Seed provided |
| US-043 | `features/RepairAndReset.feature` | @Iter-5 @UC-003 @FR-016 | Seed provided |
| US-044 | Performance test (not BDD) | @Iter-5 @NFR-004 | Manual measurement |
| US-045 | Log inspection (not BDD) | @Iter-5 @NFR-006 | Manual verification |
| US-046 | `features/ErrorCases.feature` | @Iter-5 @FR-021 | Seed TBD |
| US-050 | `features/Settings.feature` | @Iter-6 @FR-020 | Seed TBD |
| US-051 | `features/Settings.feature` | @Iter-6 @FR-020 | Seed TBD |
| US-052 | `features/Settings.feature` | @Iter-6 @FR-020 | Seed TBD |
| US-053 | `features/Settings.feature` | @Iter-6 @FR-020 @FR-017 | Seed TBD |
| US-060 | `features/PostProcessing.feature` | @Iter-7 @FR-022 | Seed TBD |
| US-061 | `features/PostProcessing.feature` | @Iter-7 @FR-022 | Seed TBD |
| US-062 | `features/PostProcessing.feature` | @Iter-7 @FR-022 | Seed TBD |
| US-070 | `features/RepairAndReset.feature` | @Iter-8 @UC-004 @FR-019 | Seed provided |
| US-071 | `features/ErrorCases.feature` | @Iter-8 @FR-021 | Seed provided |
| US-072 | Log inspection (not BDD) | @Iter-8 @FR-023 @NFR-006 | Manual verification |
| US-073 | Performance test (not BDD) | @Iter-8 @NFR-001 @NFR-004 | Manual measurement |
| US-074 | Documentation (not BDD) | @Iter-8 | README update |
| US-075 | Documentation (not BDD) | @Iter-8 | Changelog & tag |
| US-076 | Documentation (not BDD) | @Iter-8 | PR template |

---

## Iterations to User Stories

| Iteration | Focus | User Stories |
|-----------|-------|--------------|
| 1 | Hotkey & App Skeleton | US-001, US-002, US-003 |
| 2 | Audio Recording | US-010, US-011, US-012 |
| 3 | STT with Whisper | US-020, US-021, US-022, US-023 |
| 4 | Clipboard + History + Flyout | US-030, US-031, US-032, US-033, US-034, US-035, US-036 |
| 5 | First-Run Wizard + Model Check | US-040, US-041, US-042, US-043, US-044, US-045, US-046 |
| 6 | Settings | US-050, US-051, US-052, US-053 |
| 7 | Optional Post-Processing | US-060, US-061, US-062 |
| 8 | Stabilization + Reset + Logs | US-070, US-071, US-072, US-073, US-074, US-075, US-076 |

---

## Requirements Coverage Summary

### Functional Requirements

**Total:** 14 functional requirements (FR-010 through FR-024, excluding removed FR-018)

**Coverage by Iteration:**
- Iteration 1: FR-010, FR-021, FR-023
- Iteration 2: FR-011, FR-021, FR-023
- Iteration 3: FR-012, FR-021, FR-023
- Iteration 4: FR-013, FR-014, FR-015, FR-024, FR-023
- Iteration 5: FR-016, FR-017, FR-021, FR-023
- Iteration 6: FR-020, FR-017, FR-023
- Iteration 7: FR-022, FR-023
- Iteration 8: FR-019, FR-021, FR-023

**Status:** All functional requirements covered by implementation plan.

---

### Non-Functional Requirements

**Total:** 6 NFRs

**Verification Points:**
- NFR-001 (Performance): Measured in Iterations 4 & 8
- NFR-002 (Portability): Verified in Iterations 1 & 5
- NFR-003 (Reliability): Verified in Iteration 8
- NFR-004 (Usability): Measured in Iterations 4 & 5
- NFR-005 (Privacy): Verified in Iteration 3
- NFR-006 (Observability): Verified in all iterations (logging)

**Status:** All NFRs have explicit verification steps.

---

## Reverse Trace: Implementation to Requirements

**Template for future use during implementation:**

| Code Module | Purpose | Satisfies FRs | Related UC | Tests |
|-------------|---------|---------------|------------|-------|
| `HotkeyManager.cs` | Global hotkey registration | FR-010 | UC-001 | `features/DictateToClipboard.feature` @Iter-1 |
| `AudioRecorder.cs` | WASAPI recording | FR-011 | UC-001 | `features/AudioRecording.feature` @Iter-2 |
| ... | ... | ... | ... | ... |

---

## Gap Analysis

**Functional Gaps:** None identified (all FRs have stories)

**Non-Functional Gaps:** None identified (all NFRs have verification)

**Test Gaps (Seeds TBD):**
- Audio recording validation (Iteration 2)
- STT error scenarios (Iteration 3)
- Settings UI (Iteration 6)
- Post-processing fallback (Iteration 7)

→ These will be created during their respective iterations.

---

## Related Documents

- **Use Cases:** `specification/use-cases.md`
- **Functional Requirements:** `specification/functional-requirements.md`
- **Non-Functional Requirements:** `specification/non-functional-requirements.md`
- **ADRs:** `adr/0000-index.md`
- **Iteration Plan:** `iterations/iteration-plan.md`
- **BDD Seeds:** `testing/bdd-feature-seeds.md`

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial traceability matrix)
**Next update:** After Iteration 1 (populate code modules)
