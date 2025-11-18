# Iteration 5b: Model Download + Repair

**Goal:** HTTP model download with progress tracking and repair flow for corrupted/missing data

**Status:** Planned (Deferred from Iteration 5a)
**Estimated Effort:** 4-6 hours
**Dependencies:** Iteration 5a complete

---

## Overview

This iteration adds **HTTP download functionality** to the wizard and implements a **repair flow** for when the data root becomes invalid (deleted, moved, corrupted).

**Deferred Rationale**: Iteration 5a delivers a fully functional wizard with manual model selection. HTTP download is a UX enhancement, not a blocker for v0.1. This split keeps Iteration 5a manageable (4-6h) and allows focused implementation of complex download logic separately.

---

## User Stories

| ID | Title | Priority | Tags |
|----|-------|----------|------|
| US-041b | Wizard Step 2 - Model Download | Medium | @FR-017 |
| US-043 | Repair Flow (Data Root Missing) | Medium | @UC-003, @FR-016 |
| US-044 | Wizard Completion Time (NFR-004) | Low | @NFR-004 |

**Total:** 3 user stories

---

## Functional Requirements Covered

| FR ID | Title | Scope |
|-------|-------|-------|
| FR-017 | Model Verification | ✅ HTTP download + hash validation |
| FR-016 | Configuration Management | ✅ Repair flow re-creates config |
| FR-021 | Error Handling | ✅ Download failures, network errors |

---

## Architecture Changes

### New Components

**1. Services/ModelDownloader.cs**
- Downloads model files from HuggingFace via HTTP
- Reports progress (bytes downloaded, percentage, ETA)
- Supports cancellation via CancellationToken
- Retry logic (3 attempts with exponential backoff)
- SHA-1 validation after download

**2. UI/Wizard/DownloadProgressDialog.xaml + .cs**
- Progress bar (0-100%)
- Download speed (MB/s)
- ETA (estimated time remaining)
- Bytes downloaded / total size
- Cancel button
- Shows errors if download fails

**3. UI/Dialogs/RepairDialog.xaml + .cs**
- Shown when data root is invalid on startup
- Options:
  - "Neuen Ordner wählen" (re-link to moved folder)
  - "Neu einrichten" (run wizard again)
- Validates folder structure if user re-links

**4. Services/DataRootValidator.cs**
- Checks if data root exists
- Validates folder structure (config/, models/, history/, logs/, tmp/)
- Checks if required files exist (config.toml, model file)
- Returns validation result with detailed errors

### Modified Components

**UI/Wizard/ModelSelectionStep.xaml:**
- Add "Modell herunterladen" button
- Show download options (language, size selection)
- Hide file picker when download mode selected

**App.xaml.cs:**
- On startup, validate data root with DataRootValidator
- If invalid → show RepairDialog before initializing app

---

## Implementation Tasks

### Task 1: ModelDownloader Service

**File:** `src/LocalWhisper/Services/ModelDownloader.cs`

```csharp
public class ModelDownloader
{
    private readonly HttpClient _httpClient;
    private const int MaxRetries = 3;
    private const int InitialRetryDelayMs = 1000;

    public async Task<string> DownloadAsync(
        ModelDefinition model,
        string destinationPath,
        IProgress<DownloadProgress> progress,
        CancellationToken ct)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < MaxRetries)
        {
            try
            {
                AppLogger.LogInformation($"Download attempt {attempt + 1}/{MaxRetries}", new
                {
                    Model = model.Name,
                    URL = model.DownloadURL
                });

                await DownloadFileAsync(model.DownloadURL, destinationPath, progress, ct);

                // Validate SHA-1
                var validator = new ModelValidator();
                var isValid = await validator.ValidateAsync(destinationPath, model.SHA1);

                if (!isValid)
                {
                    throw new ModelDownloadException("SHA-1 hash mismatch after download");
                }

                AppLogger.LogInformation("Download successful", new
                {
                    Model = model.Name,
                    FilePath = destinationPath
                });

                return destinationPath;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                attempt++;

                if (attempt < MaxRetries)
                {
                    var delay = InitialRetryDelayMs * (int)Math.Pow(2, attempt - 1);
                    AppLogger.LogWarning($"Download failed - retrying in {delay}ms", new
                    {
                        Attempt = attempt,
                        Error = ex.Message
                    });

                    await Task.Delay(delay, ct);
                }
            }
        }

        throw new ModelDownloadException($"Download failed after {MaxRetries} attempts", lastException);
    }

    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgress> progress,
        CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;
        var startTime = DateTime.Now;

        using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
            downloadedBytes += bytesRead;

            // Report progress
            var elapsed = DateTime.Now - startTime;
            var bytesPerSecond = elapsed.TotalSeconds > 0 ? downloadedBytes / elapsed.TotalSeconds : 0;
            var eta = bytesPerSecond > 0 ? TimeSpan.FromSeconds((totalBytes - downloadedBytes) / bytesPerSecond) : TimeSpan.Zero;

            progress?.Report(new DownloadProgress
            {
                BytesDownloaded = downloadedBytes,
                TotalBytes = totalBytes,
                Percentage = totalBytes > 0 ? (int)((downloadedBytes * 100) / totalBytes) : 0,
                BytesPerSecond = bytesPerSecond,
                EstimatedTimeRemaining = eta
            });
        }
    }
}

public class DownloadProgress
{
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public int Percentage { get; set; }
    public double BytesPerSecond { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
}
```

