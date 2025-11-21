using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.UI.Settings;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for language switching in Settings window.
/// </summary>
/// <remarks>
/// Tests for US-052: Settings - Language and Format (Language part)
/// See: docs/iterations/iteration-06-settings.md (LanguageChangeTests section)
/// See: docs/ui/settings-window-specification.md (Language Section)
/// </remarks>
public class LanguageChangeTests
{
    public LanguageChangeTests()
    {
        // Initialize AppLogger with Error level to reduce test output verbosity
        var testDir = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        LocalWhisper.Core.AppLogger.Initialize(testDir, Serilog.Events.LogEventLevel.Error);
    }

    [StaFact]
    public void ChangeLanguage_GermanToEnglish_UpdatesConfig()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.LanguageEnglish.IsChecked = true;
        var updatedConfig = window.BuildConfig();

        // Assert
        updatedConfig.Language.Should().Be("en");
        window.CurrentLanguage.Should().Be("en");
    }

    [StaFact]
    public void ChangeLanguage_EnglishToGerman_UpdatesConfig()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "en";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.LanguageGerman.IsChecked = true;
        var updatedConfig = window.BuildConfig();

        // Assert
        updatedConfig.Language.Should().Be("de");
        window.CurrentLanguage.Should().Be("de");
    }

    [StaFact]
    public void SaveLanguageChange_RequiresRestart()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.LanguageEnglish.IsChecked = true;
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeTrue("language change requires restart");
    }

    [StaFact]
    public void LanguageChange_EnablesSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.SaveButton.IsEnabled.Should().BeFalse("initially disabled");

        // Act
        window.LanguageEnglish.IsChecked = true;

        // Assert
        window.SaveButton.IsEnabled.Should().BeTrue("change detected");
    }

    [StaFact]
    public void LanguageRadioButtons_AreExclusive()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.LanguageGerman.IsChecked.Should().BeTrue("initially German");

        // Act
        window.LanguageEnglish.IsChecked = true;

        // Assert
        window.LanguageEnglish.IsChecked.Should().BeTrue();
        window.LanguageGerman.IsChecked.Should().BeFalse("only one can be selected");
    }

    [StaFact]
    public void InitialLanguage_German_SelectsCorrectRadioButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";

        // Act
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Assert
        window.LanguageGerman.IsChecked.Should().BeTrue();
        window.LanguageEnglish.IsChecked.Should().BeFalse();
    }

    [StaFact]
    public void InitialLanguage_English_SelectsCorrectRadioButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "en";

        // Act
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Assert
        window.LanguageEnglish.IsChecked.Should().BeTrue();
        window.LanguageGerman.IsChecked.Should().BeFalse();
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
