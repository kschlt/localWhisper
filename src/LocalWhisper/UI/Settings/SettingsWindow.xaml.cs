using System.Windows;
using LocalWhisper.Core;
using LocalWhisper.Models;

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

        AppLogger.LogInformation("Settings window opened");
    }

    /// <summary>
    /// Load settings into UI controls.
    /// </summary>
    private void LoadSettings()
    {
        // Hotkey
        HotkeyTextBox.Text = _currentHotkey;

        // Data Root - Will add in Stage 2
        // Language - Will add in Stage 2
        // File Format - Will add in Stage 3
        // Model - Will add in Stage 3
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
    public bool HasValidationErrors => _hasHotkeyConflict; // Will expand in later stages

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
        // TODO: Implement hotkey capture dialog (Stage 1b)
        // For now, show a simple input dialog
        MessageBox.Show(
            "Hotkey-Änderung wird in der nächsten Phase implementiert.",
            "Info",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
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
    // PUBLIC PROPERTIES (for testing)
    // =============================================================================

    public string CurrentHotkey => _currentHotkey;
    public string CurrentDataRoot => _currentDataRoot;
    public string CurrentLanguage => _currentLanguage;
    public string CurrentFileFormat => _currentFileFormat;
    public string CurrentModelPath => _currentModelPath;
}
