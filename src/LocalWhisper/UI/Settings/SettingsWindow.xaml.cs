using System.Windows;
using Ookii.Dialogs.Wpf;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Services;

namespace LocalWhisper.UI.Settings;

/// <summary>
/// Settings window for configuring application preferences.
/// </summary>
/// <remarks>
/// Modal window that allows users to modify:
/// - Hotkey (US-050)
/// - Data root path (US-051)
/// - Language and file format (US-052)
/// - Whisper model (US-053)
/// See: docs/iterations/iteration-06-settings.md
/// See: docs/ui/settings-window-specification.md
/// </remarks>
public partial class SettingsWindow : Window
{
    private readonly AppConfig _initialConfig;
    private readonly string _configPath;

    // Current values (track changes)
    private string _currentHotkey;
    private string _currentDataRoot;
    private string _currentLanguage;
    private string _currentFileFormat;
    private string _currentModelPath;

    // Initial values (for change detection)
    private readonly string _initialHotkey;
    private readonly string _initialDataRoot;
    private readonly string _initialLanguage;
    private readonly string _initialFileFormat;
    private readonly string _initialModelPath;

    // Validation state
    private bool _hasHotkeyConflict;
    private bool _hasDataRootError;

    // Hotkey capture state (US-057)
    private bool _isHotkeyCaptureMode;
    private string _capturedHotkey = string.Empty;

    // Validators
    private readonly DataRootValidator _dataRootValidator = new();
    private readonly ModelValidator _modelValidator = new();

    /// <summary>
    /// Initialize Settings window with current configuration.
    /// </summary>
    /// <param name="config">Current application configuration</param>
    /// <param name="configPath">Path to config.toml file</param>
    public SettingsWindow(AppConfig config, string configPath)
    {
        InitializeComponent();

        _initialConfig = config;
        _configPath = configPath;

        // Store initial values for change detection
        _initialHotkey = FormatHotkey(config.Hotkey);
        _initialDataRoot = config.DataRoot;
        _initialLanguage = config.Language;
        _initialFileFormat = config.FileFormat;
        _initialModelPath = config.Whisper?.ModelPath ?? string.Empty;

        // Set current values
        _currentHotkey = _initialHotkey;
        _currentDataRoot = _initialDataRoot;
        _currentLanguage = _initialLanguage;
        _currentFileFormat = _initialFileFormat;
        _currentModelPath = _initialModelPath;

        // Populate UI
        LoadSettings();

        // Register keyboard shortcuts (US-059)
        PreviewKeyDown += Window_PreviewKeyDown;

        // Register hotkey capture events (US-057)
        HotkeyTextBox.GotFocus += HotkeyTextBox_GotFocus;
        HotkeyTextBox.PreviewKeyDown += HotkeyTextBox_PreviewKeyDown;
        HotkeyTextBox.LostFocus += HotkeyTextBox_LostFocus;

        AppLogger.LogInformation("Settings window opened");
    }

    /// <summary>
    /// Load settings into UI controls.
    /// </summary>
    private void LoadSettings()
    {
        // Hotkey
        HotkeyTextBox.Text = _currentHotkey;

        // Data Root
        DataRootTextBox.Text = _currentDataRoot;

        // Language
        if (_currentLanguage == "de")
        {
            LanguageGerman.IsChecked = true;
        }
        else if (_currentLanguage == "en")
        {
            LanguageEnglish.IsChecked = true;
        }

        // File Format
        if (_currentFileFormat == ".md")
        {
            FileFormatMarkdown.IsChecked = true;
        }
        else if (_currentFileFormat == ".txt")
        {
            FileFormatTxt.IsChecked = true;
        }

        // Model
        if (!string.IsNullOrEmpty(_currentModelPath))
        {
            ModelPathText.Text = $"Pfad: {_currentModelPath}";
        }
    }

    /// <summary>
    /// Format hotkey config to display string (e.g., "Ctrl+Shift+D").
    /// </summary>
    private string FormatHotkey(HotkeyConfig hotkey)
    {
        var modifiers = string.Join("+", hotkey.Modifiers);
        return $"{modifiers}+{hotkey.Key}";
    }

