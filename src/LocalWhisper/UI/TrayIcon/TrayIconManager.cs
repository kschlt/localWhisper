using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.UI.Settings;
using LocalWhisper.Utils;

namespace LocalWhisper.UI.TrayIcon;

/// <summary>
/// Manages system tray icon and context menu.
/// </summary>
/// <remarks>
/// See: US-002 (Tray Icon Shows Status)
/// </remarks>
public class TrayIconManager : IDisposable
{
    private readonly TaskbarIcon _trayIcon;
    private readonly StateMachine _stateMachine;
    private readonly Window _hiddenWindow;
    private readonly string _configPath;
    private readonly string _dataRoot;
    private bool _disposed;
    private SettingsWindow? _settingsWindow;

    // =============================================================================
    // EVENTS (for testing)
    // =============================================================================

    /// <summary>Event fired when Settings window is opened (for testing)</summary>
    internal event Action<SettingsWindow>? OnSettingsOpened;

    /// <summary>Event fired when Explorer is opened to a path (for testing)</summary>
    internal event Action<string>? OnExplorerOpened;

    /// <summary>Event fired when application exit is requested (for testing)</summary>
    internal event Action? OnExitRequested;

    public TrayIconManager(StateMachine stateMachine, string configPath, string dataRoot)
    {
        _stateMachine = stateMachine;
        _configPath = configPath;
        _dataRoot = dataRoot;

        // Create hidden window for Win32 message handling
        _hiddenWindow = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            Visibility = Visibility.Hidden
        };
        _hiddenWindow.Show();

        // Create tray icon
        _trayIcon = new TaskbarIcon
        {
            Icon = CreateIcon(AppState.Idle),
            ToolTipText = IconResources.GetStateTooltip(AppState.Idle, "de"),
            ContextMenu = CreateContextMenu()
        };

        // Subscribe to state changes
        _stateMachine.StateChanged += OnStateChanged;

        AppLogger.LogInformation("Tray icon initialized");
    }

    /// <summary>
    /// Get window handle for Win32 message handling.
    /// </summary>
    public IntPtr GetWindowHandle()
    {
        var helper = new WindowInteropHelper(_hiddenWindow);
        return helper.Handle;
    }

    /// <summary>
    /// Handle state change events (update icon/tooltip).
    /// </summary>
    private void OnStateChanged(object? sender, StateChangedEventArgs e)
    {
        // Update icon
        _trayIcon.Icon = CreateIcon(e.NewState);

        // Update tooltip
        _trayIcon.ToolTipText = IconResources.GetStateTooltip(e.NewState, "de");

        AppLogger.LogDebug("Tray icon updated", new { State = e.NewState.ToString() });
    }

    /// <summary>
    /// Create icon from state.
    /// </summary>
    private System.Drawing.Icon CreateIcon(AppState state)
    {
        // Ensure we're on the UI thread (WPF objects require STA thread)
        if (!_hiddenWindow.Dispatcher.CheckAccess())
        {
            return _hiddenWindow.Dispatcher.Invoke(() => CreateIcon(state));
        }

        var iconGlyph = IconResources.GetStateIcon(state);
        var color = IconResources.GetStateColor(state);

        // Freeze brush to make it thread-safe for cross-thread access
        if (color.CanFreeze && !color.IsFrozen)
        {
            color.Freeze();
        }

        // Create bitmap with icon glyph
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var text = new FormattedText(
                iconGlyph,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe MDL2 Assets"),
                32, // Font size
                color,
                VisualTreeHelper.GetDpi(visual).PixelsPerDip
            );

            context.DrawText(text, new Point(0, 0));
        }

        var bitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        // Convert to System.Drawing.Icon
        using var stream = new System.IO.MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
        stream.Seek(0, System.IO.SeekOrigin.Begin);

        using var bmp = new System.Drawing.Bitmap(stream);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    /// <summary>
    /// Create context menu.
    /// </summary>
    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        // Settings menu item
        var settingsItem = new MenuItem
        {
            Header = "Einstellungen"
        };
        settingsItem.Click += (s, e) => OpenSettings();
        menu.Items.Add(settingsItem);

        // History menu item
        var historyItem = new MenuItem
        {
            Header = "History"
        };
        historyItem.Click += (s, e) => OpenHistoryFolder();
        menu.Items.Add(historyItem);

        // Exit menu item
        var exitItem = new MenuItem
        {
            Header = "Beenden"
        };
        exitItem.Click += (s, e) => Exit();
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// Get context menu (for testing).
    /// </summary>
    internal ContextMenu GetContextMenu()
    {
        return CreateContextMenu();
    }

    /// <summary>
    /// Set data root path (for testing).
    /// </summary>
    internal void SetDataRoot(string? dataRoot)
    {
        // This would require refactoring the class to make _dataRoot mutable
        // For now, this is a placeholder for testing
        // Real implementation would need to update the field and possibly recreate the menu
    }

    /// <summary>
    /// Exit application (for testing).
    /// </summary>
    internal void Exit()
    {
        OnExitRequested?.Invoke();
        AppLogger.LogInformation("User requested application shutdown (via tray menu)");
        Application.Current.Shutdown(0);
    }

    /// <summary>
    /// Open Settings window.
    /// </summary>
    private void OpenSettings()
    {
        OpenSettings(null, null);
    }

    /// <summary>
    /// Open Settings window (for testing with custom config).
    /// </summary>
    internal SettingsWindow? OpenSettings(AppConfig? config, string? configPath)
    {
        // Use provided config/path or load from instance fields
        var actualConfig = config ?? ConfigManager.Load(_configPath);
        var actualConfigPath = configPath ?? _configPath;

        // Prevent multiple windows
        if (_settingsWindow != null && _settingsWindow.IsLoaded)
        {
            _settingsWindow.Activate();
            return _settingsWindow;
        }

        try
        {
            // Create and show Settings window
            _settingsWindow = new SettingsWindow(actualConfig, actualConfigPath);
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;

            // Trigger event for testing
            OnSettingsOpened?.Invoke(_settingsWindow);

            _settingsWindow.ShowDialog();

            AppLogger.LogInformation("Settings window opened from tray menu");
            return _settingsWindow;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to open Settings window", ex);
            MessageBox.Show(
                $"Fehler beim Öffnen der Einstellungen:\n\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return null;
        }
    }

    /// <summary>
    /// Open Windows Explorer to history folder.
    /// </summary>
    private void OpenHistoryFolder()
    {
        var historyPath = PathHelpers.GetHistoryPath(_dataRoot);
        OpenHistory(historyPath);
    }

    /// <summary>
    /// Open Windows Explorer to specified history path (for testing).
    /// </summary>
    internal void OpenHistory(string historyPath)
    {
        try
        {
            if (!Directory.Exists(historyPath))
            {
                Directory.CreateDirectory(historyPath);
                AppLogger.LogInformation("Created history folder", new { Path = historyPath });
            }

            // Trigger event for testing
            OnExplorerOpened?.Invoke(historyPath);

            Process.Start("explorer.exe", historyPath);
            AppLogger.LogInformation("Opened history folder", new { Path = historyPath });
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to open history folder", ex);
            MessageBox.Show(
                $"Fehler beim Öffnen des History-Ordners:\n\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _trayIcon?.Dispose();
        _hiddenWindow?.Close();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
