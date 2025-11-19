using System;
using System.Windows;
using System.Windows.Controls;

namespace LocalWhisper.UI.Wizard;

/// <summary>
/// Wizard Step 3: Post-Processing Configuration
/// </summary>
/// <remarks>
/// Implements US-064: Wizard - Post-Processing Setup
/// - Checkbox to enable/disable post-processing (default: enabled)
/// - Explanation of what post-processing does
/// - Information about required downloads
///
/// See: docs/iterations/iteration-07-post-processing-DECISIONS.md
/// </remarks>
public partial class PostProcessingStep : UserControl
{
    public event EventHandler? EnabledChanged;

    /// <summary>
    /// Whether post-processing is enabled.
    /// </summary>
    public bool IsPostProcessingEnabled => EnableCheckBox.IsChecked == true;

    public PostProcessingStep()
    {
        InitializeComponent();
    }

    private void EnableCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Update download info visibility based on checkbox state
        if (DownloadInfoPanel != null)
        {
            DownloadInfoPanel.Visibility = IsPostProcessingEnabled
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Notify parent window
        EnabledChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Validate step (always valid - post-processing is optional).
    /// </summary>
    public bool IsValid()
    {
        return true; // Always valid - user can choose to enable or disable
    }
}
