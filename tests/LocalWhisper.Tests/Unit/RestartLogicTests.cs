using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.UI.Settings;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for restart dialog behavior in Settings.
/// </summary>
/// <remarks>
/// Tests for US-055: Settings - Save and Cancel Behavior (Restart logic)
/// See: docs/iterations/iteration-06-settings.md (RestartLogicTests section)
/// See: docs/ui/settings-window-specification.md (Restart Dialog section)
/// </remarks>
[Trait("Batch", "5")]
public class RestartLogicTests
{
    [Fact]
    public void SaveHotkeyChange_ShowsRestartDialog()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.SetHotkey("Ctrl", "Alt", "V");

        // Act
        window.Save();

        // Assert
        window.RestartDialogShown.Should().BeTrue("hotkey change requires restart");
    }

    [Fact]
    public void SaveLanguageChange_ShowsRestartDialog()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.LanguageEnglish.IsChecked = true;

        // Act
        window.Save();

        // Assert
        window.RestartDialogShown.Should().BeTrue("language change requires restart");
    }

    [Fact]
    public void SaveDataRootChange_ShowsRestartDialog()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Create valid test data root
        var newDataRoot = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_Restart", Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(newDataRoot, "config"));
        Directory.CreateDirectory(Path.Combine(newDataRoot, "models"));

        try
        {
            window.SetDataRoot(newDataRoot);

            // Act
            window.Save();

            // Assert
            window.RestartDialogShown.Should().BeTrue("data root change requires restart");
        }
        finally
        {
            if (Directory.Exists(newDataRoot))
                Directory.Delete(newDataRoot, recursive: true);
        }
    }

    [Fact]
    public void SaveFileFormatChange_NoRestartDialog()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".md";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.FileFormatTxt.IsChecked = true;

        // Act
        window.Save();

        // Assert
        window.RestartDialogShown.Should().BeFalse("file format change does NOT require restart");
    }

    [Fact]
    public void SaveMultipleChanges_OneRestartDialog()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        var restartDialogCount = 0;
        window.OnRestartDialogShown += () => restartDialogCount++;

        // Act - Make multiple changes that require restart
        window.SetHotkey("Ctrl", "Alt", "V"); // Requires restart
        window.LanguageEnglish.IsChecked = true; // Requires restart
        window.Save();

        // Assert
        restartDialogCount.Should().Be(1, "only ONE restart dialog should be shown");
    }

    [Fact]
    public void RestartDialog_Yes_RestartsApp()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.LanguageEnglish.IsChecked = true;

        var restartCalled = false;
        window.OnRestartRequested += () => restartCalled = true;

        // Act
        window.Save();
        window.SimulateRestartDialogYes();

        // Assert
        restartCalled.Should().BeTrue("app restart should be triggered");
    }

    [Fact]
    public void RestartDialog_No_SavesButNoRestart()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.LanguageEnglish.IsChecked = true;

        var restartCalled = false;
        window.OnRestartRequested += () => restartCalled = true;

        // Act
        window.Save();
        window.SimulateRestartDialogNo();

        // Assert
        restartCalled.Should().BeFalse("app restart should NOT be triggered");
        window.IsClosed.Should().BeTrue("window should close");
    }

    [Fact]
    public void RequiresRestart_HotkeyChange_ReturnsTrue()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.SetHotkey("Ctrl", "Alt", "V");
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeTrue();
    }

    [Fact]
    public void RequiresRestart_LanguageChange_ReturnsTrue()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.LanguageEnglish.IsChecked = true;
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeTrue();
    }

    [Fact]
    public void RequiresRestart_FileFormatChange_ReturnsFalse()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".md";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.FileFormatTxt.IsChecked = true;
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeFalse();
    }

    [Fact]
    public void RequiresRestart_ModelPathChange_ReturnsFalse()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.SetModelPath("C:\\New\\Path\\model.bin");
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeFalse();
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
