using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.UI.Settings;
using Moq;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for Settings Window behavior.
/// </summary>
/// <remarks>
/// Tests for US-054, US-055: Settings window initialization, change detection, modal state.
/// See: docs/iterations/iteration-06-settings.md (SettingsWindowTests section)
/// See: docs/ui/settings-window-specification.md
/// </remarks>
public class SettingsWindowTests : IDisposable
{
    private readonly List<System.Windows.Window> _windows = new();
    public SettingsWindowTests()
    {
        // Initialize AppLogger with Error level to reduce test output verbosity
        var testDir = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        LocalWhisper.Core.AppLogger.Initialize(testDir, Serilog.Events.LogEventLevel.Error);
    }

    
    private SettingsWindow CreateWindow(AppConfig config, string configPath = "C:\\Test\\config.toml")
    {
        var window = new SettingsWindow(config, configPath);
        _windows.Add(window);
        return window;
    }

    public void Dispose()
    {
        foreach (var window in _windows)
        {
            try { window.Close(); } catch { }
        }
    }

    [StaFact]
    public void OpenSettings_LoadsCurrentConfig_PopulatesFields()
    {
        // Arrange
        var config = new AppConfig
        {
            Hotkey = new HotkeyConfig { Modifiers = new List<string> { "Ctrl", "Shift" }, Key = "D" },
            DataRoot = "C:\\Test\\Data",
            Language = "de",
            FileFormat = ".md",
            Whisper = new WhisperConfig { ModelPath = "C:\\Test\\model.bin" }
        };

        // Act
        var window = CreateWindow(config);

        // Assert
        window.Should().NotBeNull();
        window.CurrentHotkey.Should().Be("Ctrl+Shift+D");
        window.CurrentDataRoot.Should().Be("C:\\Test\\Data");
        window.CurrentLanguage.Should().Be("de");
        window.CurrentFileFormat.Should().Be(".md");
        window.CurrentModelPath.Should().Be("C:\\Test\\model.bin");
    }

    [StaFact]
    public void OpenSettings_NoChanges_SaveButtonDisabled()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act & Assert
        window.SaveButton.IsEnabled.Should().BeFalse("no changes have been made yet");
    }

    [StaFact]
    public void OpenSettings_WindowIsModal_BlocksAppInteraction()
    {
        // Arrange
        var config = CreateDefaultConfig();

        // Act
        var window = CreateWindow(config);

        // Assert
        window.ShowInTaskbar.Should().BeFalse();
        window.WindowStartupLocation.Should().Be(System.Windows.WindowStartupLocation.CenterScreen);
        window.ResizeMode.Should().Be(System.Windows.ResizeMode.NoResize);
        window.Width.Should().Be(500);
        window.Height.Should().Be(600);
    }

    [StaFact]
    public void OpenSettings_ShowsVersionNumber_BottomLeft()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act
        var versionText = window.VersionText.Text;

        // Assert
        versionText.Should().MatchRegex(@"^v\d+\.\d+\.\d+$", "version should be in format vX.Y.Z");
    }

    [StaFact]
    public void ChangeAnyField_EnablesSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);
        window.SaveButton.IsEnabled.Should().BeFalse("initially disabled");

        // Act - Simulate changing language
        window.LanguageEnglish.IsChecked = true;

        // Assert
        window.SaveButton.IsEnabled.Should().BeTrue("a change was detected");
    }

    [StaFact]
    public void RevertChanges_DisablesSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";
        var window = CreateWindow(config);

        // Act - Change then revert
        window.LanguageEnglish.IsChecked = true; // Change to English
        window.SaveButton.IsEnabled.Should().BeTrue();
        window.LanguageGerman.IsChecked = true; // Revert to German

        // Assert
        window.SaveButton.IsEnabled.Should().BeFalse("changes were reverted to original");
    }

    [StaFact]
    public void ValidationError_DisablesSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act - Set invalid data root
        window.SetDataRoot("C:\\NonExistent\\Invalid");

        // Assert
        window.HasValidationErrors.Should().BeTrue();
        window.SaveButton.IsEnabled.Should().BeFalse("validation error exists");
    }

    [StaFact]
    public void HasChanges_ReturnsTrueWhenFieldsDiffer()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act
        window.LanguageEnglish.IsChecked = true;

        // Assert
        window.HasChanges().Should().BeTrue();
    }

    [StaFact]
    public void HasChanges_ReturnsFalseWhenFieldsMatch()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act & Assert
        window.HasChanges().Should().BeFalse("no changes made");
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
