# ADR-0002: STT/LLM Integration via CLI Subprocesses

**Status:** Accepted
**Date:** 2025-09-17
**Affected Requirements:** FR-012, FR-022; NFR-001, NFR-003, NFR-006

---

## Context

We need to integrate Whisper (for speech-to-text) and optionally a local LLM (for post-processing) into our application. These are separate tools with their own models and runtimes.

**Requirements:**
- Invoke Whisper to transcribe audio (WAV → text)
- Optionally invoke LLM to format/improve transcription
- Support different models and versions without rebuilding app
- Handle errors gracefully (missing tools, timeouts, crashes)
- Enable easy debugging and diagnostics
- Meet performance targets (NFR-001: p95 ≤ 2.5s)

**Constraints:**
- Whisper and LLM are external tools (not .NET libraries)
- User may have custom models or tool versions
- App must remain stable even if external tools crash

---

## Options Considered

### Option A: CLI Subprocesses (Invoke via `ProcessStartInfo`)

**Description:** Launch Whisper and LLM as separate command-line processes. Communicate via stdin/stdout/files.

**Pros:**
+ **Clear separation of concerns:** App manages UI and workflow; tools do STT/LLM.
+ **Easy debugging:** Can test CLI commands independently (run in terminal, inspect output).
+ **Version independence:** User can update Whisper/LLM without recompiling app.
+ **Robust error handling:** Process exit codes and stderr provide clear error signals.
+ **Model flexibility:** User can swap models or use different CLI tools (e.g., `whisper-cpp`, `ollama`).
+ **Timeout enforcement:** Can kill process after timeout (prevents hangs).
+ **Logging:** Can capture and log full command, stdout, stderr for diagnostics.

**Cons:**
- **Subprocess overhead:** Starting a process adds ~50-100ms latency (cold start).
- **I/O overhead:** Writing/reading files (WAV, JSON) adds minor latency.
- **Complexity:** Need to manage process lifecycle (start, monitor, kill, cleanup).

---

### Option B: FFI (Foreign Function Interface via P/Invoke or C interop)

**Description:** Link Whisper and LLM as native libraries (`.dll` or `.so`) and call functions directly.

**Pros:**
+ **Faster invocation:** No subprocess overhead (~50-100ms saved).
+ **Lower latency:** Potential p95 improvement (~10-15% overall).

**Cons:**
- **Tight coupling:** App depends on specific library versions and ABIs.
- **Harder debugging:** Crashes in native code can bring down entire app.
- **Platform complexity:** Need to manage native dependencies, linking, and paths.
- **Model constraints:** Harder to swap models or tools without rebuilding.
- **Error isolation:** Native crash is catastrophic (vs. subprocess crash is recoverable).
- **Development effort:** Significant complexity for C interop, marshaling, memory management.

---

### Option C: Embedded Models (Compile into app)

**Description:** Embed Whisper/LLM models directly into the app binary or as bundled resources.

**Pros:**
+ **Simplicity:** Everything in one package.

**Cons:**
- **Massive binary size:** Models are ~1-10 GB; infeasible for distribution.
- **No flexibility:** Users cannot change models.
- **Licensing issues:** Whisper models have specific licenses.
- **Update difficulty:** Every model update requires full app rebuild.

**Verdict:** Not viable.

---

## Decision

We choose **Option A: CLI Subprocesses**.

**Rationale:**
1. **Robustness:** Process isolation ensures external tool crashes don't bring down the app (NFR-003).
2. **Debuggability:** Can reproduce issues by running CLI commands manually (NFR-006).
3. **Flexibility:** Users can upgrade models or switch tools without touching the app.
4. **Development velocity:** Simpler than FFI; no need to manage native interop.
5. **Acceptable performance trade-off:** Subprocess overhead (~50-100ms) is small relative to STT time (~1-2s). p95 target (2.5s) is still achievable.

---

## Consequences

### Positive

✅ **Clear error boundaries:** If Whisper hangs, we kill the process and show error. App stays responsive.

✅ **Easy testing:** Can test with mock CLI tools (return fixed JSON after delay).

✅ **Diagnosability:** Log full command and output; users can reproduce issues in terminal.

