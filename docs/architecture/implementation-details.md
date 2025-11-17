# Architecture Implementation Details

**Purpose:** Concrete implementation decisions for developers
**Status:** Normative (must follow these patterns)
**Last Updated:** 2025-11-17
**Owner:** Solution Architect

---

## 1. State Machine Design

### States

```
┌─────────────────────────────────────────────────────────────┐
│                     STATE MACHINE                            │
└─────────────────────────────────────────────────────────────┘

    ┌──────────┐
    │  Idle    │ ◄─────────────────────────┐
    └──────────┘                           │
         │                                 │
         │ [HotkeyDown]                    │
         ▼                                 │
    ┌──────────┐                           │
    │Recording │                           │
    └──────────┘                           │
         │                                 │
         │ [HotkeyUp]                      │
         ▼                                 │
    ┌──────────┐                           │
    │Processing│                           │
    └──────────┘                           │
         │                                 │
         │ [Success OR Error]              │
         └─────────────────────────────────┘

NO separate Error state - errors return to Idle
```

### State Definitions

| State | Meaning | Tray Icon | User Can |
|-------|---------|-----------|----------|
| **Idle** | Waiting for hotkey | Default icon (white/gray) | Press hotkey, open settings, exit |
| **Recording** | Audio capture active | Red/active icon (recording) | Release hotkey (only action) |
| **Processing** | STT/post-proc/write | Spinner icon (processing) | Nothing (brief state) |

**No Error State:** Errors transition directly back to Idle (with error dialog/flyout)

### Valid Transitions

| From | Event | To | Notes |
|------|-------|----|----|
| Idle | HotkeyDown | Recording | Start audio capture |
| Recording | HotkeyUp | Processing | Stop audio, begin STT |
| Recording | RecordingError | Idle | Microphone unavailable, etc. |
| Processing | ProcessingComplete | Idle | Success: clipboard + history written |
| Processing | ProcessingError | Idle | Failure: show error dialog |
| Processing | Timeout | Idle | STT exceeds 60s |

**Invalid Transitions (will throw exception):**
- Idle → Processing (must go through Recording)
- Recording → Recording (already recording)
- Processing → Recording (must complete processing first)

### Implementation Pattern

```csharp
public enum AppState
{
    Idle,
    Recording,
    Processing
}

public class StateMachine
{
    private AppState _currentState = AppState.Idle;
    private readonly object _stateLock = new object();

    public event EventHandler<StateChangedEventArgs> StateChanged;

    public AppState CurrentState
    {
        get { lock (_stateLock) { return _currentState; } }
    }

    public void Transition(AppState newState)
    {
        lock (_stateLock)
        {
            if (!IsValidTransition(_currentState, newState))
            {
                throw new InvalidStateTransitionException(
                    $"Cannot transition from {_currentState} to {newState}");
            }

            var oldState = _currentState;
            _currentState = newState;

            _logger.LogInformation("State transition: {From} -> {To}", oldState, newState);

            StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState));
        }
    }

    private bool IsValidTransition(AppState from, AppState to)
    {
        return (from, to) switch
        {
            (AppState.Idle, AppState.Recording) => true,
            (AppState.Recording, AppState.Processing) => true,
            (AppState.Recording, AppState.Idle) => true, // Error case
            (AppState.Processing, AppState.Idle) => true,
            _ => false
        };
    }
}
```

---

## 2. Threading Model

### Thread Allocation

**Principle:** Keep UI thread responsive; offload heavy work to background threads

| Component | Thread | Rationale |
|-----------|--------|-----------|
| **UI (Tray, Dialogs, Wizard)** | UI Thread | WPF requirement |
| **Hotkey Message Handling** | UI Thread | Win32 messages arrive on UI thread |
| **State Machine Transitions** | Any (thread-safe) | Lock-protected, can be called from any thread |
| **Audio Recording** | Background Thread | NAudio uses separate audio thread; wrap in Task |
| **STT CLI Invocation** | Background Thread | Blocks for 1-2s; must not freeze UI |
| **Post-Processing** | Background Thread | Optional, can take time |
| **Clipboard Write** | UI Thread | Requires STA (WPF dispatcher) |
| **History File Write** | Background Thread | File I/O should not block UI |
| **Logging** | Any (thread-safe) | Serilog handles thread-safety |

### Implementation Patterns

