using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.Services;
using LocalWhisper.UI.Settings;
using Moq;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for Whisper model verification and replacement in Settings window.
/// </summary>
/// <remarks>
/// Tests for US-053: Settings - Model Check/Reload
/// See: docs/iterations/iteration-06-settings.md (ModelVerificationTests section)
/// See: docs/ui/settings-window-specification.md (Whisper Model Section)
/// </remarks>
public class ModelVerificationTests : IDisposable
{
    private readonly List<System.Windows.Window> _windows = new();
    private readonly string _testDirectory;
    private readonly string _validModelPath;
    private readonly string _invalidModelPath;

    public ModelVerificationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_Models", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Initialize AppLogger with Error level to reduce test output verbosity
        LocalWhisper.Core.AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);

        // Create valid model file (with known content for SHA-1 testing)
        _validModelPath = Path.Combine(_testDirectory, "ggml-valid.bin");
        File.WriteAllText(_validModelPath, "valid model content");

        // Create invalid model file
        _invalidModelPath = Path.Combine(_testDirectory, "ggml-invalid.bin");
        File.WriteAllText(_invalidModelPath, "invalid model content");
    }

    private SettingsWindow CreateWindow(AppConfig config, string configPath = "C:\\Test\\config.toml")
    {
        var window = new SettingsWindow(config, configPath);
        _windows.Add(window);
        return window;
    }

    public void Dispose()
    {
        // Close all windows and shut down their Dispatchers
        var dispatchersToShutdown = new HashSet<System.Windows.Threading.Dispatcher>();
        
        foreach (var window in _windows)
        {
            try 
            { 
                if (window.Dispatcher != null && !window.Dispatcher.HasShutdownStarted)
                {
                    dispatchersToShutdown.Add(window.Dispatcher);
                    window.Close();
                }
            } 
            catch { }
        }
        
        // Force shutdown all Dispatchers to prevent message delivery after thread death
        foreach (var dispatcher in dispatchersToShutdown)
        {
            try
            {
                if (!dispatcher.HasShutdownStarted)
                {
                    dispatcher.InvokeShutdown();
                }
            }
            catch { }
        }

        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [StaFact]
    public void VerifyModel_ValidHash_ShowsSuccess()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _validModelPath;
        var window = CreateWindow(config);

        // Mock ModelValidator to return valid
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_validModelPath, It.IsAny<string>(), null))
            .Returns((true, "Hash matches"));
        window.SetModelValidator(mockValidator.Object);

        // Act
        window.VerifyModel();

        // Assert
        window.ModelStatusText.Text.Should().Contain("Modell OK");
        window.ModelStatusText.Foreground.Should().Be(System.Windows.Media.Brushes.Green);
    }

    [StaFact]
    public void VerifyModel_InvalidHash_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _invalidModelPath;
        var window = CreateWindow(config);

        // Mock ModelValidator to return invalid
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_invalidModelPath, It.IsAny<string>(), null))
            .Returns((false, "Hash mismatch"));
        window.SetModelValidator(mockValidator.Object);

        // Act
        window.VerifyModel();

        // Assert
        window.ModelStatusText.Text.Should().Contain("Modell ungültig");
        window.ModelStatusText.Foreground.Should().Be(System.Windows.Media.Brushes.Red);
    }

    [StaFact]
    public void VerifyModel_ShowsProgressDialog()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _validModelPath;
        var window = CreateWindow(config);

        // Act
        var progressShown = false;
        window.OnProgressDialogShown += () => progressShown = true;
        window.VerifyModel();

        // Assert
        progressShown.Should().BeTrue("progress dialog should be shown during verification");
    }

    [StaFact]
    public void ChangeModel_ValidFile_UpdatesPath()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Mock ModelValidator to return valid
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_validModelPath, It.IsAny<string>(), null))
            .Returns((true, "Hash matches"));
        window.SetModelValidator(mockValidator.Object);

        // Act - Use synchronous helper to avoid async issues
        window.SetModelPathSync(_validModelPath);

        // Assert
        window.CurrentModelPath.Should().Be(_validModelPath);
        window.ModelPathText.Text.Should().Contain(_validModelPath);
    }

    [StaFact]
    public void ChangeModel_InvalidHash_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Mock ModelValidator to return invalid
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_invalidModelPath, It.IsAny<string>(), null))
            .Returns((false, "Hash mismatch"));
        window.SetModelValidator(mockValidator.Object);

        // Act - Set path and manually verify (no auto-verification to avoid async issues)
        window.SetModelPathSync(_invalidModelPath);
        window.VerifyModel();  // Manually trigger verification

        // Assert
        window.HasValidationErrors.Should().BeTrue();
        window.ModelStatusText.Text.Should().Contain("ungültig");
        window.SaveButton.IsEnabled.Should().BeFalse("validation error exists");
    }

    [StaFact]
    public void SaveModelChange_NoRestartRequired()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Mock ModelValidator
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_validModelPath, It.IsAny<string>(), null))
            .Returns((true, "Hash matches"));
        window.SetModelValidator(mockValidator.Object);

        // Act - Only change model path, use synchronous helper
        window.SetModelPathSync(_validModelPath);
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeFalse("model path change does NOT require restart");
    }

    [StaFact]
    public void ModelPathChange_UpdatesConfigCorrectly()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Mock ModelValidator
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_validModelPath, It.IsAny<string>(), null))
            .Returns((true, "Hash matches"));
        window.SetModelValidator(mockValidator.Object);

        // Act - Use synchronous helper to avoid async issues
        window.SetModelPathSync(_validModelPath);
        var updatedConfig = window.BuildConfig();

        // Assert
        updatedConfig.Whisper.ModelPath.Should().Be(_validModelPath);
    }

    // Helper Methods

    private AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            Hotkey = new HotkeyConfig { Modifiers = new List<string> { "Ctrl", "Shift" }, Key = "D" },
            DataRoot = "C:\\Test\\Data",
            Language = "de",
            FileFormat = ".md",
            Whisper = new WhisperConfig { ModelPath = "C:\\Test\\model.bin" }
        };
    }
}
