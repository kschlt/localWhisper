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

///
/// SKIPPED: WPF integration tests disabled for v0.1 due to window lifecycle issues.
/// Coverage: Manual testing (see docs/testing/manual-test-script-iter6.md)
/// Refactor: Will be converted to ViewModel tests in v1.0 (see tests/README.md)
/// </remarks>
[Trait("Category", "WpfIntegration")]
public class RestartLogicTests
{
    public RestartLogicTests()
    {
        // Initialize AppLogger with Error level to reduce test output verbosity
        var testDir = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        LocalWhisper.Core.AppLogger.Initialize(testDir, Serilog.Events.LogEventLevel.Error);
    }

    [StaFact]
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

    [StaFact]
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

    [StaFact]
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

    [StaFact]
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

    [StaFact]
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

    [StaFact]
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

    [StaFact]
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

    [StaFact]
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

    [StaFact]
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

    [StaFact]
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

    [StaFact]
    public void RequiresRestart_ModelPathChange_ReturnsFalse()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Create temporary model file
        var tempModelPath = Path.Combine(Path.GetTempPath(), $"test-model-{Guid.NewGuid()}.bin");
        File.WriteAllText(tempModelPath, "test model");

        try
        {
            // Act - Use synchronous helper to avoid MessageBox hang
            window.SetModelPathSync(tempModelPath);
            var requiresRestart = window.RequiresRestart();

            // Assert
            requiresRestart.Should().BeFalse();
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempModelPath))
                File.Delete(tempModelPath);
        }
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