**Pattern 1: UI-Initiated Background Work**

```csharp
// Hotkey handler (runs on UI thread)
private async void OnHotkeyUp(object sender, EventArgs e)
{
    _stateMachine.Transition(AppState.Processing);

    try
    {
        // Offload to background thread
        string transcript = await Task.Run(async () =>
        {
            var wavPath = await _audioRecorder.StopAndSaveAsync(); // Background I/O
            var result = await _sttAdapter.TranscribeAsync(wavPath); // Background CLI call
            return result.Text;
        });

        // Back on UI thread for clipboard (requires STA)
        await Dispatcher.InvokeAsync(() =>
        {
            _clipboardService.Write(transcript);
            _flyout.Show("Transkript im Clipboard");
        });

        // History write on background thread
        await Task.Run(() =>
        {
            _historyWriter.Write(transcript, metadata);
        });

        _stateMachine.Transition(AppState.Idle);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Processing failed");
        await Dispatcher.InvokeAsync(() =>
        {
            _errorDialogs.Show("Transkription fehlgeschlagen");
        });
        _stateMachine.Transition(AppState.Idle);
    }
}
```

**Pattern 2: Audio Recording (NAudio Callback Thread)**

```csharp
public class AudioRecorder
{
    private WaveInEvent _waveIn; // NAudio component

    public void StartRecording()
    {
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 1) // 16kHz mono
        };

        // NAudio callback runs on separate thread
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _waveIn.StartRecording();
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Runs on NAudio thread
        // Write to buffer (thread-safe)
        lock (_bufferLock)
        {
            _waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }
}
```

**Pattern 3: CLI Subprocess (Async Process Execution)**

```csharp
public class WhisperCLIAdapter
{
    public async Task<STTResult> TranscribeAsync(string wavPath, CancellationToken ct)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _whisperCliPath,
                Arguments = BuildArguments(wavPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        // Wait asynchronously (doesn't block thread)
        using (ct.Register(() => process.Kill()))
        {
            await process.WaitForExitAsync(ct);
        }

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync();
            throw new STTException($"Whisper failed: {stderr}");
        }

        // Parse JSON result
        var json = await File.ReadAllTextAsync(GetOutputPath(), ct);
        return JsonSerializer.Deserialize<STTResult>(json);
    }
}
```

### Thread Safety Guidelines

**Always use locks for:**
- State machine transitions (`_stateLock`)
- Audio buffer writes (`_bufferLock`)

**Use `Dispatcher.InvokeAsync` for:**
- Updating UI elements (tray icon, dialogs)
- Clipboard operations (requires STA)

**Safe to call from any thread:**
- Logging (`Serilog` is thread-safe)
- File I/O (use `async` methods)
- State machine `Transition()` (internally locked)

---

## 3. Configuration Management

### Config Loading Flow

```
┌─────────────────────────────────────────────────────────────┐
│                   CONFIG LOADING FLOW                        │
└─────────────────────────────────────────────────────────────┘

App Startup
    │
    ├─ 1. Check if config.toml exists
    │      Location: <DATA_ROOT>/config/config.toml
    │
    │      [Not Found] ──────────────────────┐
    │      [Found] ──────────┐               │
    │                        ▼               ▼
    ├─ 2. Parse TOML     Launch Wizard  (UC-002)
    │      using Tomlyn      │
    │                        └──> Creates config, then restart load
    │      [Parse Error] ────┐
    │      [Parse Success] ──┼─────────────────┐
    │                        │                 │
    │                        ▼                 ▼
    ├─ 3. Validate       Log Error       Continue
    │      - Required fields present
    │      - Types correct
    │      - Paths exist (model, CLI)
    │
    │      [Validation Failed] ──┐
    │      [Validation Passed] ──┼───────────┐
    │                            │           │
    │                            ▼           ▼
    ├─ 4. Handle Error     Load Defaults  Use Config
    │      - Log validation errors
    │      - Show warning dialog:
    │        "Konfiguration ungültig. Standardwerte werden verwendet."
    │      - Load hardcoded defaults
    │
    └─ 5. Continue startup with config
```

### Error Recovery Strategy

