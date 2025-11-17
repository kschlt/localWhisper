# Non-Functional Requirements

**Purpose:** Quality attributes and constraints for the Dictate-to-Clipboard application
**Format:** Quality scenarios using Stimulus-Environment-Artifact-Response-Measure pattern
**Status:** Stable (v0.1 baseline)
**Last Updated:** 2025-09-17

---

## Overview

Non-functional requirements (NFRs) define **how well** the system performs its functions, rather than **what** it does. Each NFR is expressed as a quality scenario with:

- **Stimulus:** Event or condition that triggers the scenario
- **Environment:** Context in which the stimulus occurs
- **Artifact:** System component(s) affected
- **Response:** System's behavior
- **Measure:** Quantifiable target (pass/fail criterion)

---

## NFR-001 — Performance (Latency)

**Quality Attribute:** Performance

**Scenario:**

- **Stimulus:** User releases hotkey after 5 seconds of recording
- **Environment:** Standard laptop (Intel i5 or equivalent, 8GB RAM, no GPU acceleration)
- **Artifact:** End-to-end dictation pipeline (audio stop → STT → clipboard write)
- **Response:** Transcript is written to clipboard and ready to paste
- **Measure:** **95th percentile (p95) latency ≤ 2.5 seconds**

**Rationale:**
Users expect near-instant results when dictating. A p95 target allows for occasional slower processing (large transcripts, CPU load) while ensuring most operations feel fast.

**Measurement Strategy:**
- Instrument code with `Stopwatch` from hotkey-up event to clipboard-write completion
- Log latency for each operation
- After 100 test dictations (varying lengths: 3s, 5s, 10s), calculate percentiles
- Verify p95 ≤ 2.5s

**Acceptance:**
- ✓ p95 measured in Iteration 4 (first E2E integration)
- ✓ Re-measured in Iteration 8 (stabilization)
- ✓ If target is missed, identify bottleneck and optimize OR adjust target with justification

**Related:**
- UC-001
- FR-012 (STT performance is primary contributor)
- US-033 (Iteration 4)
- ADR-0002 (CLI subprocess overhead acknowledged)

---

## NFR-002 — Portabilität / Installationsfreiheit

**Quality Attribute:** Portability, Deployability

**Scenario:**

- **Stimulus:** User downloads app EXE to a new Windows laptop (no admin rights)
- **Environment:** Fresh user account, standard Windows 10/11, no prior installation
- **Artifact:** Application executable and data root
- **Response:** App runs successfully after wizard setup; no admin rights required
- **Measure:**
  - ✓ Single EXE file (self-contained publish)
  - ✓ Data root under `%LOCALAPPDATA%` (or user-chosen folder), no `Program Files`
  - ✓ No registry writes required
  - ✓ ~~Autostart via `.lnk` shortcut (no Task Scheduler or registry)~~ (REMOVED)

**Rationale:**
Many corporate laptops restrict admin rights. A portable app can be used on any Windows machine without IT approval.

**Verification:**
- ✓ Build with .NET `PublishSingleFile=true` and `SelfContained=true`
- ✓ Test on VM with standard user account (no admin group)
- ✓ Verify no UAC prompts during setup or operation
- ✓ Verify no registry keys created (inspect with Process Monitor)
- ✓ Verify app works when copied to USB drive and run on different machine

**Related:**
- UC-002
- FR-016 (Wizard)
- ADR-0001 (Platform choice supports this)
- ADR-0003 (Storage layout)

---

## NFR-003 — Zuverlässigkeit / Robustheit

**Quality Attribute:** Reliability, Availability

**Scenario:**

- **Stimulus:** Error conditions occur (missing model, microphone denied, disk full, hotkey conflict, STT timeout)
- **Environment:** Production use with various failure modes
- **Artifact:** Application core, error handling subsystem
- **Response:** App shows user-friendly error dialog, logs error with context, returns to stable state (no crash)
- **Measure:** **0 crashes** in standardized error injection test matrix

