using System.Diagnostics;
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
        var iconGlyph = IconResources.GetStateIcon(state);
        var color = IconResources.GetStateColor(state);

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
        exitItem.Click += (s, e) =>
        {
            AppLogger.LogInformation("User requested application shutdown (via tray menu)");
            Application.Current.Shutdown(0);
        };
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// Open Settings window.
    /// </summary>
    private void OpenSettings()
    {
        // Prevent multiple windows
        if (_settingsWindow != null && _settingsWindow.IsLoaded)
        {
            _settingsWindow.Activate();
            return;
        }

        try
        {
            // Load current config
            var config = ConfigManager.Load(_configPath);

            // Create and show Settings window
            _settingsWindow = new SettingsWindow(config, _configPath);
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.ShowDialog();

            AppLogger.LogInformation("Settings window opened from tray menu");
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
        }
    }

    /// <summary>
    /// Open Windows Explorer to history folder.
    /// </summary>
    private void OpenHistoryFolder()
    {
        try
        {
            var historyPath = PathHelpers.GetHistoryPath(_dataRoot);

            if (!Directory.Exists(historyPath))
            {
                Directory.CreateDirectory(historyPath);
                AppLogger.LogInformation("Created history folder", new { Path = historyPath });
            }

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