| Error Type | Behavior | User Experience |
|------------|----------|-----------------|
| **Config missing** | Launch wizard | Normal first-run experience |
| **Parse error (invalid TOML)** | Load defaults + warn | Dialog: "Konfiguration beschädigt. Standardwerte werden verwendet. Bitte prüfen Sie die Einstellungen." |
| **Validation error (e.g., model path invalid)** | Load config, validate at runtime | Dialog when feature is used: "Modell nicht gefunden. Bitte prüfen Sie die Einstellungen." |
| **Missing required field** | Use default value + warn | Warning logged; use default |

### Default Configuration Values

```csharp
public class AppConfig
{
    public static AppConfig GetDefaults()
    {
        var dataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpeechClipboardApp"
        );

        return new AppConfig
        {
            App = new AppSettings
            {
                Version = "0.1.0",
                Language = "de" // System language preferred, fallback to de
            },
            Hotkey = new HotkeySettings
            {
                Modifiers = new[] { "Ctrl", "Shift" },
                Key = "D"
            },
            Paths = new PathSettings
            {
                DataRoot = dataRoot,
                ModelPath = Path.Combine(dataRoot, "models", "ggml-small-de.gguf"),
                WhisperCliPath = Path.Combine(dataRoot, "bin", "whisper-cli.exe")
            },
            STT = new STTSettings
            {
                ModelName = "small",
                Language = "de"
            },
            History = new HistorySettings
            {
                FileFormat = "md",
                IncludeFrontmatter = true
            },
            PostProcessing = new PostProcessingSettings
            {
                Enabled = false
            },
            Logging = new LoggingSettings
            {
                Level = "INFO",
                MaxFileSizeMB = 10
            }
        };
    }
}
```

### Settings Changes - Restart Required?

| Setting | Restart Required? | Rationale | Apply Immediately? |
|---------|-------------------|-----------|-------------------|
| **Hotkey** | Yes | Must unregister old, register new (requires app restart for safety) | No |
| **Data Root** | Yes | All paths change; need to reload everything | No |
| **App Language** | Yes | UI strings need reload | No |
| **File Format (.md/.txt)** | No | Only affects next dictation | Yes |
| **STT Language** | No | Only affects next dictation | Yes |
| **Model Path** | No | Only used at next transcription | Yes (validate immediately) |
| **Post-Processing Enabled** | No | Only affects next dictation | Yes |
| **Logging Level** | No | Logger can update dynamically | Yes |

**Implementation:**

```csharp
public class SettingsManager
{
    private AppConfig _config;
    private bool _restartRequired = false;

    public void UpdateSetting(string key, object value)
    {
        switch (key)
        {
            case "Hotkey.Modifiers":
            case "Hotkey.Key":
            case "Paths.DataRoot":
            case "App.Language":
                _config.Set(key, value);
                _restartRequired = true;
                ShowRestartNotification();
                break;

            case "History.FileFormat":
            case "STT.Language":
            case "PostProcessing.Enabled":
                _config.Set(key, value);
                // Apply immediately
                break;

            default:
                throw new ArgumentException($"Unknown setting: {key}");
        }

        SaveConfig();
    }

    private void ShowRestartNotification()
    {
        MessageBox.Show(
            "Diese Änderung erfordert einen Neustart der Anwendung.",
            "Neustart erforderlich",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }
}
```

### Data Root Migration Strategy (US-051)

**Decision:** When user changes data root in Settings, offer two options:

1. **"Verschieben" (Move):** Copy all data to new location
2. **"Nur Pfad ändern" (Change Path Only):** Update config but leave files

**Implementation:**

```csharp
public class DataRootMigrationService
{
    public async Task MigrateDataRoot(string oldRoot, string newRoot)
    {
        var result = MessageBox.Show(
            "Möchten Sie die vorhandenen Daten verschieben oder nur den Pfad ändern?\n\n" +
            "Verschieben: Alle Dateien werden in den neuen Ordner kopiert.\n" +
            "Nur Pfad ändern: Dateien bleiben am alten Ort.",
            "Datenordner ändern",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            MessageBoxResult.Yes,
            new Dictionary<MessageBoxResult, string>
            {
                { MessageBoxResult.Yes, "Verschieben" },
                { MessageBoxResult.No, "Nur Pfad ändern" },
                { MessageBoxResult.Cancel, "Abbrechen" }
            }
        );

        switch (result)
        {
            case MessageBoxResult.Yes: // Verschieben
                await CopyDirectoryRecursiveAsync(oldRoot, newRoot);
                _configManager.UpdateDataRoot(newRoot);
                _logger.Information("Data root migrated from {OldRoot} to {NewRoot}", oldRoot, newRoot);
                break;

            case MessageBoxResult.No: // Nur Pfad ändern
                _configManager.UpdateDataRoot(newRoot);
                _logger.Information("Data root path changed to {NewRoot} (files not moved)", newRoot);
                break;

            case MessageBoxResult.Cancel:
                return; // User cancelled
        }

        ShowRestartNotification();
    }

    private async Task CopyDirectoryRecursiveAsync(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(source, target));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var targetFile = file.Replace(source, target);
            await Task.Run(() => File.Copy(file, targetFile, overwrite: false));
        }
    }
}
```