**Test Matrix:**

| Error Type | Test Action | Expected Behavior |
|------------|-------------|-------------------|
| Model file missing | Delete `models/*.gguf`, start app | Wizard repair flow; clear error message |
| Model hash mismatch | Replace model with random file | Wizard detects, shows "Modell ungültig" |
| Microphone denied | Revoke mic permissions, press hotkey | Dialog: "Mikrofon nicht verfügbar"; no crash |
| Disk full (history) | Fill disk, complete dictation | Clipboard succeeds; history fails gracefully with warning |
| Hotkey conflict | Start app with hotkey already registered | Dialog: "Hotkey belegt"; app runs without hotkey |
| STT timeout | Mock STT that hangs for 120s | Operation canceled after 60s; dialog shown |
| Data root moved | Move folder, restart app | Repair dialog shown; user re-links or resets |

**Rationale:**
Crashes erode user trust and cause data loss. Robustness is critical for a productivity tool.

**Verification:**
- ✓ Create integration tests for each error type
- ✓ Run tests in Iteration 8 (stabilization)
- ✓ Use Process Explorer to verify no zombie processes or resource leaks
- ✓ All error paths must have explicit `try-catch` with logging (no silent failures)

**Related:**
- All UC
- FR-021 (Error dialogs)
- FR-023 (Logging)
- US-071 (Iteration 8)

---

## NFR-004 — Usability

**Quality Attribute:** Usability, User Experience

**Scenarios:**

### 4a. Flyout Latency

- **Stimulus:** Clipboard write completes successfully
- **Environment:** Normal operation, no heavy CPU load
- **Artifact:** Custom flyout notification
- **Response:** Flyout appears to confirm clipboard readiness
- **Measure:** **Flyout visible ≤ 0.5 seconds after clipboard write**

**Rationale:**
Immediate feedback assures the user the operation completed. Delayed feedback causes uncertainty.

**Verification:**
- ✓ Instrument code: `Stopwatch` from clipboard write to flyout render
- ✓ Verify in Iteration 4 (flyout implementation)

---

### 4b. Wizard Completion Time

- **Stimulus:** New user starts app for first time
- **Environment:** Normal setup, default options, model already downloaded
- **Artifact:** First-run wizard
- **Response:** Wizard completes and app is ready to use
- **Measure:** **Wizard completion < 2 minutes** (measured from wizard open to app tray icon visible)

**Rationale:**
Long, complex setup discourages adoption. Fast setup encourages trial and continued use.

**Verification:**
- ✓ Manual timed testing in Iteration 5 (wizard implementation)
- ✓ 3 test scenarios:
  1. Default path, download model, default hotkey → target: < 2 min
  2. Custom path, existing model, custom hotkey → target: < 3 min
  3. With repair (folder re-selection) → target: < 2.5 min

**Related:**
- UC-002
- FR-016 (Wizard)
- US-044 (Iteration 5)

---

## NFR-005 — Privacy / Offline by Default

**Quality Attribute:** Privacy, Security

**Scenario:**

- **Stimulus:** User performs multiple dictations (normal usage)
- **Environment:** Production environment, user privacy expectations
- **Artifact:** Entire application
- **Response:** No audio or transcript data is transmitted over network
- **Measure:**
  - ✓ Zero network calls during dictation flow (verified with Wireshark/Fiddler)
  - ✓ Only permitted network call: model download during wizard (optional, user-initiated)
  - ✓ Network access can be disabled entirely (app remains functional with pre-downloaded model)

**Rationale:**
Users dictate sensitive content (work emails, personal notes, meeting discussions). Offline processing is a core value proposition.

**Verification:**
- ✓ Run app with network monitoring (Wireshark)
- ✓ Perform 10 dictations
- ✓ Verify no HTTP/HTTPS traffic except for initial model download (if chosen)
- ✓ Document this in README as a key feature

**Related:**
- Product value proposition
- FR-012 (STT is local)
- FR-022 (Post-processing is local)

