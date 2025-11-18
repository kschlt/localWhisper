using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using LocalWhisper.Core;

namespace LocalWhisper.UI.Flyout;

/// <summary>
/// Custom flyout notification window.
/// </summary>
/// <remarks>
/// Implements US-032: Custom Flyout Notification
/// - Displays near system tray (bottom-right corner)
/// - Auto-dismisses after 3 seconds
/// - Does not steal focus (ShowActivated=False)
/// - Topmost window for visibility
/// - Supports success, warning, and error types
///
/// See: docs/iterations/iteration-04-clipboard-history-flyout.md (US-032)
/// See: docs/specification/functional-requirements.md (FR-015)
/// </remarks>
public partial class FlyoutWindow : Window
{
    private const int AutoDismissSeconds = 3;
    private DispatcherTimer? _dismissTimer;

    /// <summary>
    /// Flyout notification type.
    /// </summary>
    public enum FlyoutType
    {
        Success,
        Warning,
        Error
    }

    public FlyoutWindow()
    {
        InitializeComponent();

        // Position window in bottom-right corner (near system tray)
        PositionWindow();

        // Setup auto-dismiss timer
        SetupAutoDismissTimer();
    }

    /// <summary>
    /// Show flyout notification with custom message and type.
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="type">Notification type (Success, Warning, Error)</param>
    public static void Show(string message, FlyoutType type = FlyoutType.Success)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var flyout = new FlyoutWindow();
            flyout.SetMessage(message);
            flyout.SetType(type);
            flyout.Show();

            AppLogger.LogInformation("Flyout displayed", new
            {
                Message = message,
                Type = type.ToString()
            });
        });
    }

    /// <summary>
    /// Set flyout message text.
    /// </summary>
    private void SetMessage(string message)
    {
        MessageText.Text = message;
    }

    /// <summary>
    /// Set flyout type (changes icon and color).
    /// </summary>
    private void SetType(FlyoutType type)
    {
        switch (type)
        {
            case FlyoutType.Success:
                IconBackground.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                Icon.Data = Geometry.Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"); // Checkmark
                break;

            case FlyoutType.Warning:
                IconBackground.Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                Icon.Data = Geometry.Parse("M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"); // Warning triangle
                break;

            case FlyoutType.Error:
                IconBackground.Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                Icon.Data = Geometry.Parse("M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"); // X mark
                break;
        }
    }

    /// <summary>
    /// Position window in bottom-right corner near system tray.
    /// </summary>
    private void PositionWindow()
    {
        // Get working area (screen minus taskbar)
        var workingArea = SystemParameters.WorkArea;

        // Position in bottom-right with 10px margin
        Left = workingArea.Right - Width - 10;
        Top = workingArea.Bottom - Height - 10;
    }

    /// <summary>
    /// Setup timer for auto-dismiss after 3 seconds.
    /// </summary>
    private void SetupAutoDismissTimer()
    {
        _dismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(AutoDismissSeconds)
        };

        _dismissTimer.Tick += (sender, e) =>
        {
            _dismissTimer.Stop();
            Close();

            AppLogger.LogInformation("Flyout auto-dismissed");
        };

        _dismissTimer.Start();
    }

    /// <summary>
    /// Cleanup timer on window close.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _dismissTimer?.Stop();
        _dismissTimer = null;

        base.OnClosed(e);
    }
}
