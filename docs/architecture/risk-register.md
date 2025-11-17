# Risk Register

**Purpose:** Identify architecture and implementation risks with mitigation strategies
**Audience:** Architects, developers, project lead
**Status:** Living document (updated as risks emerge or are mitigated)
**Last Updated:** 2025-09-17

---

## Risk Assessment Matrix

| Risk ID | Risk | Likelihood | Impact | Priority | Mitigation | Status |
|---------|------|------------|--------|----------|------------|--------|
| R1 | Latency exceeds 2.5s p95 target | Medium | High | **High** | Warm-start CLI, small model, p95 monitoring | Active |
| R2 | SmartScreen warnings deter users | High | Medium | Medium | README guidance; code signing in future | Accepted |
| R3 | Audio device conflicts/errors | Medium | Medium | Medium | Clear error dialogs, device picker (future) | Planned |
| R4 | Model integrity (corrupted downloads) | Low | High | **High** | SHA-256 verification in wizard | Planned |
| R5 | Hotkey conflicts with other apps | Medium | Low | Low | Conflict detection + user notification | Planned |
| R6 | Data root on network drive (offline) | Low | Medium | Low | Repair flow, recommend local path | Planned |
| R7 | Clipboard locked by other app | Low | Low | Low | Retry logic, error dialog | Planned |
| R8 | CLI subprocess hangs/crashes | Low | High | **High** | Timeout enforcement, process monitoring | Planned |
| R9 | Large EXE size (self-contained) | High | Low | Low | Accepted trade-off for portability | Accepted |
| R10 | Memory leaks (long-running tray app) | Medium | Medium | Medium | Profiling in Iter-8, dispose patterns | Planned |
| R11 | Unicode/emoji in filenames (slug) | Low | Low | Low | Slug normalization rules (FR-024) | Planned |
| R12 | Post-processing changes meaning | Medium | High | **High** | Explicit prompt constraint, fallback | Planned |

---

## Detailed Risk Analysis

### R1: Latency Exceeds Target (p95 ≤ 2.5s)

**Description:**
End-to-end latency from hotkey release to clipboard write may exceed 2.5s p95 due to STT processing time, CLI subprocess overhead, or file I/O delays.

**Likelihood:** Medium (depends on CPU speed, model size, audio length)

**Impact:** High (poor user experience, core value proposition at risk)

