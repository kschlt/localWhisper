using System.Windows;
using System.Windows.Interop;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Services;
using LocalWhisper.UI.Dialogs;
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
        _hotkeyManager!.HotkeyPressed += async (s, e) =>
        {
            if (_stateMachine!.State != AppState.Idle)
            {
                return; // Ignore hotkey if not idle
            }

            try
            {
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
                var wavFilePath = _audioRecorder.StopRecording();

                // Validate WAV file (US-011)
                if (!WavValidator.ValidateWavFile(wavFilePath, out var errorMessage))
                {
                    AppLogger.LogWarning("WAV file validation failed", new { Error = errorMessage });
                    WavValidator.MoveToFailedDirectory(wavFilePath);

                    // Return to idle
                    _stateMachine.TransitionTo(AppState.Idle);
                    return;
                }

                // TODO(PH-002, Iter-3): Process with Whisper STT
                AppLogger.LogInformation("WAV file ready for STT processing (placeholder)", new { WavFile = wavFilePath });
                await Task.Delay(300); // Simulate STT processing

                // Complete: Processing -> Idle
                _stateMachine.TransitionTo(AppState.Idle);
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error during recording/processing", ex);

                // Ensure we return to idle state
                if (_stateMachine.State != AppState.Idle)
                {
                    _stateMachine.TransitionTo(AppState.Idle);
                }

                // Show error dialog
                var errorDialog = new ErrorDialog(
                    title: "Aufnahmefehler",
                    message: $"Fehler während der Audioaufnahme:\n\n{ex.Message}",
                    iconType: ErrorIconType.Error
                );
                errorDialog.ShowDialog();
            }
        };
    }

    /// <summary>
    /// Application exit handler.
    /// </summary>
    private void OnExit(object sender, ExitEventArgs e)
    {
        AppLogger.LogInformation("Application exiting");

        // Cleanup resources
        _audioRecorder?.Dispose();
        _hotkeyManager?.Dispose();
        _trayIconManager?.Dispose();

        AppLogger.Shutdown();
    }
}
