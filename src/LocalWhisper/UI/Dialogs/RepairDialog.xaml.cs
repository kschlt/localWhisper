using System;
using System.Windows;
using LocalWhisper.Core;
using LocalWhisper.Services;
using Ookii.Dialogs.Wpf;

namespace LocalWhisper.UI.Dialogs;

/// <summary>
/// Repair dialog shown when data root is invalid on startup.
/// </summary>
/// <remarks>
/// Implements US-043: Repair Flow (Data Root Missing)
/// - Shows when data root doesn't exist or is invalid
/// - Allows user to re-link to moved folder
/// - Allows user to run wizard again (fresh setup)
/// - Allows user to exit app
///
/// See: docs/iterations/iteration-05b-download-repair.md (Task 4)
/// </remarks>
public partial class RepairDialog : Window
{
    private readonly string _currentDataRoot;
    private readonly ValidationResult _validationResult;

    public string? NewDataRoot { get; private set; }
    public bool ShouldRunWizard { get; private set; }

    public RepairDialog(string currentDataRoot, ValidationResult validationResult)
    {
        InitializeComponent();

        _currentDataRoot = currentDataRoot;
        _validationResult = validationResult;

        // Set message
        MessageText.Text = $"Der konfigurierte Datenordner wurde nicht gefunden oder ist ungültig:";
        PathText.Text = currentDataRoot;

        // Show error details if available
        if (validationResult.Errors.Count > 0)
        {
            ErrorDetailsText.Visibility = Visibility.Visible;
            ErrorDetailsText.Text = "Fehler:\n" + string.Join("\n", validationResult.Errors);
        }
    }

    private void ChooseNewFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Wählen Sie den verschobenen Datenordner:",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            var selectedPath = dialog.SelectedPath;

            // Validate folder structure
            var validator = new DataRootValidator();

            // Create a minimal config to check folder structure
            // (we don't have the real config since data root is invalid)
            var tempConfig = new Models.AppConfig();

            // Check if folder has basic structure
            var folderExists = System.IO.Directory.Exists(System.IO.Path.Combine(selectedPath, "config")) &&
                               System.IO.Directory.Exists(System.IO.Path.Combine(selectedPath, "models"));

            if (!folderExists)
            {
                MessageBox.Show(
                    "Dieser Ordner enthält keine gültige LocalWhisper-Installation.\n\n" +
                    "Bitte wählen Sie den Ordner, der die Unterordner 'config' und 'models' enthält.",
                    "Ungültiger Ordner",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Update config with new path (caller will handle this)
            NewDataRoot = selectedPath;
            DialogResult = true;

            AppLogger.LogInformation("User relinked data root", new
            {
                OldPath = _currentDataRoot,
                NewPath = selectedPath
            });

            Close();
        }
    }

    private void RunWizard_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Möchten Sie die Einrichtung wirklich neu starten?\n\n" +
            "Die alte Konfiguration wird nicht gelöscht, ist aber nicht mehr aktiv.",
            "Neu einrichten?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ShouldRunWizard = true;
            DialogResult = false; // false = didn't relink, but should run wizard

            AppLogger.LogInformation("User chose to run wizard from repair dialog");

            Close();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        AppLogger.LogInformation("User chose to exit from repair dialog");

        DialogResult = false;
        ShouldRunWizard = false;
        Close();
    }
}