**Error Handling:**
- If new folder is write-protected → Show error, don't update config
- If copy fails mid-way → Show error with partial progress, offer retry
- If disk space insufficient → Show error before starting copy

---

## 4. CLI Discovery & Path Resolution

### Whisper CLI Path Resolution Strategy

```
┌─────────────────────────────────────────────────────────────┐
│               CLI PATH RESOLUTION                            │
└─────────────────────────────────────────────────────────────┘

Start
  │
  ├─ 1. Check config.toml path
  │      Path: config.whisper_cli_path
  │
  │      [Found & Executable] ──> Use this path ✓
  │      [Not Found] ──────────┐
  │                            │
  ├─ 2. Check default location │
  │      Path: <DATA_ROOT>/bin/whisper-cli.exe
  │
  │      [Found & Executable] ──> Use this path ✓
  │      [Not Found] ──────────┤
  │                            │
  ├─ 3. Search PATH environment │
  │      foreach (dir in PATH)
  │        Check: dir\whisper-cli.exe
  │
  │      [Found & Executable] ──> Use this path ✓
  │      [Not Found] ──────────┤
  │                            │
  └─ 4. Error: CLI not found    │
         Show dialog:           │
         "Whisper-CLI nicht gefunden.
          Bitte installieren Sie es oder
          geben Sie den Pfad in den
          Einstellungen an."
         Launch settings? [Yes] [Cancel]
```

### Implementation

```csharp
public class CLIPathResolver
{
    private readonly ILogger _logger;

    public string ResolveWhisperCLIPath(AppConfig config)
    {
        // 1. Try config path
        var configPath = config.Paths.WhisperCliPath;
        if (!string.IsNullOrEmpty(configPath) && IsValidExecutable(configPath))
        {
            _logger.LogInformation("Whisper CLI found in config: {Path}", configPath);
            return configPath;
        }

        // 2. Try default location
        var defaultPath = Path.Combine(config.Paths.DataRoot, "bin", "whisper-cli.exe");
        if (IsValidExecutable(defaultPath))
        {
            _logger.LogInformation("Whisper CLI found at default location: {Path}", defaultPath);
            return defaultPath;
        }

        // 3. Search PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(dir, "whisper-cli.exe");
            if (IsValidExecutable(candidate))
            {
                _logger.LogInformation("Whisper CLI found in PATH: {Path}", candidate);
                return candidate;
            }
        }

        // 4. Not found
        _logger.LogError("Whisper CLI not found. Searched: config={ConfigPath}, default={DefaultPath}, PATH={PathEnv}",
            configPath, defaultPath, pathEnv);
        throw new FileNotFoundException("Whisper CLI executable not found");
    }

    private bool IsValidExecutable(string path)
    {
        return File.Exists(path) &&
               (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 5. Model Management

### Model Source & Download

**Default Model:**
- Name: `ggml-small.bin` (or `.gguf` depending on whisper.cpp version)
- Size: ~466 MB
- Languages: Multilingual (German, English, etc.)
- Source: **Hugging Face** (recommended)

**Download URL:**
```
https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin
```

**SHA-256 Hashes (Known-Good):**

```csharp
public static class ModelHashes
{
    public static readonly Dictionary<string, string> KnownModels = new()
    {
        // Note: These are example hashes; verify actual hashes from official source
        ["ggml-small.bin"] = "a1b2c3d4e5f6...abc123", // 64-char hex
        ["ggml-small-de.gguf"] = "f6e5d4c3b2a1...321cba", // 64-char hex
        ["ggml-base.bin"] = "123abc456def...789ghi",
        ["ggml-medium.bin"] = "789ghi012jkl...345mno"
    };
}
```

**Hash Verification:**

```csharp
public class ModelManager
{
    public async Task<bool> VerifyModelHash(string modelPath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(modelPath);

        var hashBytes = await sha256.ComputeHashAsync(stream);
        var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        var modelName = Path.GetFileName(modelPath);
        if (ModelHashes.KnownModels.TryGetValue(modelName, out var expectedHash))
        {
            var isValid = actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                _logger.LogInformation("Model hash verified: {Model}", modelName);
            }
            else
            {
                _logger.LogWarning("Model hash mismatch: {Model}, Expected={Expected}, Actual={Actual}",
                    modelName, expectedHash, actualHash);
            }

            return isValid;
        }
        else
        {
            _logger.LogWarning("Unknown model: {Model}, cannot verify hash", modelName);
            return false; // Fail-safe: reject unknown models
        }
    }
}
```

### Model Download Implementation (Wizard Step 2)

```csharp
public class ModelDownloader
{
    private readonly HttpClient _httpClient;
    private readonly IProgress<double> _progress;