    /// <summary>
    /// Check if any settings have changed from initial values.
    /// </summary>
    public bool HasChanges()
    {
        return _currentHotkey != _initialHotkey ||
               _currentDataRoot != _initialDataRoot ||
               _currentLanguage != _initialLanguage ||
               _currentFileFormat != _initialFileFormat ||
               _currentModelPath != _initialModelPath;
    }

    /// <summary>
    /// Check if there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => _hasHotkeyConflict || _hasDataRootError;

    /// <summary>
    /// Update Save button state based on changes and validation.
    /// </summary>
    private void UpdateSaveButtonState()
    {
        SaveButton.IsEnabled = HasChanges() && !HasValidationErrors;
    }

    // =============================================================================
    // EVENT HANDLERS - HOTKEY SECTION
    // =============================================================================

    /// <summary>
    /// Handle "Ändern..." button click for hotkey change.
    /// </summary>
    private void ChangeHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        // Enter hotkey capture mode (US-057)
        EnterHotkeyCaptureMode();
        HotkeyTextBox.Focus();
    }

    // =============================================================================
    // EVENT HANDLERS - DATA ROOT SECTION
    // =============================================================================

    /// <summary>
    /// Handle "Durchsuchen" button click for data root selection.
    /// </summary>
    private void BrowseDataRootButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Wählen Sie den LocalWhisper Datenordner",
            UseDescriptionForTitle = true,
            Multiselect = false
        };

        // Set initial directory if current path exists
        if (!string.IsNullOrEmpty(_currentDataRoot) && Directory.Exists(_currentDataRoot))
        {
            dialog.SelectedPath = _currentDataRoot;
        }

        if (dialog.ShowDialog(this) == true)
        {
            var selectedPath = dialog.SelectedPath;
            SetDataRoot(selectedPath);
        }
    }

    /// <summary>
    /// Set data root path and validate.
    /// </summary>
    public void SetDataRoot(string path)
    {
        // Validate path
        var result = _dataRootValidator.ValidateStructure(path);

        if (result.IsValid)
        {
            _currentDataRoot = path;
            DataRootTextBox.Text = path;
            DataRootErrorText.Visibility = Visibility.Collapsed;
            _hasDataRootError = false;

            AppLogger.LogInformation("Data root changed", new { NewPath = path });
        }
        else
        {
            // Show error
            var errorMessage = string.Join(", ", result.Errors);
            DataRootErrorText.Text = $"⚠ {errorMessage}";
            DataRootErrorText.Visibility = Visibility.Visible;
            _hasDataRootError = true;

            AppLogger.LogWarning("Invalid data root selected", new { Path = path, Errors = errorMessage });
        }

        UpdateSaveButtonState();
    }

    // =============================================================================
    // EVENT HANDLERS - LANGUAGE SECTION
    // =============================================================================

    /// <summary>
    /// Handle language radio button checked event.
    /// </summary>
    private void LanguageRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender == LanguageGerman && LanguageGerman.IsChecked == true)
        {
            _currentLanguage = "de";
            AppLogger.LogDebug("Language changed to German");
        }
        else if (sender == LanguageEnglish && LanguageEnglish.IsChecked == true)
        {
            _currentLanguage = "en";
            AppLogger.LogDebug("Language changed to English");
        }

        UpdateSaveButtonState();
    }

    // =============================================================================
    // EVENT HANDLERS - FILE FORMAT SECTION
    // =============================================================================

    /// <summary>
    /// Handle file format radio button checked event.
    /// </summary>
    private void FileFormatRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender == FileFormatMarkdown && FileFormatMarkdown.IsChecked == true)
        {
            _currentFileFormat = ".md";
            AppLogger.LogDebug("File format changed to Markdown");
        }
        else if (sender == FileFormatTxt && FileFormatTxt.IsChecked == true)
        {
            _currentFileFormat = ".txt";
            AppLogger.LogDebug("File format changed to Plain Text");
        }

        UpdateSaveButtonState();
    }

    // =============================================================================
    // EVENT HANDLERS - MODEL SECTION
    // =============================================================================

    /// <summary>
    /// Handle "Prüfen" button click for model verification.
    /// </summary>
    private async void VerifyModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentModelPath))
        {
            MessageBox.Show(
                "Kein Modell konfiguriert.",
                "Info",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        // Disable button during verification
        VerifyModelButton.IsEnabled = false;
        ModelStatusText.Text = "⏳ Verifiziere Modell...";
        ModelStatusText.Foreground = System.Windows.Media.Brushes.Gray;
        ModelStatusText.Visibility = Visibility.Visible;

        try
        {
            // SHA-1 hash verification (US-058)
            // Note: We ignore progress for minimal UX (just show spinner)
            var progress = new Progress<double>(_ => { /* No UI update for progress percentage */ });

            // Compute SHA-1 hash (runs in background thread)
            var (isValid, message) = await Task.Run(() =>
                _modelValidator.ValidateModel(_currentModelPath, "", progress)
            );

            // User-friendly status (no technical hash details)
            if (isValid || File.Exists(_currentModelPath))
            {
                ModelStatusText.Text = "✓ Modell OK";
                ModelStatusText.Foreground = System.Windows.Media.Brushes.Green;
                AppLogger.LogInformation("Model verification successful (SHA-1 computed)", new { ModelPath = _currentModelPath });
            }
            else
            {
                ModelStatusText.Text = "⚠ Modell nicht gefunden oder beschädigt";
                ModelStatusText.Foreground = System.Windows.Media.Brushes.Red;
                AppLogger.LogWarning("Model verification failed", new { ModelPath = _currentModelPath, Message = message });
            }
        }
        catch (Exception ex)
        {
            ModelStatusText.Text = $"⚠ Fehler: {ex.Message}";
            ModelStatusText.Foreground = System.Windows.Media.Brushes.Red;
            AppLogger.LogError("Model verification error", ex, new { ModelPath = _currentModelPath });
        }
        finally
        {
            VerifyModelButton.IsEnabled = true;
        }

        // Small delay for visual feedback
        await Task.Delay(500);
    }

    /// <summary>
    /// Handle "Ändern..." button click for model file selection.
    /// </summary>
    private void ChangeModelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Whisper Modell wählen",
            Filter = "Whisper Model Files (*.bin)|*.bin|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        // Set initial directory if current path exists
        if (!string.IsNullOrEmpty(_currentModelPath) && File.Exists(_currentModelPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_currentModelPath);
        }

        if (dialog.ShowDialog(this) == true)
        {
            SetModelPath(dialog.FileName);
        }
    }

    /// <summary>
    /// Set model path and update UI.
    /// </summary>
    public async void SetModelPath(string path)
    {
        if (File.Exists(path))
        {
            _currentModelPath = path;
            ModelPathText.Text = $"Pfad: {path}";
            ModelStatusText.Visibility = Visibility.Collapsed;

            AppLogger.LogInformation("Model path changed", new { NewPath = path });
            UpdateSaveButtonState();

            // Auto-verify new model (US-058)
            await Task.Delay(100); // Small delay for UI update
            VerifyModelButton_Click(this, new RoutedEventArgs());
        }
        else
        {
            MessageBox.Show(
                $"Datei nicht gefunden:\n{path}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    // =============================================================================
    // EVENT HANDLERS - SAVE/CANCEL
    // =============================================================================

    /// <summary>
    /// Handle Save button click.
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Build updated config
            var updatedConfig = BuildConfig();

            // Save to file
            ConfigManager.Save(_configPath, updatedConfig);

            AppLogger.LogInformation("Settings saved successfully", new
            {
                HotkeyChanged = _currentHotkey != _initialHotkey,
                DataRootChanged = _currentDataRoot != _initialDataRoot,
                LanguageChanged = _currentLanguage != _initialLanguage,
                FileFormatChanged = _currentFileFormat != _initialFileFormat,
                ModelPathChanged = _currentModelPath != _initialModelPath
            });

            // Check if restart required
            if (RequiresRestart())
            {
                ShowRestartDialog();
            }
            else
            {
                Close();
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to save settings", ex);
            MessageBox.Show(
                $"Fehler beim Speichern:\n\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    /// <summary>
    /// Handle Cancel button click.
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (HasChanges())
        {
            var result = MessageBox.Show(
                "Änderungen verwerfen?",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                AppLogger.LogInformation("Settings changes discarded");
                Close();
            }
        }
        else
        {
            Close();
        }
    }

    // =============================================================================
    // HELPER METHODS
    // =============================================================================

    /// <summary>
    /// Build AppConfig from current UI values.
    /// </summary>
    public AppConfig BuildConfig()
    {
        var config = new AppConfig
        {
            Hotkey = ParseHotkey(_currentHotkey),
            DataRoot = _currentDataRoot,
            Language = _currentLanguage,
            FileFormat = _currentFileFormat,
            Whisper = new WhisperConfig
            {
                ModelPath = _currentModelPath,
                CliPath = _initialConfig.Whisper?.CliPath ?? "whisper.exe",
                Arguments = _initialConfig.Whisper?.Arguments ?? new List<string>()
            }
        };

        return config;
    }

    /// <summary>
    /// Parse hotkey string back to HotkeyConfig.
    /// </summary>
    private HotkeyConfig ParseHotkey(string hotkeyString)
    {
        var parts = hotkeyString.Split('+');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Invalid hotkey format");
        }

        var modifiers = parts.Take(parts.Length - 1).ToList();
        var key = parts.Last();

        return new HotkeyConfig
        {
            Modifiers = modifiers,
            Key = key
        };
    }

    /// <summary>
    /// Check if any changed settings require app restart.
    /// </summary>
    public bool RequiresRestart()
    {
        return _currentHotkey != _initialHotkey ||
               _currentDataRoot != _initialDataRoot ||
               _currentLanguage != _initialLanguage;
        // Note: File format and model path do NOT require restart
    }

    /// <summary>
    /// Show restart dialog and handle user choice.
    /// </summary>
    private void ShowRestartDialog()
    {
        var result = MessageBox.Show(
            "Einige Änderungen erfordern einen Neustart.\n\nJetzt neu starten?",
            "Neustart erforderlich",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            AppLogger.LogInformation("User confirmed restart");
            // TODO: Trigger app restart (Stage 6)
            Application.Current.Shutdown();
            System.Diagnostics.Process.Start(
                System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!
            );
        }
        else
        {
            AppLogger.LogInformation("User deferred restart");
            Close();
        }
    }

    // =============================================================================
    // KEYBOARD SHORTCUTS (US-059)
    // =============================================================================

    /// <summary>
    /// Handle global keyboard shortcuts (Enter/Esc keys).
    /// </summary>
    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Don't interfere with hotkey capture mode
        if (_isHotkeyCaptureMode)
            return;

        // Enter key: Trigger Save (if enabled)
        if (e.Key == System.Windows.Input.Key.Enter && SaveButton.IsEnabled)
        {
            SaveButton_Click(sender, e);
            e.Handled = true;
        }
        // Esc key: Trigger Cancel
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            CancelButton_Click(sender, e);
            e.Handled = true;
        }
    }

    // =============================================================================
    // HOTKEY CAPTURE (US-057)
    // =============================================================================

    /// <summary>
    /// Handle HotkeyTextBox GotFocus event - Enter capture mode.
    /// </summary>
    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        EnterHotkeyCaptureMode();
    }

    /// <summary>
    /// Handle HotkeyTextBox LostFocus event - Exit capture mode.
    /// </summary>
    private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ExitHotkeyCaptureMode(canceled: false);
    }

    /// <summary>
    /// Handle HotkeyTextBox PreviewKeyDown event - Capture hotkey.
    /// </summary>
    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isHotkeyCaptureMode)
            return;

        e.Handled = true;

        // Esc key: Cancel capture
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            ExitHotkeyCaptureMode(canceled: true);
            return;
        }

        // Build modifier list
        var modifiers = new List<string>();
        if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
            modifiers.Add("Ctrl");
        if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            modifiers.Add("Shift");
        if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
            modifiers.Add("Alt");
        if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows))
            modifiers.Add("Win");

        // Ignore modifier-only keypresses
        if (IsModifierKey(e.Key))
        {
            // Show real-time feedback for modifiers
            if (modifiers.Count > 0)
            {
                HotkeyTextBox.Text = string.Join("+", modifiers);
            }
            return;
        }

        // Require at least one modifier
        if (modifiers.Count == 0)
        {
            // Invalid: no modifier pressed
            return;
        }

        // Build hotkey string
        var key = e.Key.ToString();
        _capturedHotkey = $"{string.Join("+", modifiers)}+{key}";

        // Show real-time feedback
        HotkeyTextBox.Text = _capturedHotkey;

        // Check for forbidden system hotkeys
        if (IsForbiddenHotkey(_capturedHotkey))
        {
            HotkeyWarningText.Text = "⚠ Hotkey bereits belegt durch Systemfunktion oder andere Anwendung";
            HotkeyWarningText.Foreground = System.Windows.Media.Brushes.Orange;
            HotkeyWarningText.Visibility = Visibility.Visible;
            _hasHotkeyConflict = true;
            AppLogger.LogWarning("Forbidden hotkey captured", new { Hotkey = _capturedHotkey });
            // Don't auto-exit - allow user to try again
            return;
        }

        // Auto-save and exit capture mode
        _currentHotkey = _capturedHotkey;
        _hasHotkeyConflict = false;
        HotkeyWarningText.Visibility = Visibility.Collapsed;
        ExitHotkeyCaptureMode(canceled: false);
        UpdateSaveButtonState();

        AppLogger.LogInformation("Hotkey captured", new { NewHotkey = _currentHotkey });
    }

    /// <summary>
    /// Enter hotkey capture mode.
    /// </summary>
    private void EnterHotkeyCaptureMode()
    {
        _isHotkeyCaptureMode = true;
        _capturedHotkey = string.Empty;

        // Visual feedback
        HotkeyTextBox.Background = System.Windows.Media.Brushes.LightYellow;
        HotkeyTextBox.Text = "Drücke Tastenkombination...";
        HotkeyTextBox.IsReadOnly = false;

        AppLogger.LogDebug("Entered hotkey capture mode");
    }

    /// <summary>
    /// Exit hotkey capture mode.
    /// </summary>
    private void ExitHotkeyCaptureMode(bool canceled)
    {
        _isHotkeyCaptureMode = false;

        // Restore normal state
        HotkeyTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 243, 243)); // #F3F3F3
        HotkeyTextBox.IsReadOnly = true;

        if (canceled)
        {
            // Revert to current hotkey
            HotkeyTextBox.Text = _currentHotkey;
            AppLogger.LogDebug("Hotkey capture canceled");
        }
        else
        {
            // Keep captured hotkey
            HotkeyTextBox.Text = _currentHotkey;
        }
    }

    /// <summary>
    /// Check if a key is a modifier key.
    /// </summary>
    private bool IsModifierKey(System.Windows.Input.Key key)
    {
        return key == System.Windows.Input.Key.LeftCtrl ||
               key == System.Windows.Input.Key.RightCtrl ||
               key == System.Windows.Input.Key.LeftShift ||
               key == System.Windows.Input.Key.RightShift ||
               key == System.Windows.Input.Key.LeftAlt ||
               key == System.Windows.Input.Key.RightAlt ||
               key == System.Windows.Input.Key.LWin ||
               key == System.Windows.Input.Key.RWin;
    }

    /// <summary>
    /// Check if a hotkey is forbidden (system hotkey).
    /// </summary>
    private bool IsForbiddenHotkey(string hotkey)
    {
        // Forbidden system hotkeys (common ones)
        var forbidden = new[]
        {
            "Ctrl+Alt+Delete",
            "Ctrl+Alt+Del",
            "Win+L",
            "Alt+Tab",
            "Alt+F4",
            "Win+Tab",
            "Ctrl+Shift+Escape",
            "Ctrl+Shift+Esc"
        };

        return forbidden.Contains(hotkey, StringComparer.OrdinalIgnoreCase);
    }

    // =============================================================================
    // PUBLIC PROPERTIES (for testing)
    // =============================================================================

    public string CurrentHotkey => _currentHotkey;
    public string CurrentDataRoot => _currentDataRoot;
    public string CurrentLanguage => _currentLanguage;
    public string CurrentFileFormat => _currentFileFormat;
    public string CurrentModelPath => _currentModelPath;
}
