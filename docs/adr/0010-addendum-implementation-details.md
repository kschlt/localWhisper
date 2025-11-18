# ADR-0010 Addendum: llama.cpp Implementation Details

**Status:** ✅ Accepted
**Date:** 2025-11-18
**Parent:** ADR-0010 (LLM Post-Processing Architecture)
**Purpose:** Document concrete implementation details for llama.cpp integration

---

## 1. Chat Template Handling

### Research Finding

llama.cpp automatically handles chat templates via the `--jinja` flag. The Llama 3.2 3B Instruct GGUF model file contains the chat template embedded in its metadata (`tokenizer.chat_template`).

### Decision: Use Simple System Prompt (No Manual Template)

**Approach:** Use llama-cli's `-sys` (system prompt) flag instead of manually formatting chat templates.

**Rationale:**
- ✅ llama.cpp handles Llama 3.2 template automatically
- ✅ Simpler command-line invocation
- ✅ No risk of template format errors
- ✅ Compatible with future model updates

**Implementation:**

```bash
llama-cli.exe \
  -m "<DATA_ROOT>\models\llama-3.2-3b-q4.gguf" \
  -sys "<SYSTEM_PROMPT>" \
  -p "<TRANSCRIPT>" \
  -n 512 \
  --temp 0.0 \
  --top-p 0.25 \
  --repeat-penalty 1.05 \
  -t <CPU_CORES> \
  -ngl 99 \
  --log-disable
```

**Example:**

```bash
llama-cli.exe -m "C:\Data\models\llama-3.2-3b-q4.gguf" \
  -sys "You are a careful transcript formatter. Fix punctuation, capitalization, and expand common abbreviations. DO NOT change the meaning or add content." \
  -p "lets meet at 3pm asap fyi" \
  -n 512 --temp 0.0 --top-p 0.25 --repeat-penalty 1.05 -t 8 -ngl 99 --log-disable
```

**Expected Output:**
```
Let's meet at 3pm, as soon as possible. For your information...
```

---

## 2. llama.cpp Distribution Strategy

### Research Finding

llama.cpp official releases: https://github.com/ggml-org/llama.cpp/releases

**Latest stable:** `b7097` (November 2025)

**Available builds:**
- **CUDA 12.4:** `cudart-llama-bin-win-cuda-12.4-x64.zip` (373 MB)
  - SHA256: `8c79a9b226de4b3cacfd1f83d24f962d0773be79f1e7b75c6af4ded7e32ae1d6`
  - Requires NVIDIA GPU with driver ≥ 531.14
- **CPU-only:** `llama-bin-win-x64.zip` (size varies, ~50-100 MB)
  - No GPU dependencies

### Decision: Auto-Download Based on GPU Detection

**Approach:** Detect GPU during wizard setup, download appropriate build.

**Distribution Flow:**

```
[Wizard Step: Post-Processing Setup]
  ↓
[Detect GPU capabilities]
  ├─ NVIDIA GPU found → Download CUDA build (373 MB)
  └─ No GPU / AMD / Intel → Download CPU-only build (~100 MB)
  ↓
[Download llama-cli.exe to <APP_DIR>\llama\llama-cli.exe]
  ↓
[Verify SHA-256 hash]
  ↓
[Test invocation: llama-cli.exe --version]
  ↓
[If success: ✓ Ready | If fail: Show error, offer manual install]
```