    public async Task<string> DownloadModelAsync(string modelName, string targetPath, CancellationToken ct)
    {
        var url = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/{modelName}";

        _logger.LogInformation("Downloading model from {Url}", url);

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
            downloadedBytes += bytesRead;

            if (totalBytes > 0)
            {
                var progressPercentage = (double)downloadedBytes / totalBytes * 100;
                _progress?.Report(progressPercentage);
            }
        }

        _logger.LogInformation("Model downloaded: {Path}, Size={Size} MB", targetPath, downloadedBytes / 1024.0 / 1024.0);

        return targetPath;
    }
}
```

---

## 6. Timeout & Cancellation Strategy

### STT Timeout (60 seconds)

```csharp
public async Task<STTResult> TranscribeAsync(string wavPath)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

    try
    {
        return await TranscribeInternalAsync(wavPath, cts.Token);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("STT timeout after 60 seconds: {Path}", wavPath);
        throw new STTTimeoutException("Transcription exceeded 60 seconds");
    }
}
```

### Post-Processing Timeout (10 seconds)

```csharp
public async Task<string> ProcessAsync(string text)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

    try
    {
        return await ProcessInternalAsync(text, cts.Token);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Post-processing timeout, using original text");
        return text; // Fallback to original
    }
}
```

---

## 7. Temporary File Cleanup

### Cleanup Strategy

**When:**
- Immediately after successful processing (WAV → STT → Cleanup)
- On app startup (delete orphaned files from crashes)

**How:**

```csharp
public class TempFileCleanupService
{
    private readonly string _tmpPath;

    public void CleanupAfterProcessing(string wavPath, string jsonPath)
    {
        try
        {
            if (File.Exists(wavPath))
                File.Delete(wavPath);
            if (File.Exists(jsonPath))
                File.Delete(jsonPath);

            _logger.LogInformation("Cleaned up temp files: {Wav}, {Json}", wavPath, jsonPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup temp files");
        }
    }

    public void CleanupOrphanedFiles()
    {
        var cutoff = DateTime.Now.AddHours(-24);

        foreach (var file in Directory.GetFiles(_tmpPath, "*.wav").Concat(Directory.GetFiles(_tmpPath, "*.json")))
        {
            try
            {
                if (File.GetCreationTime(file) < cutoff)
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted orphaned temp file: {File}", file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete orphaned file: {File}", file);
            }
        }
    }
}
```

---

## Summary of Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **State Machine** | 3 states (Idle, Recording, Processing); no Error state | Errors return to Idle with dialog; simpler |
| **Threading** | UI thread for UI/clipboard; background for I/O/CLI | Keeps UI responsive |
| **Config Error Recovery** | Load defaults + warn | Fail-safe; user can still use app |
| **CLI Discovery** | Config → Default → PATH → Error | Predictable, extensible |
| **Model Source** | Hugging Face with SHA-256 | Official, verifiable |
| **Setting Changes** | Most require restart; some immediate | Hotkey/paths need restart for safety |
| **Timeouts** | 60s STT, 10s post-processing | Prevent hangs; generous but not infinite |
| **Temp Cleanup** | Immediate + 24h orphan cleanup | Prevents disk bloat |

---

**Last Updated:** 2025-11-17
**Approved By:** Solution Architect (AI Agent)
**Status:** Ready for Implementation