**Mitigation Strategies:**
1. **Warm-start CLI:** Consider keeping Whisper process alive (stdin mode) to avoid cold-start overhead (~100-200ms saved)
2. **Small model default:** Use `small` model (not `large`) for faster processing
3. **Parallel operations:** Write clipboard and history in parallel (don't block clipboard on history)
4. **Performance monitoring:** Instrument all steps, measure p50/p95/p99 in Iteration 4 and 8
5. **User guidance:** Recommend local SSD, adequate CPU; document performance characteristics

**Verification:**
- Measure actual p95 in Iteration 4 with 100 test dictations (3s, 5s, 10s audio)
- If target missed: profile bottleneck, optimize, or adjust target with user approval

**Status:** Active (to be verified in Iter-4)

---

### R2: SmartScreen Warnings Deter Users

**Description:**
Without code signing, Windows SmartScreen shows "Unknown publisher" warning when users first run the EXE. This may deter non-technical users.

**Likelihood:** High (guaranteed without signing)

**Impact:** Medium (reduces adoption, but tech-savvy users will proceed)

**Mitigation Strategies:**
1. **README guidance:** Clear instructions on bypassing SmartScreen ("More info" → "Run anyway")
2. **Distributor reputation:** Build up downloads over time (SmartScreen learns)
3. **Future code signing:** Obtain certificate for v0.2+ (cost: ~$200-500/year)
4. **Alternative distribution:** Offer via Microsoft Store (auto-signed) in future

**Verification:**
- Include SmartScreen bypass instructions in README
- Test on fresh Windows VM to document exact steps

**Status:** Accepted for v0.1 (deferred to future version)

---

### R3: Audio Device Conflicts/Errors

**Description:**
Microphone may be unavailable, denied permissions, or in use by another app. Audio driver issues can cause crashes if not handled.

**Likelihood:** Medium (common in corporate environments with strict permissions)

**Impact:** Medium (app must remain stable, provide clear error)

**Mitigation Strategies:**
1. **Error handling:** Catch `COMException` from WASAPI, show user-friendly dialog (FR-021)
2. **Device enumeration:** Future feature: let user choose microphone if multiple available
3. **Retry logic:** Allow user to retry after fixing permissions
4. **Logging:** Log device info (name, state) for diagnostics

**Verification:**
- Test with microphone disabled in Windows settings
- Test with microphone in use by Zoom/Teams
- Verify app does not crash (NFR-003)

**Status:** Planned (Iter-2, Iter-8)

---

### R4: Model Integrity (Corrupted Downloads)

**Description:**
Downloaded Whisper model may be incomplete, corrupted, or tampered with. Running with a bad model causes crashes or incorrect transcriptions.

**Likelihood:** Low (modern download protocols are reliable)

**Impact:** High (app unusable, hard to diagnose for user)

**Mitigation Strategies:**
1. **SHA-256 verification:** Compute hash after download, compare to known-good value (FR-017)
2. **Retry on mismatch:** Allow user to re-download or provide their own model
3. **Checksum storage:** Store known-good hashes in config or embedded in app
4. **Partial download detection:** Check file size before hash computation

**Verification:**
- Test wizard with intentionally corrupted model file
- Verify hash mismatch is detected and reported

**Status:** Planned (Iter-5)

---

### R5: Hotkey Conflicts with Other Apps

**Description:**
User's chosen hotkey may already be registered by another app (e.g., screen capture tools, game overlays). This causes `RegisterHotKey` to fail.

**Likelihood:** Medium (common hotkeys like `Ctrl+Shift+S` are often taken)

**Impact:** Low (user can choose different hotkey)

**Mitigation Strategies:**
1. **Conflict detection:** Check `RegisterHotKey` return value, show clear error if it fails (FR-021)
2. **Guided retry:** Suggest alternative hotkeys in error dialog (e.g., "Try Ctrl+Alt+D instead")
3. **Testing:** Recommend less-common combos (e.g., `Ctrl+Shift+D` is relatively safe)

**Verification:**
- Manually register same hotkey in test app, verify detection

**Status:** Planned (Iter-1)

---

### R6: Data Root on Network Drive (Offline)

**Description:**
User chooses network drive (e.g., `Z:\`) for data root. When offline or VPN disconnected, app cannot access config or write history.

**Likelihood:** Low (most users will use local drive)

**Impact:** Medium (app fails to start or history writes fail)

**Mitigation Strategies:**
1. **Wizard warning:** Recommend local path in wizard (`%LOCALAPPDATA%` default)
2. **Repair flow:** If data root inaccessible at startup, offer to choose new path (UC-003)
3. **Graceful degradation:** If history write fails, still succeed with clipboard write
4. **Logging:** Log network path warnings

**Verification:**
- Test with network drive, disconnect, verify repair flow works

**Status:** Planned (Iter-5)

---

### R7: Clipboard Locked by Other App

**Description:**
Windows clipboard can only be opened by one app at a time. If another app has it locked, `SetText()` fails.

**Likelihood:** Low (rare, transient condition)

**Impact:** Low (user can manually copy; rare occurrence)

**Mitigation Strategies:**
1. **Retry logic:** Wait 100ms and retry once
2. **Error dialog:** If still fails, show "Clipboard gesperrt. Bitte erneut versuchen."
3. **Logging:** Log clipboard errors with context

**Verification:**
- Simulate by locking clipboard in test app, verify retry and error dialog

**Status:** Planned (Iter-4)

---

### R8: CLI Subprocess Hangs/Crashes

**Description:**
Whisper or LLM CLI may hang indefinitely, crash, or produce no output. App must not freeze or become unresponsive.

**Likelihood:** Low (mature tools, but possible with bad models or inputs)

**Impact:** High (app hangs, user loses trust)

**Mitigation Strategies:**
1. **Timeout enforcement:** Kill process after 60s (Whisper) or 10s (LLM) (ADR-0002)
2. **Process monitoring:** Check exit codes, capture stderr for diagnostics
3. **Error mapping:** Map exit codes to user-friendly errors (interface contracts)
4. **Async execution:** Run CLI on background thread, keep UI responsive
5. **Logging:** Log full command, exit code, stdout/stderr

**Verification:**
- Create mock CLI that hangs, verify app kills it after timeout
- Test with invalid model path, verify error dialog

**Status:** Planned (Iter-3, Iter-7)

---

### R9: Large EXE Size (Self-Contained Publish)

**Description:**
Self-contained .NET publish creates 150-200 MB EXE (includes entire runtime). Users may be reluctant to download such a large file.

**Likelihood:** High (guaranteed with self-contained publish)

**Impact:** Low (acceptable for desktop app; portable value justifies size)

**Mitigation Strategies:**
1. **Accepted trade-off:** Portability > size (ADR-0001)
2. **Compression:** Use UPX or similar to reduce size (optional, test stability)
3. **Future:** Consider framework-dependent publish for users with .NET 8 installed (separate download)

**Verification:**
- Measure actual EXE size after publish
- Document in README

**Status:** Accepted (not a blocker for v0.1)

---

### R10: Memory Leaks (Long-Running Tray App)

**Description:**
App runs continuously in system tray. Memory leaks in event handlers, disposable objects, or subprocess management can cause gradual memory growth.

**Likelihood:** Medium (common pitfall in long-running apps)

**Impact:** Medium (app becomes sluggish, eventual crash if severe)

**Mitigation Strategies:**
1. **Dispose patterns:** Implement `IDisposable` for audio recorder, file streams, process handles
2. **Profiling:** Use dotMemory or WPF perf tools in Iteration 8
3. **Manual testing:** Run app for 24 hours, monitor memory usage
4. **Event unsubscription:** Ensure event handlers are unregistered when components are disposed

**Verification:**
- Run app for 1000 dictations, check memory growth in Task Manager
- Target: < 200 MB after 1000 operations

**Status:** Planned (Iter-8)

---

### R11: Unicode/Emoji in Filenames (Slug)

**Description:**
User dictates text with emoji or special Unicode characters. Slug generation may produce invalid Windows filenames (`<>:"/\|?*` not allowed).

**Likelihood:** Low (most dictations are plain text)

**Impact:** Low (history write fails, but clipboard succeeds)

**Mitigation Strategies:**
1. **Slug normalization:** Replace invalid characters with `-` (FR-024)
2. **Fallback slug:** If normalization results in empty string, use `"transcript"`
3. **Testing:** Test with emoji, umlauts, special chars

**Verification:**
- Test dictations: "Meeting @ 3:00 PM", "Re: Project", "🚀 Launch today"
- Verify filenames are valid

**Status:** Planned (Iter-4)

---

### R12: Post-Processing Changes Meaning

**Description:**
LLM may hallucinate, expand abbreviations incorrectly, or change the meaning of the transcript (e.g., "let's meet" → "let us meet" is OK, but "3pm" → "5pm" is NOT OK).

**Likelihood:** Medium (LLMs can hallucinate)

**Impact:** High (user loses trust if transcript is inaccurate)

**Mitigation Strategies:**
1. **Explicit prompt constraint:** "DO NOT change the meaning" in LLM prompt (FR-022)
2. **Fallback on error:** If post-processing fails or produces suspiciously different text (e.g., > 2x length), use original STT text
3. **Optional feature:** Disabled by default, user must opt-in
4. **Testing:** Compare STT vs. post-processed text for a test set, verify meaning is preserved
5. **User education:** Document that post-processing is experimental and should be reviewed

**Verification:**
- Test with sample transcripts, verify meaning preservation
- A/B test: 10 users with/without post-processing, gather feedback

**Status:** Planned (Iter-7)

---

## Risk Monitoring

**Review Cadence:**
- **After each iteration:** Update risk status (mitigated, accepted, or new risks)
- **Before v0.1 release:** Ensure all "High" priority risks are mitigated or accepted

**Escalation:**
- If "High" risk cannot be mitigated → Flag for user/stakeholder decision (accept or defer feature)

---

## Accepted Risks (No Mitigation)

| Risk | Reason for Acceptance |
|------|----------------------|
| R2: SmartScreen warnings | Cost of code signing outweighs benefit for v0.1; future enhancement |
| R9: Large EXE size | Portability is more important than download size; users accept large apps |

---

## Related Documents

- **Architecture Overview:** `architecture/architecture-overview.md`
- **NFRs:** `specification/non-functional-requirements.md` (NFR-001, NFR-003)
- **ADRs:** `adr/0000-index.md` (ADR-0001, ADR-0002)
- **Functional Requirements:** `specification/functional-requirements.md` (FR-021, FR-022)

---

**Last updated:** 2025-09-17
**Version:** v0.1 (Initial risk register)
**Next review:** After Iteration 4 (performance verification)