✅ **Model flexibility:** User can bring their own models, use `whisper-cpp` vs. `whisper.cpp`, etc.

✅ **Version independence:** Whisper updates don't require app rebuild.

### Negative

❌ **Subprocess latency:** Adds ~50-100ms to E2E latency (reduces performance budget).
  - **Mitigation:**
    - Use "warm start" if possible (keep process alive between invocations; future optimization).
    - Optimize other parts of pipeline (parallel clipboard/history writes).
    - Document that larger models (e.g., `large`) will be slower.

❌ **Dependency management:** User must install Whisper CLI separately (or we bundle it).
  - **Mitigation:**
    - Wizard offers to download CLI + model during setup.
    - Clear error messages if CLI is missing.

❌ **Process management complexity:** Must handle timeouts, zombies, file cleanup.
  - **Mitigation:**
    - Use `ProcessStartInfo` with redirected streams and timeout enforcement.
    - Ensure WAV and JSON temp files are cleaned up.

### Neutral

⚪ **Latency trade-off is acceptable:** p95 target (2.5s) includes subprocess overhead. If we miss target, we optimize or adjust expectations.

---

## Implementation Notes

### Whisper CLI Invocation

**Command:**
```bash
whisper-cli.exe --model <path> --language <lang> --output-format json --output-file <path> <input.wav>
```

**Process Setup:**
```csharp
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = whisperCliPath,
        Arguments = $"--model \"{modelPath}\" --language {lang} --output-format json --output-file \"{outputPath}\" \"{inputPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }
};

process.Start();
var timeout = TimeSpan.FromSeconds(60);
if (!process.WaitForExit((int)timeout.TotalMilliseconds))
{
    process.Kill();
    throw new TimeoutException("STT process exceeded timeout");
}

if (process.ExitCode != 0)
{
    var stderr = process.StandardError.ReadToEnd();
    throw new STTException($"STT failed with exit code {process.ExitCode}: {stderr}");
}
```

### Error Handling

**Exit Code Mapping:**
- `0`: Success → Parse JSON
- `2`: Model error → Show "Model not found" dialog
- `3`: Device error → Show "Microphone error" dialog
- `4`: Timeout → Show "Operation timed out" dialog
- Other: Generic error → Log stderr, show generic dialog

**Timeout Enforcement:**
- Whisper: 60 seconds (configurable)
- LLM: 10 seconds (shorter, as it's optional and should be fast)

### Logging

**Log all invocations:**
```csharp
logger.LogInformation("STT invocation: {Command}", fullCommandLine);
logger.LogInformation("STT completed: ExitCode={ExitCode}, Duration={Duration}ms, TranscriptLength={Length}",
    exitCode, duration, transcriptLength);
```

---

## Related Decisions

- **ADR-0001:** .NET platform enables easy process management (`Process` class)
- **ADR-0003:** Storage layout includes `tmp/` for WAV and JSON files

---

## Related Requirements

**Functional:**
- FR-012: STT with Whisper → Implemented via CLI adapter
- FR-022: Post-processing → Optional CLI adapter for LLM

**Non-Functional:**
- NFR-001: Performance → Subprocess overhead acknowledged; p95 target still achievable
- NFR-003: Reliability → Process isolation prevents crashes
- NFR-006: Observability → Full command and output logged

---

## Interface Contracts

**See:** `architecture/interface-contracts.md` for detailed CLI invocation, JSON schema, exit codes.

**Key Points:**
- Whisper outputs JSON with `{ "text": "...", "language": "...", "duration_sec": ... }`
- LLM reads text via stdin, outputs formatted text via stdout
- Both use exit code 0 for success, non-zero for errors

---

## Future Optimizations

**If p95 latency exceeds target:**
- **Warm start:** Keep Whisper process alive in background, feed audio via stdin/pipe (avoids cold start).
- **Caching:** Cache transcriptions for identical audio (unlikely to help, but possible).
- **GPU acceleration:** Use Whisper with CUDA/DirectML (requires different CLI or library).

**Trade-off:** Warm start adds complexity (stateful process management); defer unless needed.

---

**Last updated:** 2025-09-17
**Version:** v1
