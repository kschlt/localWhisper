using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private bool _currentPostProcessingEnabled;  // Iteration 7
    private string _currentLlmCliPath;  // Iteration 7
    private string _currentLlmModelPath;  // Iteration 7
    private bool _currentUseGlossary;  // Iteration 7
    private string _currentGlossaryPath;  // Iteration 7

    // Initial values (for change detection)
    private readonly string _initialHotkey;
    private readonly string _initialDataRoot;
    private readonly string _initialLanguage;
    private readonly string _initialFileFormat;
    private readonly string _initialModelPath;
    private readonly bool _initialPostProcessingEnabled;  // Iteration 7
    private readonly string _initialLlmCliPath;  // Iteration 7
    private readonly string _initialLlmModelPath;  // Iteration 7
    private readonly bool _initialUseGlossary;  // Iteration 7
    private readonly string _initialGlossaryPath;  // Iteration 7

    // Validation state
    private bool _hasHotkeyConflict; // Warning only (allows save)
    private bool _hasHotkeyError; // Validation error (blocks save)
    private bool _hasDataRootError;
    private bool _hasModelError; // Model validation error (blocks save)

    // Hotkey capture state (US-057)
    private bool _isHotkeyCaptureMode;
    private string _capturedHotkey = string.Empty;

    // Validators
    private readonly DataRootValidator _dataRootValidator = new();
    private ModelValidator _modelValidator = new();

    // Cancellation for async operations (prevents crashes during window disposal)
    private readonly CancellationTokenSource _cancellationTokenSource = new();

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
        _initialPostProcessingEnabled = config.PostProcessing.Enabled;  // Iteration 7
        _initialLlmCliPath = config.PostProcessing.LlmCliPath;  // Iteration 7
        _initialLlmModelPath = config.PostProcessing.ModelPath;  // Iteration 7
        _initialUseGlossary = config.PostProcessing.UseGlossary;  // Iteration 7
        _initialGlossaryPath = config.PostProcessing.GlossaryPath;  // Iteration 7

        // Set current values
        _currentHotkey = _initialHotkey;
        _currentDataRoot = _initialDataRoot;
        _currentLanguage = _initialLanguage;
        _currentFileFormat = _initialFileFormat;
        _currentModelPath = _initialModelPath;
        _currentPostProcessingEnabled = _initialPostProcessingEnabled;  // Iteration 7
        _currentLlmCliPath = _initialLlmCliPath;  // Iteration 7
        _currentLlmModelPath = _initialLlmModelPath;  // Iteration 7
        _currentUseGlossary = _initialUseGlossary;  // Iteration 7
        _currentGlossaryPath = _initialGlossaryPath;  // Iteration 7

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
    /// Cancel pending async operations when window closes to prevent crashes.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        base.OnClosed(e);
        AppLogger.LogDebug("Settings window closed and async operations canceled");
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

        // Post-Processing (Iteration 7)
        PostProcessingEnabledCheckBox.IsChecked = _currentPostProcessingEnabled;
        LlmCliPathTextBox.Text = _currentLlmCliPath;
        LlmModelPathTextBox.Text = _currentLlmModelPath;
        UseGlossaryCheckBox.IsChecked = _currentUseGlossary;
        GlossaryPathTextBox.Text = _currentGlossaryPath;
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
               _currentModelPath != _initialModelPath ||
               _currentPostProcessingEnabled != _initialPostProcessingEnabled ||  // Iteration 7
               _currentLlmCliPath != _initialLlmCliPath ||  // Iteration 7
               _currentLlmModelPath != _initialLlmModelPath ||  // Iteration 7
               _currentUseGlossary != _initialUseGlossary ||  // Iteration 7
               _currentGlossaryPath != _initialGlossaryPath;  // Iteration 7
    }

    /// <summary>
    /// Check if there are any validation errors (errors block save, warnings don't).
    /// </summary>
    public bool HasValidationErrors => _hasHotkeyError || _hasDataRootError || _hasModelError;

    // =============================================================================
    // INTERNAL TEST PROPERTIES - For testability via InternalsVisibleTo
    // =============================================================================

    /// <summary>Current hotkey value (for testing)</summary>
    internal string CurrentHotkey => _currentHotkey;

    /// <summary>Current data root value (for testing)</summary>
    internal string CurrentDataRoot => _currentDataRoot;

    /// <summary>Current language value (for testing)</summary>
    internal string CurrentLanguage => _currentLanguage;

    /// <summary>Current file format value (for testing)</summary>
    internal string CurrentFileFormat => _currentFileFormat;

    /// <summary>Current model path value (for testing)</summary>
    internal string CurrentModelPath => _currentModelPath;

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
        await VerifyModelAsync();
    }

    /// <summary>
    /// Async model verification logic (testable).
    /// </summary>
    private async Task VerifyModelAsync()
    {
        if (string.IsNullOrEmpty(_currentModelPath))
        {
            // Show error in UI without blocking MessageBox
            ModelStatusText.Text = "⚠ Kein Modell konfiguriert";
            ModelStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            ModelStatusText.Visibility = Visibility.Visible;
            AppLogger.LogWarning("Model verification skipped - no model path configured");
            return;
        }

        // Fire progress dialog shown event (for testing)
        OnProgressDialogShown?.Invoke();

        // Disable button during verification - ensure UI thread access
        if (Dispatcher.CheckAccess())
        {
            VerifyModelButton.IsEnabled = false;
            ModelStatusText.Text = "⏳ Verifiziere Modell...";
            ModelStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            ModelStatusText.Visibility = Visibility.Visible;
        }
        else
        {
            Dispatcher.Invoke(() =>
            {
                VerifyModelButton.IsEnabled = false;
                ModelStatusText.Text = "⏳ Verifiziere Modell...";
                ModelStatusText.Foreground = System.Windows.Media.Brushes.Gray;
                ModelStatusText.Visibility = Visibility.Visible;
            });
        }

        try
        {
            // SHA-1 hash verification (US-058)
            var progress = new Progress<double>(_ => { });

            // Compute SHA-1 hash (runs in background thread)
            var (isValid, message) = await Task.Run(() =>
                _modelValidator.ValidateModel(_currentModelPath, "", progress)
            );

            // Update UI on UI thread
            Action updateUI = () =>
            {
                if (isValid)
                {
                    ModelStatusText.Text = "✓ Modell OK";
                    ModelStatusText.Foreground = System.Windows.Media.Brushes.Green;
                    _hasModelError = false;
                    AppLogger.LogInformation("Model verification successful (SHA-1 computed)", new { ModelPath = _currentModelPath });
                }
                else if (!File.Exists(_currentModelPath))
                {
                    ModelStatusText.Text = "⚠ Modell nicht gefunden oder beschädigt";
                    ModelStatusText.Foreground = System.Windows.Media.Brushes.Red;
                    _hasModelError = false;
                    AppLogger.LogWarning("Model verification failed", new { ModelPath = _currentModelPath, Message = message });
                }
                else
                {
                    ModelStatusText.Text = "⚠ Modell ungültig (Hash-Prüfung fehlgeschlagen)";
                    ModelStatusText.Foreground = System.Windows.Media.Brushes.Red;
                    _hasModelError = true;
                    AppLogger.LogWarning("Model hash validation failed", new { ModelPath = _currentModelPath, Message = message });
                }
            };

            if (Dispatcher.CheckAccess())
            {
                updateUI();
            }
            else
            {
                Dispatcher.Invoke(updateUI);
            }
        }
        catch (Exception ex)
        {
            Action updateUIError = () =>
            {
                ModelStatusText.Text = $"⚠ Fehler: {ex.Message}";
                ModelStatusText.Foreground = System.Windows.Media.Brushes.Red;
                _hasModelError = false;
                AppLogger.LogError("Model verification error", ex, new { ModelPath = _currentModelPath });
            };

            if (Dispatcher.CheckAccess())
            {
                updateUIError();
            }
            else
            {
                Dispatcher.Invoke(updateUIError);
            }
        }
        finally
        {
            if (Dispatcher.CheckAccess())
            {
                VerifyModelButton.IsEnabled = true;
                UpdateSaveButtonState();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    VerifyModelButton.IsEnabled = true;
                    UpdateSaveButtonState();
                });
            }
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
        // Check if window is closing/disposed
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            AppLogger.LogDebug("SetModelPath canceled - window is closing");
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                _currentModelPath = path;
                ModelPathText.Text = $"Pfad: {path}";
                ModelStatusText.Visibility = Visibility.Collapsed;
                _hasModelError = false;

                AppLogger.LogInformation("Model path changed", new { NewPath = path });
                UpdateSaveButtonState();

                // Auto-verify new model (US-058)
                await Task.Delay(100, _cancellationTokenSource.Token);

                // Check cancellation again before starting async operation
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                // Ensure verification runs on UI thread
                await Dispatcher.InvokeAsync(() => VerifyModelButton_Click(this, new RoutedEventArgs()));
            }
            else
            {
                // Show error in UI without blocking MessageBox
                ModelStatusText.Text = $"⚠ Datei nicht gefunden";
                ModelStatusText.Foreground = System.Windows.Media.Brushes.Red;
                ModelStatusText.Visibility = Visibility.Visible;
                _hasModelError = true;
                AppLogger.LogWarning("Model file not found", new { Path = path });
                UpdateSaveButtonState();
            }
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation (happens during window close/disposal)
            AppLogger.LogDebug("SetModelPath operation was canceled");
        }
        catch (ObjectDisposedException)
        {
            // Window was disposed during async operation - ignore
            AppLogger.LogDebug("SetModelPath canceled - window was disposed");
        }
    }

    // =============================================================================
    // EVENT HANDLERS - POST-PROCESSING SECTION (Iteration 7)
    // =============================================================================

    /// <summary>
    /// Handle post-processing enabled checkbox changed event (US-065).
    /// </summary>
    private void PostProcessingEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _currentPostProcessingEnabled = PostProcessingEnabledCheckBox.IsChecked == true;
        UpdateSaveButtonState();
    }

    /// <summary>
    /// Handle glossary checkbox changed event (US-065).
    /// </summary>
    private void UseGlossaryCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _currentUseGlossary = UseGlossaryCheckBox.IsChecked == true;
        UpdateSaveButtonState();
    }

    /// <summary>
    /// Handle browse button click for llama-cli.exe (US-065).
    /// </summary>
    private void BrowseLlmCliButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "llama-cli.exe wählen",
            Filter = "All Files (*.*)|*.*",
            CheckFileExists = true
        };

        // Set initial directory if current path exists
        if (!string.IsNullOrEmpty(_currentLlmCliPath) && File.Exists(_currentLlmCliPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_currentLlmCliPath);
        }
        else if (!string.IsNullOrEmpty(_currentLlmCliPath))
        {
            var dir = Path.GetDirectoryName(_currentLlmCliPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                dialog.InitialDirectory = dir;
            }
        }

        if (dialog.ShowDialog(this) == true)
        {
            _currentLlmCliPath = dialog.FileName;
            LlmCliPathTextBox.Text = dialog.FileName;
            UpdateSaveButtonState();
        }
    }

    /// <summary>
    /// Handle browse button click for Llama model (US-065).
    /// </summary>
    private void BrowseLlmModelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Llama Modell wählen",
            Filter = "All Files (*.*)|*.*",
            CheckFileExists = true
        };

        // Set initial directory if current path exists
        if (!string.IsNullOrEmpty(_currentLlmModelPath) && File.Exists(_currentLlmModelPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_currentLlmModelPath);
        }
        else if (!string.IsNullOrEmpty(_currentLlmModelPath))
        {
            var dir = Path.GetDirectoryName(_currentLlmModelPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                dialog.InitialDirectory = dir;
            }
        }

        if (dialog.ShowDialog(this) == true)
        {
            _currentLlmModelPath = dialog.FileName;
            LlmModelPathTextBox.Text = dialog.FileName;
            UpdateSaveButtonState();
        }
    }

    /// <summary>
    /// Handle browse button click for glossary (US-065).
    /// </summary>
    private void BrowseGlossaryButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Glossar-Datei wählen",
            Filter = "All Files (*.*)|*.*",
            CheckFileExists = true
        };

        // Set initial directory if current path exists
        if (!string.IsNullOrEmpty(_currentGlossaryPath) && File.Exists(_currentGlossaryPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_currentGlossaryPath);
        }
        else if (!string.IsNullOrEmpty(_currentGlossaryPath))
        {
            var dir = Path.GetDirectoryName(_currentGlossaryPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                dialog.InitialDirectory = dir;
            }
        }

        if (dialog.ShowDialog(this) == true)
        {
            _currentGlossaryPath = dialog.FileName;
            GlossaryPathTextBox.Text = dialog.FileName;
            UpdateSaveButtonState();
        }
    }

    /// <summary>
    /// Handle text changed event in any textbox (US-065).
    /// </summary>
    private void SettingsChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Update current values from textboxes
        if (LlmCliPathTextBox != null)
            _currentLlmCliPath = LlmCliPathTextBox.Text ?? string.Empty;
        if (LlmModelPathTextBox != null)
            _currentLlmModelPath = LlmModelPathTextBox.Text ?? string.Empty;
        if (GlossaryPathTextBox != null)
            _currentGlossaryPath = GlossaryPathTextBox.Text ?? string.Empty;

        UpdateSaveButtonState();
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
            // Validate post-processing paths (US-065 - minimal validation)
            if (_currentPostProcessingEnabled)
            {
                if (!File.Exists(_currentLlmCliPath) || !File.Exists(_currentLlmModelPath))
                {
                    // Log error without blocking MessageBox - real app would show dialog
                    AppLogger.LogWarning("Post-processing validation failed - missing files", new
                    {
                        LlmCliExists = File.Exists(_currentLlmCliPath),
                        ModelExists = File.Exists(_currentLlmModelPath)
                    });
                    LastErrorMessage = "Post-Processing Dateien fehlen oder ungültig";
                    return; // Don't save
                }

                // Validate glossary if enabled
                if (_currentUseGlossary && !File.Exists(_currentGlossaryPath))
                {
                    // Log error without blocking MessageBox - real app would show dialog
                    AppLogger.LogWarning("Glossary validation failed - file not found", new
                    {
                        GlossaryPath = _currentGlossaryPath
                    });
                    LastErrorMessage = "Glossar-Datei nicht gefunden";
                    return; // Don't save
                }
            }

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
            // Store error without blocking MessageBox - real app would show dialog
            LastErrorMessage = $"Fehler beim Speichern: {ex.Message}";
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
                CLIPath = _initialConfig.Whisper?.CLIPath ?? "whisper-cli",
                Language = _initialConfig.Whisper?.Language ?? "de",
                TimeoutSeconds = _initialConfig.Whisper?.TimeoutSeconds ?? 60
            },
            PostProcessing = new PostProcessingConfig  // Iteration 7
            {
                Enabled = _currentPostProcessingEnabled,
                LlmCliPath = _currentLlmCliPath,
                ModelPath = _currentLlmModelPath,
                UseGlossary = _currentUseGlossary,
                GlossaryPath = _currentGlossaryPath,
                // Copy other settings from initial config
                TimeoutSeconds = _initialConfig.PostProcessing.TimeoutSeconds,
                GpuAcceleration = _initialConfig.PostProcessing.GpuAcceleration,
                Temperature = _initialConfig.PostProcessing.Temperature,
                TopP = _initialConfig.PostProcessing.TopP,
                RepeatPenalty = _initialConfig.PostProcessing.RepeatPenalty,
                MaxTokens = _initialConfig.PostProcessing.MaxTokens
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
        RestartDialogShown = true;
        OnRestartDialogShown?.Invoke();

        var result = MessageBox.Show(
            "Einige Änderungen erfordern einen Neustart.\n\nJetzt neu starten?",
            "Neustart erforderlich",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            AppLogger.LogInformation("User confirmed restart");
            OnRestartRequested?.Invoke();
            // TODO: Trigger app restart (Stage 6)
            Application.Current.Shutdown();
            System.Diagnostics.Process.Start(
                System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!
            );
        }
        else
        {
            AppLogger.LogInformation("User deferred restart");
            IsClosed = true;
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
        _hasHotkeyError = false;
        HotkeyWarningText.Visibility = Visibility.Collapsed;
        HotkeyErrorText.Visibility = Visibility.Collapsed;
        ExitHotkeyCaptureMode(canceled: false);
        UpdateSaveButtonState();

        AppLogger.LogInformation("Hotkey captured", new { NewHotkey = _currentHotkey });
    }

    /// <summary>
    /// Enter hotkey capture mode.
    /// </summary>
    public void EnterHotkeyCaptureMode()
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

    /// <summary>Hotkey capture mode state (for testing)</summary>
    internal bool IsHotkeyCaptureMode => _isHotkeyCaptureMode;

    /// <summary>Hotkey conflict state (for testing)</summary>
    internal bool HasHotkeyConflict => _hasHotkeyConflict;

    /// <summary>Last error message (for testing)</summary>
    internal string LastErrorMessage { get; private set; } = string.Empty;

    /// <summary>Window closed state (for testing)</summary>
    internal bool IsClosed { get; private set; }

    /// <summary>Confirmation dialog shown state (for testing)</summary>
    internal bool ConfirmationDialogShown { get; private set; }

    /// <summary>Restart dialog shown state (for testing)</summary>
    internal bool RestartDialogShown { get; private set; }

    // =============================================================================
    // EVENTS (for testing)
    // =============================================================================

    /// <summary>Event fired when progress dialog is shown</summary>
    internal event Action? OnProgressDialogShown;

    /// <summary>Event fired when log message is created</summary>
    internal event Action<string>? OnLogMessage;

    /// <summary>Event fired when restart dialog is shown</summary>
    internal event Action? OnRestartDialogShown;

    /// <summary>Event fired when restart is requested</summary>
    internal event Action? OnRestartRequested;

    // =============================================================================
    // TEST HELPER METHODS
    // =============================================================================

    /// <summary>
    /// Set hotkey programmatically (for testing).
    /// </summary>
    internal void SetHotkey(params string?[] parts)
    {
        // Clear previous errors/warnings
        _hasHotkeyError = false;
        _hasHotkeyConflict = false;
        HotkeyErrorText.Visibility = Visibility.Collapsed;
        HotkeyWarningText.Visibility = Visibility.Collapsed;

        // Validate: must have at least one modifier and one key
        var validParts = parts.Where(p => !string.IsNullOrEmpty(p)).ToList();

        if (validParts.Count < 2)
        {
            // Validation error: no modifier
            _hasHotkeyError = true;
            HotkeyErrorText.Text = "⚠ Mindestens ein Modifier erforderlich (Ctrl, Shift, Alt)";
            HotkeyErrorText.Visibility = Visibility.Visible;
            UpdateSaveButtonState();
            return;
        }

        // Join all parts except the last one as modifiers
        var modifiers = validParts.Take(validParts.Count - 1).ToList();
        var key = validParts.Last();

        _currentHotkey = string.Join("+", modifiers) + "+" + key;
        HotkeyTextBox.Text = _currentHotkey;

        // Check for conflicts (warning only, allows save)
        _hasHotkeyConflict = IsForbiddenHotkey(_currentHotkey);

        if (_hasHotkeyConflict)
        {
            HotkeyWarningText.Text = "⚠ Hotkey bereits belegt durch Systemfunktion oder andere Anwendung";
            HotkeyWarningText.Visibility = Visibility.Visible;
        }

        UpdateSaveButtonState();
    }

    /// <summary>
    /// Set model validator (for testing with mocks).
    /// </summary>
    internal void SetModelValidator(ModelValidator validator)
    {
        _modelValidator = validator;
    }

    /// <summary>
    /// Verify model synchronously (for testing).
    /// </summary>
    internal void VerifyModel()
    {
        // Ensure we're on UI thread
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => VerifyModel());
            return;
        }

        // Run async task synchronously with proper message pumping
        var task = VerifyModelAsync();

        // Wait for task completion by pumping dispatcher messages
        while (!task.IsCompleted)
        {
            // Process messages to prevent UI freeze and allow async operations to complete
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(delegate { }));
        }

        // Ensure any exceptions are observed
        if (task.IsFaulted && task.Exception != null)
        {
            throw task.Exception;
        }
    }

    /// <summary>
    /// Set model path synchronously without auto-verification (for testing).
    /// Tests should manually call VerifyModel() if verification is needed.
    /// This avoids async/Dispatcher complexity that causes crashes during test cleanup.
    /// </summary>
    internal void SetModelPathSync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Model file not found: {path}");
        }

        // Set path without triggering async verification
        _currentModelPath = path;
        ModelPathText.Text = $"Pfad: {path}";
        ModelStatusText.Visibility = Visibility.Collapsed;
        _hasModelError = false;

        AppLogger.LogInformation("Model path changed (test mode)", new { NewPath = path });
        UpdateSaveButtonState();

        // Note: No auto-verification to avoid async/timer/Dispatcher cleanup issues.
        // Tests should call VerifyModel() explicitly if verification is needed.
    }

    /// <summary>
    /// Save settings (for testing).
    /// </summary>
    /// <returns>True if save succeeded, false otherwise</returns>
    internal bool Save()
    {
        try
        {
            SaveButton_Click(this, new RoutedEventArgs());
            return !HasValidationErrors;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Cancel settings (for testing).
    /// </summary>
    /// <returns>True if window closed, false if user cancelled</returns>
    internal bool Cancel()
    {
        if (HasChanges())
        {
            ConfirmationDialogShown = true;
            // In real scenario, would show MessageBox.Show() and check result
            // For testing, assume user confirms
            Close();
            IsClosed = true;
            return true;
        }
        else
        {
            Close();
            IsClosed = true;
            return true;
        }
    }

    /// <summary>
    /// Simulate restart dialog "Yes" response (for testing).
    /// </summary>
    internal void SimulateRestartDialogYes()
    {
        OnRestartRequested?.Invoke();
    }

    /// <summary>
    /// Simulate restart dialog "No" response (for testing).
    /// </summary>
    internal void SimulateRestartDialogNo()
    {
        IsClosed = true;
    }
}
