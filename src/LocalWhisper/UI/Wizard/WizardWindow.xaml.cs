using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LocalWhisper.Core;
using LocalWhisper.Models;

namespace LocalWhisper.UI.Wizard;

/// <summary>
/// First-run wizard window.
/// </summary>
/// <remarks>
/// Implements US-040, US-041a, US-042: Wizard Steps 1-3
/// - Step 1: Data root selection
/// - Step 2: Model verification (file selection)
/// - Step 3: Hotkey configuration
///
/// See: docs/iterations/iteration-05a-wizard-core.md
/// </remarks>
public partial class WizardWindow : Window
{
    private int _currentStep = 1;
    private readonly WizardManager _manager = new();

    // Wizard results
    public string? DataRoot { get; private set; }
    public ModelDefinition? SelectedModel { get; private set; }
    public string? ModelFilePath { get; private set; }
    public string? SelectedLanguage { get; private set; }
    public ModifierKeys HotkeyModifiers { get; private set; }
    public Key HotkeyKey { get; private set; }

    // Step controls
    private DataRootStep? _dataRootStep;
    private ModelSelectionStep? _modelSelectionStep;
    private HotkeyStep? _hotkeyStep;

    public WizardWindow()
    {
        InitializeComponent();

        AppLogger.LogInformation("Wizard opened");

        // Show Step 1
        ShowStep(1);
    }

    private void ShowStep(int stepNumber)
    {
        _currentStep = stepNumber;

        // Update step indicators
        UpdateStepIndicators();

        // Update button visibility
        BackButton.IsEnabled = _currentStep > 1;
        NextButton.Visibility = _currentStep < 3 ? Visibility.Visible : Visibility.Collapsed;
        FinishButton.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

        // Load step content
        switch (_currentStep)
        {
            case 1:
                if (_dataRootStep == null)
                {
                    _dataRootStep = new DataRootStep();
                    _dataRootStep.DataRootChanged += (s, e) => NextButton.IsEnabled = true;
                }
                ContentArea.Content = _dataRootStep;
                NextButton.IsEnabled = _dataRootStep.IsValid();
                break;

            case 2:
                if (_modelSelectionStep == null)
                {
                    _modelSelectionStep = new ModelSelectionStep();
                    _modelSelectionStep.ModelChanged += (s, e) => NextButton.IsEnabled = _modelSelectionStep.IsValid();
                }
                ContentArea.Content = _modelSelectionStep;
                NextButton.IsEnabled = _modelSelectionStep.IsValid();
                break;

            case 3:
                if (_hotkeyStep == null)
                {
                    _hotkeyStep = new HotkeyStep();
                    _hotkeyStep.HotkeyChanged += (s, e) => FinishButton.IsEnabled = true;
                }
                ContentArea.Content = _hotkeyStep;
                FinishButton.IsEnabled = _hotkeyStep.IsValid();
                break;
        }

        AppLogger.LogInformation($"Wizard step {_currentStep} shown");
    }

    private void UpdateStepIndicators()
    {
        // Step 1
        if (_currentStep >= 1)
        {
            Step1Indicator.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            Step1Indicator.Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        }
        else
        {
            Step1Indicator.Fill = Brushes.Transparent;
            Step1Indicator.Stroke = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // Gray
        }

        // Step 2
        if (_currentStep >= 2)
        {
            Step2Indicator.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            Step2Indicator.Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            Step2Text.Foreground = Brushes.Black;
            Step2Text.FontWeight = _currentStep == 2 ? FontWeights.SemiBold : FontWeights.Normal;
        }
        else
        {
            Step2Indicator.Fill = Brushes.Transparent;
            Step2Indicator.Stroke = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            Step2Text.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            Step2Text.FontWeight = FontWeights.Normal;
        }

        // Step 3
        if (_currentStep >= 3)
        {
            Step3Indicator.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            Step3Indicator.Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            Step3Text.Foreground = Brushes.Black;
            Step3Text.FontWeight = FontWeights.SemiBold;
        }
        else
        {
            Step3Indicator.Fill = Brushes.Transparent;
            Step3Indicator.Stroke = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            Step3Text.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            Step3Text.FontWeight = FontWeights.Normal;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 1)
        {
            ShowStep(_currentStep - 1);
        }
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate current step
        if (_currentStep == 1 && _dataRootStep != null)
        {
            if (!_dataRootStep.IsValid())
            {
                MessageBox.Show("Bitte wählen Sie einen gültigen Datenordner.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRoot = _dataRootStep.GetDataRoot();

            // Validate write access
            if (!_manager.ValidateDataRoot(DataRoot))
            {
                MessageBox.Show("Keine Schreibrechte für diesen Ordner. Bitte wählen Sie einen anderen Ordner.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Create folder structure
            try
            {
                _manager.CreateDataRootStructure(DataRoot);
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Failed to create data root structure", ex);
                MessageBox.Show($"Fehler beim Erstellen der Ordnerstruktur:\n\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ShowStep(2);
        }
        else if (_currentStep == 2 && _modelSelectionStep != null)
        {
            if (!_modelSelectionStep.IsValid())
            {
                MessageBox.Show("Bitte wählen Sie ein Modell und warten Sie auf die Validierung.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedModel = _modelSelectionStep.GetSelectedModel();
            ModelFilePath = _modelSelectionStep.GetModelFilePath();
            SelectedLanguage = _modelSelectionStep.GetSelectedLanguage();

            ShowStep(3);
        }
    }

    private void FinishButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate hotkey
        if (_hotkeyStep == null || !_hotkeyStep.IsValid())
        {
            MessageBox.Show("Bitte wählen Sie eine gültige Tastenkombination.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        HotkeyModifiers = _hotkeyStep.GetHotkeyModifiers();
        HotkeyKey = _hotkeyStep.GetHotkeyKey();

        // Warn if hotkey has conflict (but allow user to proceed)
        if (_hotkeyStep.HasConflict())
        {
            var result = MessageBox.Show(
                "Diese Tastenkombination wird möglicherweise bereits von einer anderen Anwendung verwendet.\n\nTrotzdem fortfahren?",
                "Hotkey-Konflikt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                return;
            }
        }

        // Generate config
        try
        {
            _manager.GenerateInitialConfig(
                DataRoot!,
                ModelFilePath!,
                SelectedLanguage!,
                HotkeyModifiers,
                HotkeyKey);

            AppLogger.LogInformation("Wizard completed successfully");

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to complete wizard", ex);
            MessageBox.Show($"Fehler beim Abschließen der Einrichtung:\n\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Möchten Sie die Einrichtung wirklich abbrechen?\n\nDie App kann ohne Konfiguration nicht gestartet werden.",
            "Einrichtung abbrechen?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            AppLogger.LogInformation("Wizard cancelled by user");
            DialogResult = false;
            Close();
        }
    }
}
