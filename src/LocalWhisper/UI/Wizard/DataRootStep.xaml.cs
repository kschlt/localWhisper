using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace LocalWhisper.UI.Wizard;

/// <summary>
/// Wizard Step 1: Data Root Selection
/// </summary>
/// <remarks>
/// Implements US-040: Wizard Step 1 - Data Root Selection
/// - Default path: %LOCALAPPDATA%\LocalWhisper\
/// - Browse button using Ookii.Dialogs.Wpf
/// - Validates write access
///
/// See: docs/iterations/iteration-05a-wizard-core.md (Task 5)
/// </remarks>
public partial class DataRootStep : UserControl
{
    private string _dataRoot;

    public event EventHandler? DataRootChanged;

    public DataRootStep()
    {
        InitializeComponent();

        // Set default data root
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _dataRoot = Path.Combine(appDataPath, "LocalWhisper");

        DataRootTextBox.Text = _dataRoot;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Wählen Sie den Datenordner:",
            UseDescriptionForTitle = true,
            SelectedPath = _dataRoot,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            _dataRoot = dialog.SelectedPath;
            DataRootTextBox.Text = _dataRoot;
            InfoText.Text = "Benutzerdefinierter Ordner ausgewählt.";

            DataRootChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string GetDataRoot() => _dataRoot;

    public bool IsValid() => !string.IsNullOrWhiteSpace(_dataRoot);
}