---

## NFR-006 — Observability

**Quality Attribute:** Observability, Debuggability

**Scenario:**

- **Stimulus:** Normal operation and error scenarios
- **Environment:** Production or development
- **Artifact:** Logging subsystem
- **Response:** All significant events and errors are logged with sufficient context
- **Measure:**
  - ✓ Logs include: Timestamp, Log Level, Component, Message, Structured Data (key-value pairs)
  - ✓ Key events logged:
    - App start (version, config path, OS version)
    - Hotkey registration (key combo, success/conflict)
    - State transitions (Idle → Recording → Processing → Idle)
    - Audio recording (start time, stop time, file path, file size)
    - STT invocation (command line, duration, exit code, transcript length)
    - Clipboard write (success/failure, timestamp)
    - History write (file path, success/failure, timestamp)
    - Settings changes (old value → new value)
    - Errors (exception type, message, stack trace, context)
  - ✓ Log level is configurable (default: INFO; DEBUG for troubleshooting)
  - ✓ Logs do not expose sensitive transcript content in production (INFO/WARN/ERROR levels)

**Rationale:**
When users report issues, logs are the primary diagnostic tool. Without good logs, debugging is guesswork.

**Verification:**
- ✓ Review log output after running E2E scenarios (UC-001, UC-002, error cases)
- ✓ Verify log contains enough context to reconstruct event sequence
- ✓ Verify no sensitive data (full transcripts) in default log level
- ✓ Check log file rotation works (max size, archival)

**Related:**
- FR-023 (Logging)
- US-072 (Iteration 8)
- All error cases (FR-021)

---

## NFR Summary Table

| ID | Quality Attribute | Key Measure | Verification Iteration |
|----|-------------------|-------------|------------------------|
| NFR-001 | Performance | p95 latency ≤ 2.5s | Iter-4, Iter-8 |
| NFR-002 | Portability | No admin rights, self-contained EXE | Iter-1, Iter-5 |
| NFR-003 | Reliability | 0 crashes in error matrix | Iter-8 |
| NFR-004 | Usability | Flyout ≤ 0.5s, Wizard < 2 min | Iter-4, Iter-5 |
| NFR-005 | Privacy | No network calls (except model DL) | Iter-3 |
| NFR-006 | Observability | Complete event logs | All iterations |

---

## Quality Attribute Trade-offs

### Performance vs. Accuracy (NFR-001 vs. Implicit Accuracy Goal)

**Trade-off:**
- Larger Whisper models (e.g., `large`) are more accurate but slower
- Smaller models (e.g., `small`) are faster but may have lower accuracy

**Decision:**
- Default to `small` model for best balance (good accuracy, meets latency target)
- Allow advanced users to choose `large` in settings (accept slower p95)
- Document performance difference in README

---

### Portability vs. Performance (NFR-002 vs. NFR-001)

**Trade-off:**
- Self-contained EXE is larger (~150-200 MB) and has slower first-run startup
- Framework-dependent deployment is smaller and faster but requires .NET runtime pre-installed

**Decision:**
- Prioritize portability (self-contained) per ADR-0001
- Accept larger EXE size and slower cold start (NFR-001 target applies to warm operation, not first app launch)

---

### Robustness vs. Simplicity (NFR-003 vs. Development Time)

**Trade-off:**
- Comprehensive error handling adds code complexity and testing burden
- Simpler code is faster to deliver but less reliable

**Decision:**
- Prioritize robustness (per product goal: "zero friction, reliable")
- Allocate Iteration 8 specifically for stabilization and error handling
- Error matrix testing is mandatory for v0.1 release

---

## Related Documents

- **Use Cases:** `specification/use-cases.md`
- **Functional Requirements:** `specification/functional-requirements.md`
- **Architecture:** `architecture/architecture-overview.md`
- **ADRs:** `adr/0000-index.md`
- **Iteration Plan:** `iterations/iteration-plan.md`

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial NFRs; autostart removed from NFR-002)
