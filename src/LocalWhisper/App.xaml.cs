using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using LocalWhisper.Adapters;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Services;
using LocalWhisper.UI.Dialogs;
using LocalWhisper.UI.Flyout;
using LocalWhisper.UI.TrayIcon;
using LocalWhisper.Utils;

namespace LocalWhisper;

/// <summary>
/// Main application entry point.
/// </summary>
public partial class App : Application
{
    private string? _dataRoot;
    private StateMachine? _stateMachine;
    private HotkeyManager? _hotkeyManager;
    private TrayIconManager? _trayIconManager;
    private AppConfig? _config;
    private AudioRecorder? _audioRecorder;
    private WhisperCLIAdapter? _whisperAdapter;
    private ClipboardWriter? _clipboardWriter;
    private HistoryWriter? _historyWriter;
    private readonly SemaphoreSlim _recordingSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Application startup handler.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            // 1. Initialize data root
            _dataRoot = PathHelpers.GetDataRoot();
            PathHelpers.EnsureDataRootExists(_dataRoot);

            // 2. Initialize logging
            AppLogger.Initialize(_dataRoot);
            AppLogger.LogInformation("Application started", new
            {
                Version = "0.1.0",
                OS = Environment.OSVersion.ToString(),
                DataRoot = _dataRoot
            });

            // 3. Load configuration
            var configPath = PathHelpers.GetConfigPath(_dataRoot);
            _config = ConfigManager.Load(configPath);

            // 4. Initialize audio recorder and check microphone availability
            _audioRecorder = new AudioRecorder();
            if (!_audioRecorder.IsMicrophoneAvailable())
            {
                ShowMicrophoneUnavailableDialog();
                // Continue running (allow user to fix issue and restart)
            }

            // 4.5. Initialize Whisper CLI adapter (Iteration 3)
            _whisperAdapter = new WhisperCLIAdapter(_config.Whisper);

            // 4.6. Initialize clipboard and history writers (Iteration 4)
            _clipboardWriter = new ClipboardWriter();
            _historyWriter = new HistoryWriter();

            // 5. Initialize state machine
            _stateMachine = new StateMachine();

            // 6. Initialize tray icon (must happen before hotkey for window handle)
            _trayIconManager = new TrayIconManager(_stateMachine);

            // 7. Register hotkey
            RegisterHotkey();

            // 8. Wire hotkey events to state machine
            WireHotkeyEvents();

