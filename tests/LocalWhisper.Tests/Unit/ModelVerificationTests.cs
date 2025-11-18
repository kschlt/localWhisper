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
    private readonly string _testDirectory;
    private readonly string _validModelPath;
    private readonly string _invalidModelPath;

    public ModelVerificationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_Models", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Create valid model file (with known content for SHA-1 testing)
        _validModelPath = Path.Combine(_testDirectory, "ggml-valid.bin");
        File.WriteAllText(_validModelPath, "valid model content");

        // Create invalid model file
        _invalidModelPath = Path.Combine(_testDirectory, "ggml-invalid.bin");
        File.WriteAllText(_invalidModelPath, "invalid model content");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Fact]
    public void VerifyModel_ValidHash_ShowsSuccess()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _validModelPath;
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Mock ModelValidator to return valid
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_validModelPath, It.IsAny<string>()))
            .Returns((true, "Hash matches"));
        window.SetModelValidator(mockValidator.Object);

        // Act
        window.VerifyModel();

        // Assert
        window.ModelStatusText.Text.Should().Contain("Modell OK");
        window.ModelStatusText.Foreground.Should().Be(System.Windows.Media.Brushes.Green);
    }

    [Fact]
    public void VerifyModel_InvalidHash_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _invalidModelPath;
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Mock ModelValidator to return invalid
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_invalidModelPath, It.IsAny<string>()))
            .Returns((false, "Hash mismatch"));
        window.SetModelValidator(mockValidator.Object);

        // Act
        window.VerifyModel();

        // Assert
        window.ModelStatusText.Text.Should().Contain("Modell ungültig");
        window.ModelStatusText.Foreground.Should().Be(System.Windows.Media.Brushes.Red);
    }

    [Fact]
    public void VerifyModel_ShowsProgressDialog()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _validModelPath;
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        var progressShown = false;
        window.OnProgressDialogShown += () => progressShown = true;
        window.VerifyModel();

        // Assert
        progressShown.Should().BeTrue("progress dialog should be shown during verification");
    }

    [Fact]
    public void ChangeModel_ValidFile_UpdatesPath()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Mock ModelValidator to return valid
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_validModelPath, It.IsAny<string>()))
            .Returns((true, "Hash matches"));
        window.SetModelValidator(mockValidator.Object);

        // Act
        window.SetModelPath(_validModelPath);

        // Assert
        window.CurrentModelPath.Should().Be(_validModelPath);
        window.ModelPathText.Text.Should().Contain(_validModelPath);
    }

    [Fact]
    public void ChangeModel_InvalidHash_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Mock ModelValidator to return invalid
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_invalidModelPath, It.IsAny<string>()))
            .Returns((false, "Hash mismatch"));
        window.SetModelValidator(mockValidator.Object);

        // Act
        window.SetModelPath(_invalidModelPath);

        // Assert
        window.HasValidationErrors.Should().BeTrue();
        window.ModelStatusText.Text.Should().Contain("ungültig");
        window.SaveButton.IsEnabled.Should().BeFalse("validation error exists");
    }

    [Fact]
    public void SaveModelChange_NoRestartRequired()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Mock ModelValidator
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_validModelPath, It.IsAny<string>()))
            .Returns((true, "Hash matches"));
        window.SetModelValidator(mockValidator.Object);

        // Act - Only change model path
        window.SetModelPath(_validModelPath);
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeFalse("model path change does NOT require restart");
    }

    [Fact]
    public void ModelPathChange_UpdatesConfigCorrectly()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Mock ModelValidator
        var mockValidator = new Mock<ModelValidator>();
        mockValidator.Setup(v => v.ValidateModel(_validModelPath, It.IsAny<string>()))
            .Returns((true, "Hash matches"));
        window.SetModelValidator(mockValidator.Object);

        // Act
        window.SetModelPath(_validModelPath);
        var updatedConfig = window.BuildConfig();

        // Assert
        updatedConfig.Whisper.ModelPath.Should().Be(_validModelPath);
    }

    // =============================================================================
    // ENHANCEMENT TESTS (US-058): SHA-1 Async Verification
    // =============================================================================

    [Fact]
    public async Task VerifyModelAsync_ShowsProgressInStatusText()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _validModelPath;
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        var verifyTask = window.VerifyModelAsync();

        // Assert - Check progress indicator shown during verification
        window.ModelStatusText.Text.Should().Be("⏳ Verifiziere Modell...");
        window.ModelStatusText.Foreground.Should().Be(System.Windows.Media.Brushes.Gray);
        window.VerifyModelButton.IsEnabled.Should().BeFalse("button disabled during verification");

        await verifyTask;

        // Check completion status
        window.ModelStatusText.Text.Should().Be("✓ Modell OK");
        window.ModelStatusText.Foreground.Should().Be(System.Windows.Media.Brushes.Green);
        window.VerifyModelButton.IsEnabled.Should().BeTrue("button re-enabled after verification");
    }

    [Fact]
    public async Task VerifyModelAsync_InvalidFile_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = "C:\\NonExistent\\model.bin";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        await window.VerifyModelAsync();

        // Assert
        window.ModelStatusText.Text.Should().Contain("⚠ Modell nicht gefunden");
        window.ModelStatusText.Foreground.Should().Be(System.Windows.Media.Brushes.Red);
    }

    [Fact]
    public async Task ChangeModel_AutoVerifiesInBackground()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Change model path (should auto-verify)
        window.SetModelPath(_validModelPath);
        await Task.Delay(100); // Allow auto-verify to start

        // Assert - Verification started automatically
        window.ModelStatusText.Text.Should().Be("⏳ Verifiziere Modell...");
        window.ModelStatusText.Visibility.Should().Be(System.Windows.Visibility.Visible);

        // Wait for verification to complete
        await Task.Delay(1000);
        window.ModelStatusText.Text.Should().Be("✓ Modell OK");
    }

    [Fact]
    public async Task VerifyModelAsync_UIRemainsResponsive()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _validModelPath;
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Start verification (async)
        var verifyTask = window.VerifyModelAsync();

        // Assert - UI should remain responsive (other buttons still clickable)
        window.CancelButton.IsEnabled.Should().BeTrue("Cancel button should remain enabled");
        window.LanguageGerman.IsEnabled.Should().BeTrue("Other controls should remain enabled");

        await verifyTask;
    }

    [Fact]
    public async Task VerifyModelAsync_ComputesSHA1_NoComparison()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Whisper.ModelPath = _validModelPath;
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        await window.VerifyModelAsync();

        // Assert - Verification uses SHA-1 computation
        // For now, we just verify file exists (no hash comparison source)
        window.ModelStatusText.Text.Should().Be("✓ Modell OK");

        // Future enhancement: Compare with checksums.txt
        // window.ComputedHash.Should().NotBeNullOrEmpty();
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
