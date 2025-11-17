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

            // 4. Initialize state machine
            _stateMachine = new StateMachine();

            // 5. Initialize tray icon (must happen before hotkey for window handle)
            _trayIconManager = new TrayIconManager(_stateMachine);

            // 6. Register hotkey
            RegisterHotkey();

            // 7. Wire hotkey events to state machine
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
    /// Wire hotkey events to state machine (US-001).
    /// </summary>
    private void WireHotkeyEvents()
    {
        _hotkeyManager!.HotkeyPressed += async (s, e) =>
        {
            // For Iteration 1: Simulate full state flow
            // TODO(PH-002, Iter-3): Replace simulation with real audio/STT processing

            if (_stateMachine!.State == AppState.Idle)
            {
                // Hotkey pressed: Idle -> Recording
                _stateMachine.TransitionTo(AppState.Recording);

                // Simulate recording (in real version, hold duration determines recording length)
                await Task.Delay(100);

                // Auto-transition: Recording -> Processing
                _stateMachine.TransitionTo(AppState.Processing);

                // Simulate processing
                AppLogger.LogInformation("Simulated processing started (no audio/STT yet)");
                await Task.Delay(500); // Simulate STT processing time
                AppLogger.LogInformation("Simulated processing complete");

                // Complete: Processing -> Idle
                _stateMachine.TransitionTo(AppState.Idle);
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
        _hotkeyManager?.Dispose();
        _trayIconManager?.Dispose();

        AppLogger.Shutdown();
    }
}