            AppLogger.LogInformation("Application initialization complete");
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Application startup failed", ex);
            MessageBox.Show(
                $"LocalWhisper konnte nicht gestartet werden:\n\n{ex.Message}",
                "Startfehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown(1);
        }
    }

    /// <summary>
    /// Register global hotkey.
    /// </summary>
    private void RegisterHotkey()
    {
        // Get hidden window handle from tray icon manager
        var windowHandle = _trayIconManager!.GetWindowHandle();

        _hotkeyManager = new HotkeyManager();
        bool success = _hotkeyManager.RegisterHotkey(windowHandle, _config!.Hotkey);

        if (!success)
        {
            // Hotkey conflict detected (US-003)
            ShowHotkeyConflictDialog();
        }
    }

    /// <summary>
    /// Show hotkey conflict error dialog (US-003).
    /// </summary>
    private void ShowHotkeyConflictDialog()
    {
        var dialog = new ErrorDialog(
            title: "Hotkey nicht verfügbar",
            message: "Hotkey bereits belegt. Bitte wählen Sie eine andere Kombination in den Einstellungen.",
            iconType: ErrorIconType.Warning
        );

        dialog.ShowDialog();
    }

    /// <summary>
    /// Show microphone unavailable error dialog (US-012).
    /// </summary>
    private void ShowMicrophoneUnavailableDialog()
    {
        var dialog = new ErrorDialog(
            title: "Kein Mikrofon gefunden",
            message: "Bitte schließen Sie ein Mikrofon an oder prüfen Sie die Windows-Audioeinstellungen.",
            iconType: ErrorIconType.Error
        );

        AppLogger.LogWarning("Microphone unavailable at startup");
        dialog.ShowDialog();
    }

    /// <summary>
    /// Wire hotkey events to state machine (US-001, US-010).
    /// </summary>
    private void WireHotkeyEvents()
    {
        // Use synchronous handler that fires and forgets async work
        _hotkeyManager!.HotkeyPressed += (s, e) =>
        {
            // Fire and forget with proper error handling
            _ = HandleHotkeyPressAsync();
        };
    }

    /// <summary>
    /// Handle hotkey press asynchronously with proper error handling.
    /// </summary>
    private async Task HandleHotkeyPressAsync()
    {
        // Prevent concurrent recordings
        if (!_recordingSemaphore.Wait(0))
        {
            AppLogger.LogWarning("Recording already in progress - ignoring hotkey");
            return;
        }

        try
        {
            if (_stateMachine!.State != AppState.Idle)
            {
                return; // Ignore hotkey if not idle
            }

            // Check microphone availability (US-012)
            if (!_audioRecorder!.IsMicrophoneAvailable())
            {
                ShowMicrophoneUnavailableDialog();
                return;
            }

            // Start recording: Idle -> Recording (US-010)
            _stateMachine.TransitionTo(AppState.Recording);

            var tmpPath = PathHelpers.GetTmpPath(_dataRoot!);
            _audioRecorder.StartRecording(tmpPath);

            // For Iteration 2: Record for fixed duration (500ms)
            // TODO(Iter-3): Implement proper hold-to-talk with key-up detection
            await Task.Delay(500);

            // Stop recording: Recording -> Processing
            _stateMachine.TransitionTo(AppState.Processing);
            var wavFilePath = await _audioRecorder.StopRecordingAsync();

            // Validate WAV file (US-011)
            if (!WavValidator.ValidateWavFile(wavFilePath, out var errorMessage))
            {
                AppLogger.LogWarning("WAV file validation failed", new { Error = errorMessage });
                WavValidator.MoveToFailedDirectory(wavFilePath);

                // Return to idle
                _stateMachine.TransitionTo(AppState.Idle);
                return;
            }

            // Transcribe with Whisper CLI (US-020)
            try
            {
                var sttResult = await _whisperAdapter!.TranscribeAsync(wavFilePath);

                if (sttResult.IsEmpty)
                {
                    AppLogger.LogInformation("No speech detected in recording");
                    FlyoutWindow.Show("Keine Sprache erkannt", FlyoutWindow.FlyoutType.Warning);
                    _stateMachine.TransitionTo(AppState.Idle);
                    return;
                }

                // Start E2E latency measurement (US-033)
                var e2eStopwatch = Stopwatch.StartNew();

                // 1. Write to clipboard (US-030)
                var clipboardSuccess = false;
                try
                {
                    var clipboardStopwatch = Stopwatch.StartNew();
                    await _clipboardWriter!.WriteAsync(sttResult.Text);
                    clipboardStopwatch.Stop();
                    clipboardSuccess = true;

                    AppLogger.LogInformation("Clipboard write succeeded", new
                    {
                        TextLength = sttResult.Text.Length,
                        Clipboard_Write_Duration_Ms = clipboardStopwatch.ElapsedMilliseconds
                    });
                }
                catch (ClipboardLockedException ex)
                {
                    AppLogger.LogError("Clipboard locked - cannot write", ex, new
                    {
                        RetryCount = ex.RetryCount
                    });

                    FlyoutWindow.Show("Zwischenablage gesperrt", FlyoutWindow.FlyoutType.Warning);
                    // Continue to history write anyway
                }

                // 2. Write to history (US-031)
                try
                {
                    var historyStopwatch = Stopwatch.StartNew();
                    var historyEntry = new HistoryEntry
                    {
                        Created = DateTimeOffset.Now,
                        Text = sttResult.Text,
                        Language = sttResult.Language,
                        SttModel = Path.GetFileName(_config!.Whisper.ModelPath),
                        DurationSeconds = sttResult.DurationSeconds,
                        PostProcessed = false
                    };

                    var historyPath = await _historyWriter!.WriteAsync(historyEntry, _dataRoot!);
                    historyStopwatch.Stop();

                    AppLogger.LogInformation("History file created", new
                    {
                        Path = historyPath,
                        History_Write_Duration_Ms = historyStopwatch.ElapsedMilliseconds
                    });
                }
                catch (Exception ex)
                {
                    AppLogger.LogWarning("History write failed - continuing anyway", new
                    {
                        Error = ex.Message
                    });
                    // Do not block on history failure
                }

                // 3. Show flyout notification (US-032)
                e2eStopwatch.Stop();
                AppLogger.LogInformation("E2E dictation completed", new
                {
                    E2E_Latency_Ms = e2eStopwatch.ElapsedMilliseconds,
                    TranscriptLength = sttResult.Text.Length,
                    ClipboardSuccess = clipboardSuccess
                });

                var flyoutStopwatch = Stopwatch.StartNew();
                if (clipboardSuccess)
                {
                    FlyoutWindow.Show("Transkript im Clipboard", FlyoutWindow.FlyoutType.Success);
                }
                else
                {
                    FlyoutWindow.Show("Transkript gespeichert (Clipboard fehlgeschlagen)", FlyoutWindow.FlyoutType.Warning);
                }
                flyoutStopwatch.Stop();

                AppLogger.LogInformation("Flyout displayed", new
                {
                    Flyout_Display_Latency_Ms = flyoutStopwatch.ElapsedMilliseconds
                });

                // 4. Return to idle
                _stateMachine.TransitionTo(AppState.Idle);
            }
            catch (ModelNotFoundException ex)
            {
                AppLogger.LogError("Whisper model not found", ex);
                _stateMachine.TransitionTo(AppState.Idle);

                Dispatcher.Invoke(() =>
                {
                    var errorDialog = new ErrorDialog(
                        title: "Whisper-Modell nicht gefunden",
                        message: $"Das konfigurierte Whisper-Modell wurde nicht gefunden.\\n\\nBitte prüfen Sie die Einstellungen.\\n\\nDetails: {ex.Message}",
                        iconType: ErrorIconType.Error
                    );
                    errorDialog.ShowDialog();
                });
            }
            catch (STTTimeoutException ex)
            {
                AppLogger.LogError("STT timeout", ex);
                _stateMachine.TransitionTo(AppState.Idle);

                Dispatcher.Invoke(() =>
                {
                    var errorDialog = new ErrorDialog(
                        title: "Transkription zu langsam",
                        message: $"Die Spracherkennung hat zu lange gedauert und wurde abgebrochen.\\n\\nDetails: {ex.Message}",
                        iconType: ErrorIconType.Warning
                    );
                    errorDialog.ShowDialog();
                });
            }
            catch (InvalidAudioException ex)
            {
                AppLogger.LogError("Invalid audio for STT", ex);
                _stateMachine.TransitionTo(AppState.Idle);

                Dispatcher.Invoke(() =>
                {
                    var errorDialog = new ErrorDialog(
                        title: "Ungültige Audiodatei",
                        message: $"Die Audiodatei konnte nicht verarbeitet werden.\\n\\nDetails: {ex.Message}",
                        iconType: ErrorIconType.Error
                    );
                    errorDialog.ShowDialog();
                });
            }
            catch (STTException ex)
            {
                AppLogger.LogError("STT error", ex);
                _stateMachine.TransitionTo(AppState.Idle);

                Dispatcher.Invoke(() =>
                {
                    var errorDialog = new ErrorDialog(
                        title: "Spracherkennungsfehler",
                        message: $"Fehler bei der Spracherkennung:\\n\\n{ex.Message}",
                        iconType: ErrorIconType.Error
                    );
                    errorDialog.ShowDialog();
                });
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Error during recording/processing", ex);

            // Ensure we return to idle state
            if (_stateMachine!.State != AppState.Idle)
            {
                _stateMachine.TransitionTo(AppState.Idle);
            }

            // Show error dialog
            Dispatcher.Invoke(() =>
            {
                var errorDialog = new ErrorDialog(
                    title: "Aufnahmefehler",
                    message: $"Fehler während der Audioaufnahme:\n\n{ex.Message}",
                    iconType: ErrorIconType.Error
                );
                errorDialog.ShowDialog();
            });
        }
        finally
        {
            _recordingSemaphore.Release();
        }
    }

    /// <summary>
    /// Application exit handler.
    /// </summary>
    private void OnExit(object sender, ExitEventArgs e)
    {
        AppLogger.LogInformation("Application exiting");

        // Cleanup resources
        _recordingSemaphore?.Dispose();
        _audioRecorder?.Dispose();
        _hotkeyManager?.Dispose();
        _trayIconManager?.Dispose();

        AppLogger.Shutdown();
    }
}