### Task 2: Download Progress Dialog

**File:** `src/LocalWhisper/UI/Wizard/DownloadProgressDialog.xaml`

**UI Elements**:
- ProgressBar (IsIndeterminate=false, Value=0-100)
- TextBlock: "Lädt {model.Name} herunter..."
- TextBlock: "{downloadedMB} MB / {totalMB} MB"
- TextBlock: "Geschwindigkeit: {speed} MB/s"
- TextBlock: "Verbleibend: {eta}"
- Button: "Abbrechen"

**Code-behind**:
```csharp
public partial class DownloadProgressDialog : Window
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ModelDownloader _downloader = new();

    public async Task<string> DownloadModelAsync(ModelDefinition model, string destinationPath)
    {
        var progress = new Progress<DownloadProgress>(p =>
        {
            ProgressBar.Value = p.Percentage;
            DownloadedText.Text = $"{p.BytesDownloaded / 1024 / 1024} MB / {p.TotalBytes / 1024 / 1024} MB";
            SpeedText.Text = $"Geschwindigkeit: {p.BytesPerSecond / 1024 / 1024:F2} MB/s";
            ETAText.Text = $"Verbleibend: {p.EstimatedTimeRemaining:mm\\:ss}";
        });

        try
        {
            var filePath = await _downloader.DownloadAsync(model, destinationPath, progress, _cts.Token);
            DialogResult = true;
            return filePath;
        }
        catch (OperationCanceledException)
        {
            AppLogger.LogInformation("Download cancelled by user");
            DialogResult = false;
            throw;
        }
        catch (ModelDownloadException ex)
        {
            AppLogger.LogError("Download failed", ex);
            MessageBox.Show($"Download fehlgeschlagen:\n\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
            throw;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
    }
}
```

### Task 3: DataRootValidator

**File:** `src/LocalWhisper/Services/DataRootValidator.cs`

```csharp
public class DataRootValidator
{
    public ValidationResult Validate(string dataRoot, AppConfig config)
    {
        var result = new ValidationResult();

        // Check existence
        if (!Directory.Exists(dataRoot))
        {
            result.IsValid = false;
            result.Errors.Add($"Data root does not exist: {dataRoot}");
            return result;
        }

        // Check folder structure
        var requiredFolders = new[] { "config", "models", "history", "logs", "tmp" };
        foreach (var folder in requiredFolders)
        {
            var path = Path.Combine(dataRoot, folder);
            if (!Directory.Exists(path))
            {
                result.Warnings.Add($"Missing folder: {folder}");
            }
        }

        // Check config.toml
        var configPath = PathHelpers.GetConfigPath(dataRoot);
        if (!File.Exists(configPath))
        {
            result.IsValid = false;
            result.Errors.Add("config.toml not found");
        }

        // Check model file
        var modelPath = config.Whisper.ModelPath;
        if (!File.Exists(modelPath))
        {
            result.IsValid = false;
            result.Errors.Add($"Model file not found: {modelPath}");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}
```

### Task 4: Repair Dialog

**File:** `src/LocalWhisper/UI/Dialogs/RepairDialog.xaml`

**UI**:
- Icon: Warning triangle
- Title: "Datenordner nicht gefunden"
- Message: "Der konfigurierte Datenordner wurde nicht gefunden:\n{dataRoot}\n\nMögliche Ursachen:\n- Ordner wurde verschoben oder gelöscht\n- Externe Festplatte nicht verbunden"
- Buttons:
  - "Neuen Ordner wählen" (re-link)
  - "Neu einrichten" (run wizard)
  - "Beenden" (exit app)

