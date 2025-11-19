using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.UI.Settings;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for file format switching in Settings window.
/// </summary>
/// <remarks>
/// Tests for US-052: Settings - Language and Format (File Format part)
/// See: docs/iterations/iteration-06-settings.md (FileFormatChangeTests section)
/// See: docs/ui/settings-window-specification.md (File Format Section)
/// </remarks>
public class FileFormatChangeTests
{
    public FileFormatChangeTests()
    {
        // Initialize AppLogger with Error level to reduce test output verbosity
        var testDir = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        LocalWhisper.Core.AppLogger.Initialize(testDir, Serilog.Events.LogEventLevel.Error);
    }

    [Fact]
    public void ChangeFileFormat_MarkdownToTxt_UpdatesConfig()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".md";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.FileFormatTxt.IsChecked = true;
        var updatedConfig = window.BuildConfig();

        // Assert
        updatedConfig.FileFormat.Should().Be(".txt");
        window.CurrentFileFormat.Should().Be(".txt");
    }

    [Fact]
    public void ChangeFileFormat_TxtToMarkdown_UpdatesConfig()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".txt";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.FileFormatMarkdown.IsChecked = true;
        var updatedConfig = window.BuildConfig();

        // Assert
        updatedConfig.FileFormat.Should().Be(".md");
        window.CurrentFileFormat.Should().Be(".md");
    }

    [Fact]
    public void SaveFileFormatChange_NoRestartRequired()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".md";
        config.Language = "de"; // Keep language same
        config.DataRoot = "C:\\Test\\Data"; // Keep data root same
        config.Hotkey = new HotkeyConfig { Modifiers = new List<string> { "Ctrl", "Shift" }, Key = "D" }; // Keep hotkey same
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Only change file format
        window.FileFormatTxt.IsChecked = true;
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeFalse("file format change does NOT require restart");
    }

    [Fact]
    public void FileFormatChange_EnablesSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".md";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.SaveButton.IsEnabled.Should().BeFalse("initially disabled");

        // Act
        window.FileFormatTxt.IsChecked = true;

        // Assert
        window.SaveButton.IsEnabled.Should().BeTrue("change detected");
    }

    [Fact]
    public void FileFormatRadioButtons_AreExclusive()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".md";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.FileFormatMarkdown.IsChecked.Should().BeTrue("initially Markdown");

        // Act
        window.FileFormatTxt.IsChecked = true;

        // Assert
        window.FileFormatTxt.IsChecked.Should().BeTrue();
        window.FileFormatMarkdown.IsChecked.Should().BeFalse("only one can be selected");
    }

    [Fact]
    public void InitialFileFormat_Markdown_SelectsCorrectRadioButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".md";

        // Act
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Assert
        window.FileFormatMarkdown.IsChecked.Should().BeTrue();
        window.FileFormatTxt.IsChecked.Should().BeFalse();
    }

    [Fact]
    public void InitialFileFormat_Txt_SelectsCorrectRadioButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.FileFormat = ".txt";

        // Act
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Assert
        window.FileFormatTxt.IsChecked.Should().BeTrue();
        window.FileFormatMarkdown.IsChecked.Should().BeFalse();
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