**Rationale:**
- ✅ Smaller downloads (only what's needed)
- ✅ No bundling in installer (reduces app size)
- ✅ Automatic optimization (GPU users get GPU build)
- ✅ Same pattern as Whisper model download (consistent UX)

**Fallback:** If download fails, show dialog:
> "llama-cli.exe konnte nicht heruntergeladen werden. Bitte laden Sie es manuell von https://github.com/ggml-org/llama.cpp/releases herunter und platzieren Sie es unter `<APP_DIR>\llama\llama-cli.exe`."

---

## 3. GPU Detection Algorithm

### Research Finding

**CUDA Detection (NVIDIA GPUs):**
- Check for NVIDIA GPU via WMI (`Win32_VideoController`)
- Or check for `nvcuda.dll` in `%SystemRoot%\System32`

**DirectML (AMD/Intel GPUs):**
- DirectML available on Windows 10+ with DirectX 12
- Check for `d3d12.dll` in `%SystemRoot%\System32`
- Not prioritized for Iteration 7 (llama.cpp CUDA build is better optimized)

### Decision: Simple WMI-Based NVIDIA Detection

**Implementation:**

```csharp
public static class GpuDetector
{
    public static bool IsNvidiaGpuAvailable()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, VideoProcessor FROM Win32_VideoController");

            foreach (var obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString() ?? "";
                string processor = obj["VideoProcessor"]?.ToString() ?? "";

                // Check if NVIDIA GPU
                if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                    processor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("NVIDIA GPU detected: {Name} ({Processor})", name, processor);
                    return true;
                }
            }

            Log.Information("No NVIDIA GPU detected, using CPU-only build");
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to detect GPU, defaulting to CPU-only build");
            return false; // Fallback to CPU build on error
        }
    }
}
```

**Usage in Wizard:**

```csharp
// Step 2b: Post-Processing Setup (Optional)
bool hasNvidiaGpu = GpuDetector.IsNvidiaGpuAvailable();

string downloadUrl = hasNvidiaGpu
    ? "https://github.com/ggml-org/llama.cpp/releases/download/b7097/cudart-llama-bin-win-cuda-12.4-x64.zip"
    : "https://github.com/ggml-org/llama.cpp/releases/download/b7097/llama-bin-win-x64.zip";

string expectedHash = hasNvidiaGpu
    ? "8c79a9b226de4b3cacfd1f83d24f962d0773be79f1e7b75c6af4ded7e32ae1d6"
    : "<CPU_BUILD_HASH>"; // TODO: Get actual hash from GitHub

await DownloadAndExtractLlamaCli(downloadUrl, expectedHash);
```

**Performance Impact:**
- CUDA build: ~200-500ms inference (with NVIDIA GPU)
- CPU build: ~1-3s inference (acceptable, within 5s timeout)

---

## 4. Model Download Specification

### Research Finding

**Source:** Hugging Face (bartowski/Llama-3.2-3B-Instruct-GGUF)

**File Details:**
- **URL:** `https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf`
- **SHA-256:** `6c1a2b41161032677be168d354123594c0e6e67d2b9227c84f296ad037c728ff`
- **File Size:** 2,168,659,968 bytes (2.02 GB)
- **License:** Apache 2.0 (via Meta Llama 3.2)

### Decision: Same Download Pattern as Whisper

**Implementation:** Use `HttpClient` with progress reporting, SHA-256 verification.

**Code Example:**

```csharp
public static class ModelDownloader
{
    private const string MODEL_URL =
        "https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf";

    private const string MODEL_SHA256 =
        "6c1a2b41161032677be168d354123594c0e6e67d2b9227c84f296ad037c728ff";

    private const long MODEL_SIZE_BYTES = 2_168_659_968;

    public static async Task<bool> DownloadLlamaModelAsync(
        string destinationPath,
        IProgress<int> progress,
        CancellationToken ct)
    {
        try
        {
            // 1. Download with progress
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            using var response = await client.GetAsync(MODEL_URL, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var fileStream = File.Create(destinationPath);
            using var httpStream = await response.Content.ReadAsStreamAsync(ct);

            long totalBytes = response.Content.Headers.ContentLength ?? MODEL_SIZE_BYTES;
            long downloadedBytes = 0;
            byte[] buffer = new byte[8192];

            while (true)
            {
                int bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                downloadedBytes += bytesRead;

                int percentComplete = (int)((downloadedBytes * 100) / totalBytes);
                progress?.Report(percentComplete);
            }

            // 2. Verify SHA-256 hash
            using var sha256 = SHA256.Create();
            fileStream.Seek(0, SeekOrigin.Begin);
            byte[] hashBytes = await sha256.ComputeHashAsync(fileStream, ct);
            string actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            if (actualHash != MODEL_SHA256)
            {
                Log.Error("Model hash mismatch: expected {Expected}, got {Actual}",
                    MODEL_SHA256, actualHash);
                File.Delete(destinationPath);
                return false;
            }

            Log.Information("Model downloaded and verified: {Path} ({Size} bytes)",
                destinationPath, MODEL_SIZE_BYTES);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download LLM model from {Url}", MODEL_URL);
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);
            return false;
        }
    }
}
```

**No Fallback Mirrors:** Hugging Face is reliable. If download fails, user can retry or manually download.

---

## 5. CLI Output Parsing

### Research Finding

llama-cli outputs **timing information** before the actual generated text:

```
llama_print_timings:        load time =     234.56 ms
llama_print_timings:      sample time =      12.34 ms /   50 tokens
llama_print_timings: eval time =     456.78 ms /   30 tokens

Let's meet at 3pm, as soon as possible. For your information...
```

**Flags tested:**
- `--log-disable`: Disables some logging but may cause freezing issues
- `--silent-prompt`: Not available in llama-cli (only in llamafile)

### Decision: Parse stdout, Strip Metadata Lines

**Approach:** Capture stdout, remove lines starting with `llama_`, trim whitespace.

**Implementation:**

```csharp
private string CleanLlamaOutput(string rawOutput)
{
    if (string.IsNullOrWhiteSpace(rawOutput))
        return rawOutput;

    // Split into lines
    var lines = rawOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

    // Filter out metadata lines (start with "llama_")
    var textLines = lines
        .Where(line => !line.TrimStart().StartsWith("llama_", StringComparison.OrdinalIgnoreCase))
        .Where(line => !string.IsNullOrWhiteSpace(line));

    // Rejoin and trim
    string cleaned = string.Join(" ", textLines).Trim();

    return cleaned;
}
```

**Example:**

```csharp
string rawOutput = @"llama_print_timings:        load time =     234.56 ms
llama_print_timings:      sample time =      12.34 ms
Let's meet at 3pm, as soon as possible.";

string cleaned = CleanLlamaOutput(rawOutput);
// Result: "Let's meet at 3pm, as soon as possible."
```

**Fallback:** If cleaned output is empty after stripping, use original transcript (error scenario).

---

## 6. Configuration Schema

### Decision: Hardcode LLM Parameters, Expose Only Essential Settings

**Rationale:**
- Users don't need to tweak `temp`, `top_p`, `max_tokens` for transcript formatting
- Reduces configuration complexity
- Prevents users from breaking functionality with bad settings

**Config Schema:**

```toml
[postprocessing]
enabled = false  # true | false
llm_cli_path = "C:\\Program Files\\LocalWhisper\\llama\\llama-cli.exe"  # Auto-set during wizard
llm_model_path = "${data_root}\\models\\llama-3.2-3b-q4.gguf"  # Auto-set during wizard
llm_model_hash_sha256 = "6c1a2b41161032677be168d354123594c0e6e67d2b9227c84f296ad037c728ff"
timeout_ms = 5000  # Timeout for LLM invocation (5 seconds)
use_gpu = true  # Auto-detected, can be manually disabled if GPU causes issues
```

**Hardcoded Parameters (in `LlmCliAdapter`):**

```csharp
private const int MAX_TOKENS = 512;
private const double TEMPERATURE = 0.0;  // Deterministic
private const double TOP_P = 0.25;       // Low creativity
private const double REPEAT_PENALTY = 1.05;  // Slight penalty for repetition
```

**Why hardcode?**
- ✅ Optimal values tested and documented
- ✅ Prevents user misconfiguration
- ✅ Can be made configurable in post-v1.0 if needed

---

## 7. Trigger Word Stripping Logic

### Decision: First/Last 5 Words, Strip "markdown mode"

**Algorithm:**

```csharp
private bool DetectAndStripMarkdownMode(string transcript, out string cleanedTranscript)
{
    // Extract first 5 words and last 5 words
    string[] words = transcript.Split(new[] { ' ', '\t', '\n', '\r' },
        StringSplitOptions.RemoveEmptyEntries);

    if (words.Length == 0)
    {
        cleanedTranscript = transcript;
        return false;
    }

    // First 5 words
    string firstWords = string.Join(" ", words.Take(Math.Min(5, words.Length)));

    // Last 5 words
    string lastWords = words.Length > 5
        ? string.Join(" ", words.Skip(Math.Max(0, words.Length - 5)))
        : "";

    // Check for trigger (case insensitive)
    var triggerRegex = new Regex(@"\bmarkdown\s+mode\b", RegexOptions.IgnoreCase);

    bool foundInFirst = triggerRegex.IsMatch(firstWords);
    bool foundInLast = triggerRegex.IsMatch(lastWords);

    if (!foundInFirst && !foundInLast)
    {
        cleanedTranscript = transcript;
        return false; // No trigger found → Plain Text mode
    }

    // Strip trigger phrase (preserve rest of text)
    cleanedTranscript = transcript;

    if (foundInFirst)
    {
        // Remove from beginning (handle punctuation)
        cleanedTranscript = triggerRegex.Replace(cleanedTranscript, "", 1).TrimStart(' ', '.', ',');
    }

    if (foundInLast && foundInFirst != foundInLast) // Don't strip twice if same occurrence
    {
        // Remove from end (handle punctuation)
        cleanedTranscript = Regex.Replace(cleanedTranscript,
            @"\bmarkdown\s+mode\b\s*[.,]?\s*$", "", RegexOptions.IgnoreCase).TrimEnd();
    }

    return true; // Markdown mode detected
}
```

**Examples:**

| Input | Detected? | Cleaned Output | Mode |
|-------|-----------|----------------|------|
| `"markdown mode. Let's discuss the architecture..."` | ✅ Yes | `"Let's discuss the architecture..."` | Markdown |
| `"Let's discuss the architecture. markdown mode"` | ✅ Yes | `"Let's discuss the architecture."` | Markdown |
| `"markdown mode"` (only trigger) | ✅ Yes | `""` (empty) → **Error, fallback to original** | Error |
| `"Let's meet at 3pm asap"` | ❌ No | `"Let's meet at 3pm asap"` | Plain Text |

**Edge Case:** If cleaned text is empty after stripping, log error and use original transcript.

---

## 8. Exit Code Handling

### Research Finding

llama.cpp **does not document exit codes officially**. Based on general CLI patterns and community reports:

- **0:** Success
- **1:** General error (model not found, invalid arguments, etc.)
- **Other:** Undefined (varies by version)

**Common stderr patterns:**
- `error: failed to load model` → Model file not found or corrupt
- `CUDA error` or `out of memory` → GPU OOM
- `invalid argument` → Bad command-line parameters

### Decision: Parse stderr for Error Context

**Implementation:**

```csharp
public async Task<(bool Success, string Text)> RunLlamaCliAsync(
    string prompt, CancellationToken ct)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = _llamaCliPath,
            Arguments = $"-m \"{_modelPath}\" -sys \"{_systemPrompt}\" -p \"{prompt}\" -n 512 --temp 0.0 --top-p 0.25 -ngl 99 --log-disable",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.Start();

    // Wait with timeout
    bool exited = await process.WaitForExitAsync(ct);

    if (!exited || process.ExitCode != 0)
    {
        string stderr = await process.StandardError.ReadToEndAsync(ct);

        // Parse stderr for specific errors
        if (stderr.Contains("failed to load model", StringComparison.OrdinalIgnoreCase))
        {
            Log.Error("LLM model not found or corrupt: {Stderr}", stderr);
            return (false, ""); // Fallback to original transcript
        }
        else if (stderr.Contains("CUDA", StringComparison.OrdinalIgnoreCase) ||
                 stderr.Contains("out of memory", StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning("GPU error, retrying with CPU fallback: {Stderr}", stderr);
            // TODO: Retry with -ngl 0 (CPU-only)
        }
        else
        {
            Log.Error("LLM invocation failed (exit code {ExitCode}): {Stderr}",
                process.ExitCode, stderr);
        }

        return (false, "");
    }

    // Success: parse stdout
    string rawOutput = await process.StandardOutput.ReadToEndAsync(ct);
    string cleaned = CleanLlamaOutput(rawOutput);

    return (true, cleaned);
}
```

**GPU Fallback:** If CUDA error detected, retry once with `-ngl 0` (CPU-only mode).

---

## 9. Process Timeout & Cancellation

### Research Finding

**.NET Best Practices (2024-2025):**
- Use `CancellationTokenSource.CancelAfter(ms)` for timeout
- Use `Process.WaitForExitAsync(CancellationToken)` (.NET 5+)
- Call `Process.Kill()` if timeout reached
- Handle `InvalidOperationException` (process already exited)

### Decision: CancellationToken + Process.Kill()

**Implementation:**

```csharp
public async Task<(bool Success, string Text)> ProcessTranscriptAsync(
    string transcript, CancellationToken externalCt)
{
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_timeoutMs));
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

    Process? process = null;

    try
    {
        process = new Process { /* StartInfo setup */ };
        process.Start();

        // Wait for exit with timeout
        await process.WaitForExitAsync(linkedCts.Token);

        // Check exit code
        if (process.ExitCode != 0)
        {
            string stderr = await process.StandardError.ReadToEndAsync(linkedCts.Token);
            Log.Error("LLM process failed: {Stderr}", stderr);
            return (false, transcript); // Fallback
        }

        // Parse output
        string rawOutput = await process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        string cleaned = CleanLlamaOutput(rawOutput);

        return (true, cleaned);
    }
    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
    {
        // Timeout reached
        Log.Warning("LLM process timeout after {Timeout}ms, killing process", _timeoutMs);

        try
        {
            if (process != null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true); // Kill child processes too
                await process.WaitForExitAsync(CancellationToken.None); // Wait for cleanup
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited, ignore
        }

        return (false, transcript); // Fallback to original
    }
    catch (Exception ex)
    {
        Log.Error(ex, "LLM processing failed unexpectedly");
        return (false, transcript);
    }
    finally
    {
        process?.Dispose();
    }
}
```

**Key Points:**
- ✅ `CancellationTokenSource.CreateLinkedTokenSource` combines external cancellation + timeout
- ✅ `Process.Kill(entireProcessTree: true)` kills child processes (important for llama.cpp which may spawn GPU workers)
- ✅ Always fallback to original transcript on timeout/error
- ✅ Log all failures for diagnostics

---

## Summary: All Gaps Closed

| # | Question | Decision | Documented |
|---|----------|----------|------------|
| 1 | Chat template format | Use `-sys` flag (llama.cpp handles template) | ✅ |
| 2 | llama.cpp distribution | Auto-download from GitHub based on GPU detection | ✅ |
| 3 | Which build to download | Detect NVIDIA GPU → CUDA build; else CPU build | ✅ |
| 4 | Model download details | SHA-256: `6c1a2b...`, 2.02 GB, HttpClient pattern | ✅ |
| 5 | GPU detection | WMI check for NVIDIA via `Win32_VideoController` | ✅ |
| 6 | CLI output parsing | Strip lines starting with `llama_` | ✅ |
| 7 | Config exposure | Hardcode temp/top_p, expose only enable/timeout | ✅ |
| 8 | Trigger stripping | First/last 5 words, regex `\bmarkdown\s+mode\b` | ✅ |
| 9 | Exit codes | 0=success, 1=error, parse stderr for details | ✅ |
| 10 | Process timeout | `CancellationToken` + `Process.Kill()` after 5s | ✅ |

---

**Related Documents:**
- ADR-0010: LLM Post-Processing Architecture (parent)
- `architecture/interface-contracts.md`: CLI contracts (to be updated)
- `specification/data-structures.md`: Config schema (to be updated)

---

**Last Updated:** 2025-11-18
**Version:** v1 (Complete implementation specification)