**Logic**:
```csharp
private void ChooseNewFolder_Click(object sender, RoutedEventArgs e)
{
    var dialog = new VistaFolderBrowserDialog
    {
        Description = "Wählen Sie den verschobenen Datenordner:",
        UseDescriptionForTitle = true
    };

    if (dialog.ShowDialog() == true)
    {
        var validator = new DataRootValidator();
        var result = validator.ValidateFolderStructure(dialog.SelectedPath);

        if (result.IsValid)
        {
            // Update config with new path
            UpdateDataRootInConfig(dialog.SelectedPath);
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Dieser Ordner enthält keine gültige LocalWhisper-Installation.", "Fehler");
        }
    }
}

private void RunWizard_Click(object sender, RoutedEventArgs e)
{
    // Close repair dialog, main app will launch wizard
    DialogResult = false;
    ShouldRunWizard = true;
}
```

### Task 5: Integration in App Startup

**File:** `src/LocalWhisper/App.xaml.cs` (modify)

```csharp
private void OnStartup(object sender, StartupEventArgs e)
{
    // ... determine data root ...

    if (!File.Exists(configPath))
    {
        // No config → run wizard (from Iteration 5a)
        RunWizard();
    }
    else
    {
        // Config exists → validate data root
        _config = ConfigManager.Load(configPath);
        var validator = new DataRootValidator();
        var validationResult = validator.Validate(_dataRoot, _config);

        if (!validationResult.IsValid)
        {
            AppLogger.LogWarning("Data root validation failed", new
            {
                Errors = validationResult.Errors
            });

            // Show repair dialog
            var repairDialog = new RepairDialog(_dataRoot, validationResult);
            var result = repairDialog.ShowDialog();

            if (result == true)
            {
                // User re-linked folder → reload config
                _dataRoot = repairDialog.NewDataRoot;
                _config = ConfigManager.Load(PathHelpers.GetConfigPath(_dataRoot));
            }
            else if (repairDialog.ShouldRunWizard)
            {
                // User chose "Neu einrichten"
                RunWizard();
            }
            else
            {
                // User chose "Beenden"
                Shutdown();
                return;
            }
        }
    }

    // Continue with normal initialization...
}
```

---

## Out of Scope (Future Enhancements)

- Resume partial downloads (would require Range header support)
- Parallel chunk downloads (complex, marginal benefit)
- Automatic model updates (check for newer versions)
- Migration tool for old data structures

---

## Testing Strategy

### Unit Tests

**ModelDownloader**:
- Mock HTTP responses
- Test retry logic (fail twice, succeed on 3rd attempt)
- Test SHA-1 validation after download
- Test cancellation

**DataRootValidator**:
- Test valid data root (all folders + files exist)
- Test missing config.toml
- Test missing model file
- Test missing folders (warnings)

### Integration Tests

**Download Flow**:
- Download small test file (use mock HTTP server)
- Verify progress reporting
- Verify SHA-1 validation
- Verify file written to disk

**Repair Flow**:
- Delete data root, start app → repair dialog shown
- Re-link to valid folder → app continues
- Choose "Neu einrichten" → wizard shown

### Manual Tests

**Download**:
- Download actual model (e.g., base 142 MB)
- Monitor progress bar accuracy
- Test cancel during download
- Simulate network failure (disconnect WiFi)

**Repair**:
- Move data root folder → start app → re-link
- Delete data root → start app → run wizard
- Delete only models/ → start app → show error

---

## Performance Targets

| Metric | Target | Verification |
|--------|--------|--------------|
| Download progress update | Every 100ms | Visual smoothness |
| SHA-1 computation (466 MB) | < 5 seconds | Logged timing |
| Wizard completion (download) | < 2 min (small model on 50 Mbps) | Manual test |

---

## Definition of Done

- [ ] US-041b, US-043, US-044 implemented
- [ ] HTTP download with progress tracking works
- [ ] SHA-1 validation after download
- [ ] Retry logic (3 attempts) works
- [ ] Cancel button stops download
- [ ] Repair dialog shown when data root invalid
- [ ] Re-link to moved folder works
- [ ] "Neu einrichten" launches wizard
- [ ] DataRootValidator checks folder structure
- [ ] Unit tests for ModelDownloader, DataRootValidator
- [ ] Manual download test (real HuggingFace download)
- [ ] Logging for download progress, errors
- [ ] Traceability matrix updated
- [ ] Changelog entry added

---

**Iteration Start:** TBD (after Iteration 5a)
**Target Completion:** Download + repair functional
