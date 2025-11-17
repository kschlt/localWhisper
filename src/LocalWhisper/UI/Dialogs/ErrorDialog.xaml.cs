using System.Windows;
using System.Windows.Media;
using LocalWhisper.Utils;

namespace LocalWhisper.UI.Dialogs;

/// <summary>
/// Error dialog icon types.
/// </summary>
public enum ErrorIconType
{
    Error,
    Warning,
    Info
}

/// <summary>
/// User-friendly error dialog.
/// </summary>
/// <remarks>
/// See: US-003 (Hotkey Conflict Error Dialog), FR-021 (Error Dialogs)
/// </remarks>
public partial class ErrorDialog : Window
{
    public string DialogTitle { get; }
    public string Message { get; }
    private readonly ErrorIconType _iconType;

    public ErrorDialog(string title, string message, ErrorIconType iconType = ErrorIconType.Error, bool showSettingsButton = false)
    {
        InitializeComponent();

        DialogTitle = title;
        Message = message;
        _iconType = iconType;

        DataContext = this;

        // Set icon
        SetIcon();

        // Show settings button if requested
        if (showSettingsButton)
        {
            SettingsButton.Visibility = Visibility.Visible;
        }
    }

    private void SetIcon()
    {
        switch (_iconType)
        {
            case ErrorIconType.Error:
                IconText.Text = IconResources.ErrorIcon;
                IconText.Foreground = IconResources.ErrorColor;
                break;
            case ErrorIconType.Warning:
                IconText.Text = IconResources.WarningIcon;
                IconText.Foreground = IconResources.WarningColor;
                break;
            case ErrorIconType.Info:
                IconText.Text = IconResources.InfoIcon;
                IconText.Foreground = IconResources.InfoColor;
                break;
        }
    }

    private void OnOkButtonClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        // TODO(PH-001, Iter-6): Replace placeholder with real SettingsWindow
        // See: docs/meta/placeholders-tracker.md (PH-001)
        MessageBox.Show(
            "Settings functionality coming in Iteration 6",
            "Not Yet Implemented",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }
}
