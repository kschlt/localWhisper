using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.Services;
using LocalWhisper.UI.Settings;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for data root path changes in Settings window.
/// </summary>
/// <remarks>
/// Tests for US-051: Settings - Data Root Change
/// See: docs/iterations/iteration-06-settings.md (DataRootChangeTests section)
/// See: docs/ui/settings-window-specification.md (Data Root Section)
/// </remarks>
public class DataRootChangeTests : IDisposable
{
    private readonly string _validTestDataRoot;
    private readonly string _invalidTestDataRoot;

    public DataRootChangeTests()
    {
        // Create valid test data root structure
        _validTestDataRoot = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_Valid", Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_validTestDataRoot, "config"));
        Directory.CreateDirectory(Path.Combine(_validTestDataRoot, "models"));
        Directory.CreateDirectory(Path.Combine(_validTestDataRoot, "history"));
        Directory.CreateDirectory(Path.Combine(_validTestDataRoot, "logs"));
        Directory.CreateDirectory(Path.Combine(_validTestDataRoot, "tmp"));

        // Create invalid test data root (no subdirectories)
        _invalidTestDataRoot = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_Invalid", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_invalidTestDataRoot);
    }

    public void Dispose()
    {
        // Cleanup
        if (Directory.Exists(_validTestDataRoot))
            Directory.Delete(_validTestDataRoot, recursive: true);
        if (Directory.Exists(_invalidTestDataRoot))
            Directory.Delete(_invalidTestDataRoot, recursive: true);
    }

    [Fact]
    public void ChangeDataRoot_ValidPath_UpdatesField()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.SetDataRoot(_validTestDataRoot);

        // Assert
        window.CurrentDataRoot.Should().Be(_validTestDataRoot);
        window.DataRootTextBox.Text.Should().Be(_validTestDataRoot);
        window.HasValidationErrors.Should().BeFalse("path is valid");
    }

    [Fact]
    public void ChangeDataRoot_InvalidStructure_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.SetDataRoot(_invalidTestDataRoot);

        // Assert
        window.HasValidationErrors.Should().BeTrue();
        window.DataRootErrorText.Text.Should().Contain("keine gültige LocalWhisper-Installation");
        window.DataRootErrorText.Visibility.Should().Be(System.Windows.Visibility.Visible);
        window.SaveButton.IsEnabled.Should().BeFalse("validation error exists");
    }

    [Fact]
    public void ChangeDataRoot_NonExistent_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.SetDataRoot("C:\\NonExistent\\Path\\That\\Does\\Not\\Exist");

        // Assert
        window.HasValidationErrors.Should().BeTrue();
        window.DataRootErrorText.Text.Should().Contain("Pfad nicht gefunden");
        window.SaveButton.IsEnabled.Should().BeFalse("validation error exists");
    }

    [Fact]
    public void ChangeDataRoot_ValidatesUsingDataRootValidator()
    {
        // Arrange
        var validator = new DataRootValidator();
        var config = CreateDefaultConfig();
        config.Whisper = new WhisperConfig { ModelPath = Path.Combine(_validTestDataRoot, "models", "model.bin") };

        // Create model file
        File.WriteAllText(config.Whisper.ModelPath, "dummy model content");

        // Act
        var result = validator.Validate(_validTestDataRoot, config);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SaveDataRootChange_RequiresRestart()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.SetDataRoot(_validTestDataRoot);

        // Act
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeTrue("data root change requires restart");
    }

    [Fact]
    public void DataRootChange_UpdatesConfigCorrectly()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.SetDataRoot(_validTestDataRoot);
        var updatedConfig = window.BuildConfig();

        // Assert
        updatedConfig.DataRoot.Should().Be(_validTestDataRoot);
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
