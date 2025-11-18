using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using LocalWhisper.Models;
using LocalWhisper.Services;
using Microsoft.Win32;

namespace LocalWhisper.UI.Wizard;

/// <summary>
/// Wizard Step 2: Model Selection
/// </summary>
/// <remarks>
/// Implements US-041a: Wizard Step 2 - Model Selection (File Picker)
/// - Language selection (German/English) filters available models
/// - DataGrid shows model tradeoffs (size, speed, description)
/// - User browses for .bin file downloaded from HuggingFace
/// - SHA-1 validation with progress feedback
///
/// See: docs/iterations/iteration-05a-wizard-core.md (Task 6)
/// </remarks>
public partial class ModelSelectionStep : UserControl
{
    private ModelDefinition[] _allModels;
    private ModelDefinition? _selectedModel;
    private string? _modelFilePath;
    private string _selectedLanguage = "de"; // Default: German
    private readonly ModelValidator _validator = new();

    public event EventHandler? ModelChanged;

    public ModelSelectionStep()
    {
        InitializeComponent();

        _allModels = ModelDefinition.GetAvailableModels();
        LoadModelsForLanguage(_selectedLanguage);
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;

        _selectedLanguage = selectedItem.Tag as string ?? "de";
        LoadModelsForLanguage(_selectedLanguage);

        // Reset selection when language changes
        _selectedModel = null;
        _modelFilePath = null;
        ModelFileTextBox.Text = "Keine Datei ausgewählt";
        BrowseModelButton.IsEnabled = false;
        ValidationPanel.Visibility = Visibility.Collapsed;
        ValidationResult.Visibility = Visibility.Collapsed;

        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadModelsForLanguage(string language)
    {
        ModelDefinition[] filteredModels;

        if (language == "en")
        {
            // English: Show only .en models + large-v3 (multilingual only)
            filteredModels = _allModels
                .Where(m => m.IsEnglishOnly || m.Name == "large-v3")
                .ToArray();
        }
        else
        {
            // German: Show only multilingual models (no .en suffix)
            filteredModels = _allModels
                .Where(m => !m.IsEnglishOnly)
                .ToArray();
        }

        ModelGrid.ItemsSource = filteredModels;

        // Auto-select recommended model (small or small.en)
        var recommended = filteredModels.FirstOrDefault(m =>
            m.Name == "small" || m.Name == "small.en");

        if (recommended != null)
        {
            ModelGrid.SelectedItem = recommended;
        }
    }

    private void ModelGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelGrid.SelectedItem is not ModelDefinition model)
        {
            BrowseModelButton.IsEnabled = false;
            return;
        }

        _selectedModel = model;
        BrowseModelButton.IsEnabled = true;

        // Reset file selection when model changes
        _modelFilePath = null;
        ModelFileTextBox.Text = "Keine Datei ausgewählt";
        ValidationPanel.Visibility = Visibility.Collapsed;
        ValidationResult.Visibility = Visibility.Collapsed;

        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    private async void BrowseModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedModel == null)
            return;

        var dialog = new OpenFileDialog
        {
            Title = $"Wählen Sie die Datei für {_selectedModel.Name}",
            Filter = "Whisper Model Files (*.bin)|*.bin|All Files (*.*)|*.*",
            FileName = _selectedModel.FileName,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            return;

        _modelFilePath = dialog.FileName;
        ModelFileTextBox.Text = Path.GetFileName(_modelFilePath);

        // Start validation
        await ValidateModelFileAsync();
    }

    private async System.Threading.Tasks.Task ValidateModelFileAsync()
    {
        if (string.IsNullOrEmpty(_modelFilePath) || _selectedModel == null)
            return;

        // Show validation UI
        ValidationPanel.Visibility = Visibility.Visible;
        ValidationResult.Visibility = Visibility.Collapsed;
        ValidationStatus.Text = "Berechne SHA-1 Hash...";
        BrowseModelButton.IsEnabled = false;

        try
        {
            AppLogger.LogInformation("Validating model file", new
            {
                Model = _selectedModel.Name,
                FilePath = _modelFilePath,
                ExpectedHash = _selectedModel.SHA1
            });

            var isValid = await _validator.ValidateAsync(_modelFilePath, _selectedModel.SHA1);

            // Hide progress, show result
            ValidationPanel.Visibility = Visibility.Collapsed;
            ValidationResult.Visibility = Visibility.Visible;

            if (isValid)
            {
                ValidationResult.Text = "✓ Datei erfolgreich validiert";
                ValidationResult.Foreground = System.Windows.Media.Brushes.Green;

                AppLogger.LogInformation("Model validation successful", new
                {
                    Model = _selectedModel.Name
                });

                ModelChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ValidationResult.Text = "✗ SHA-1 Hash stimmt nicht überein - Datei ist möglicherweise beschädigt";
                ValidationResult.Foreground = System.Windows.Media.Brushes.Red;

                AppLogger.LogWarning("Model validation failed - SHA-1 mismatch", new
                {
                    Model = _selectedModel.Name,
                    FilePath = _modelFilePath
                });

                _modelFilePath = null; // Invalidate selection
            }
        }
        catch (Exception ex)
        {
            ValidationPanel.Visibility = Visibility.Collapsed;
            ValidationResult.Visibility = Visibility.Visible;
            ValidationResult.Text = $"✗ Fehler bei Validierung: {ex.Message}";
            ValidationResult.Foreground = System.Windows.Media.Brushes.Red;

            AppLogger.LogError("Model validation error", ex, new
            {
                Model = _selectedModel.Name,
                FilePath = _modelFilePath
            });

            _modelFilePath = null;
        }
        finally
        {
            BrowseModelButton.IsEnabled = true;
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        // Open HuggingFace URL in browser
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true
            });
            e.Handled = true;

            AppLogger.LogInformation("Opened HuggingFace link in browser");
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to open browser", ex);
            MessageBox.Show(
                $"Link konnte nicht geöffnet werden:\n{e.Uri}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    public ModelDefinition? GetSelectedModel() => _selectedModel;

    public string? GetModelFilePath() => _modelFilePath;

    public string GetSelectedLanguage() => _selectedLanguage;

    public bool IsValid()
    {
        return _selectedModel != null &&
               !string.IsNullOrEmpty(_modelFilePath) &&
               File.Exists(_modelFilePath);
    }
}
