using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.UI.Settings;
using Moq;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for saving settings to config.toml.
/// </summary>
/// <remarks>
/// Tests for US-055: Settings - Save and Cancel Behavior (Save part)
/// Tests for US-056: Settings - Validation and Error Handling (Config save failure)
/// See: docs/iterations/iteration-06-settings.md (SettingsPersistenceTests section)

///
/// SKIPPED: WPF integration tests disabled for v0.1 due to window lifecycle issues.
/// Coverage: Manual testing (see docs/testing/manual-test-script-iter6.md)
/// Refactor: Will be converted to ViewModel tests in v1.0 (see tests/README.md)
/// </remarks>
[Trait("Category", "WpfIntegration")]
public class SettingsPersistenceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testConfigPath;

    public SettingsPersistenceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_Persistence", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _testConfigPath = Path.Combine(_testDirectory, "config.toml");
        // Initialize AppLogger with Error level to reduce test output verbosity
        LocalWhisper.Core.AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [StaFact]
    public void Save_WritesToConfigToml()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, _testConfigPath);
        window.LanguageEnglish.IsChecked = true;

        // Act
        window.Save();

        // Assert
        File.Exists(_testConfigPath).Should().BeTrue("config file should be created");
        var savedContent = File.ReadAllText(_testConfigPath);
        savedContent.Should().Contain("language = \"en\"");
    }

    [StaFact]
    public void Save_MultipleChanges_AllPersisted()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, _testConfigPath);

        // Act - Make multiple changes
        window.LanguageEnglish.IsChecked = true;
        window.FileFormatTxt.IsChecked = true;
        window.Save();

        // Assert
        var savedContent = File.ReadAllText(_testConfigPath);
        savedContent.Should().Contain("language = \"en\"");
        savedContent.Should().Contain("file_format = \".txt\"");
    }

    [StaFact]
    public void Save_WriteFailure_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var readOnlyPath = Path.Combine(_testDirectory, "readonly_config.toml");
        File.WriteAllText(readOnlyPath, "existing content");
        File.SetAttributes(readOnlyPath, FileAttributes.ReadOnly);

        var window = new SettingsWindow(config, readOnlyPath);
        window.LanguageEnglish.IsChecked = true;

        // Act
        var saveResult = window.Save();

        // Assert
        saveResult.Should().BeFalse("save should fail");
        window.LastErrorMessage.Should().Contain("Fehler beim Speichern");

        // Cleanup
        File.SetAttributes(readOnlyPath, FileAttributes.Normal);
    }

    [StaFact]
    public void Save_ValidatesBeforeSaving()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, _testConfigPath);
        window.SetDataRoot("C:\\NonExistent\\Path");

        // Act
        var saveResult = window.Save();

        // Assert
        saveResult.Should().BeFalse("save should fail due to validation error");
        File.Exists(_testConfigPath).Should().BeFalse("config file should NOT be created");
    }

    [StaFact]
    public void Cancel_NoChanges_ClosesImmediately()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, _testConfigPath);

        // Act
        var closeResult = window.Cancel();

        // Assert
        closeResult.Should().BeTrue("window should close immediately");
        window.ConfirmationDialogShown.Should().BeFalse("no confirmation should be shown");
    }

    [StaFact]
    public void Cancel_WithChanges_ShowsConfirmation()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, _testConfigPath);
        window.LanguageEnglish.IsChecked = true; // Make a change

        // Act
        var closeResult = window.Cancel();

        // Assert
        window.ConfirmationDialogShown.Should().BeTrue("confirmation dialog should be shown");
    }

    [StaFact]
    public void Save_LogsSettingsChanges()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, _testConfigPath);
        window.LanguageEnglish.IsChecked = true;

        var logMessages = new List<string>();
        window.OnLogMessage += (msg) => logMessages.Add(msg);

        // Act
        window.Save();

        // Assert
        logMessages.Should().Contain(msg => msg.Contains("Settings changed"));
        logMessages.Should().Contain(msg => msg.Contains("Language"));
    }

    [StaFact]
    public void Save_ClosesWindowAfterSuccess()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, _testConfigPath);
        window.FileFormatTxt.IsChecked = true; // Change that doesn't require restart

        // Act
        var saveResult = window.Save();

        // Assert
        saveResult.Should().BeTrue("save should succeed");
        window.IsClosed.Should().BeTrue("window should close after save");
    }

    [StaFact]
    public void Save_WithRestartRequired_ShowsRestartDialog()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, _testConfigPath);
        window.LanguageEnglish.IsChecked = true; // Language change requires restart

        // Act
        var saveResult = window.Save();

        // Assert
        saveResult.Should().BeTrue("save should succeed");
        window.RestartDialogShown.Should().BeTrue("restart dialog should be shown");
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
