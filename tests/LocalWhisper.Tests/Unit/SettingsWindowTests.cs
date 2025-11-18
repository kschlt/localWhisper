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
public class SettingsWindowTests
{
    [Fact]
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
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Assert
        window.Should().NotBeNull();
        window.CurrentHotkey.Should().Be("Ctrl+Shift+D");
        window.CurrentDataRoot.Should().Be("C:\\Test\\Data");
        window.CurrentLanguage.Should().Be("de");
        window.CurrentFileFormat.Should().Be(".md");
        window.CurrentModelPath.Should().Be("C:\\Test\\model.bin");
    }

    [Fact]
    public void OpenSettings_NoChanges_SaveButtonDisabled()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act & Assert
        window.SaveButton.IsEnabled.Should().BeFalse("no changes have been made yet");
    }

    [Fact]
    public void OpenSettings_WindowIsModal_BlocksAppInteraction()
    {
        // Arrange
        var config = CreateDefaultConfig();

        // Act
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Assert
        window.ShowInTaskbar.Should().BeFalse();
        window.WindowStartupLocation.Should().Be(System.Windows.WindowStartupLocation.CenterScreen);
        window.ResizeMode.Should().Be(System.Windows.ResizeMode.NoResize);
        window.Width.Should().Be(500);
        window.Height.Should().Be(600);
    }

    [Fact]
    public void OpenSettings_ShowsVersionNumber_BottomLeft()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        var versionText = window.VersionText.Text;

        // Assert
        versionText.Should().MatchRegex(@"^v\d+\.\d+\.\d+$", "version should be in format vX.Y.Z");
    }

    [Fact]
    public void ChangeAnyField_EnablesSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.SaveButton.IsEnabled.Should().BeFalse("initially disabled");

        // Act - Simulate changing language
        window.LanguageEnglish.IsChecked = true;

        // Assert
        window.SaveButton.IsEnabled.Should().BeTrue("a change was detected");
    }

    [Fact]
    public void RevertChanges_DisablesSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Language = "de";
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Change then revert
        window.LanguageEnglish.IsChecked = true; // Change to English
        window.SaveButton.IsEnabled.Should().BeTrue();
        window.LanguageGerman.IsChecked = true; // Revert to German

        // Assert
        window.SaveButton.IsEnabled.Should().BeFalse("changes were reverted to original");
    }

    [Fact]
    public void ValidationError_DisablesSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Set invalid data root
        window.SetDataRoot("C:\\NonExistent\\Invalid");

        // Assert
        window.HasValidationErrors.Should().BeTrue();
        window.SaveButton.IsEnabled.Should().BeFalse("validation error exists");
    }

    [Fact]
    public void HasChanges_ReturnsTrueWhenFieldsDiffer()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act
        window.LanguageEnglish.IsChecked = true;

        // Assert
        window.HasChanges().Should().BeTrue();
    }

    [Fact]
    public void HasChanges_ReturnsFalseWhenFieldsMatch()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act & Assert
        window.HasChanges().Should().BeFalse("no changes made");
    }

    // =============================================================================
    // ENHANCEMENT TESTS (US-059): Keyboard Shortcuts
    // =============================================================================

    [Fact]
    public void KeyboardShortcut_EnterKey_TriggersSave_WhenEnabled()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.LanguageEnglish.IsChecked = true; // Make a change to enable Save

        // Act - Simulate Enter keypress
        window.SimulateKeyPress(System.Windows.Input.Key.Enter);

        // Assert - Save should be triggered (dialog shown or window closed)
        window.SaveAttempted.Should().BeTrue("Enter key should trigger Save when enabled");
    }

    [Fact]
    public void KeyboardShortcut_EnterKey_DoesNothing_WhenDisabled()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.SaveButton.IsEnabled.Should().BeFalse("no changes made yet");

        // Act - Simulate Enter keypress
        window.SimulateKeyPress(System.Windows.Input.Key.Enter);

        // Assert - Save should NOT be triggered
        window.SaveAttempted.Should().BeFalse("Enter key should do nothing when Save disabled");
    }

    [Fact]
    public void KeyboardShortcut_EscKey_TriggersCancel_WithConfirmation()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.LanguageEnglish.IsChecked = true; // Make a change

        // Act - Simulate Esc keypress
        window.SimulateKeyPress(System.Windows.Input.Key.Escape);

        // Assert - Confirmation dialog should appear
        window.CancelConfirmationShown.Should().BeTrue("Esc key should show confirmation when changes exist");
    }

    [Fact]
    public void KeyboardShortcut_EscKey_ClosesImmediately_NoChanges()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.HasChanges().Should().BeFalse();

        // Act - Simulate Esc keypress
        window.SimulateKeyPress(System.Windows.Input.Key.Escape);

        // Assert - Window should close immediately without confirmation
        window.CancelConfirmationShown.Should().BeFalse("No confirmation needed when no changes");
        window.CloseAttempted.Should().BeTrue("Window should close immediately");
    }

    [Fact]
    public void KeyboardShortcut_AltS_TriggersSaveButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");
        window.LanguageEnglish.IsChecked = true; // Enable Save button

        // Act - Simulate Alt+S keypress
        window.SimulateKeyPress(System.Windows.Input.Key.S, alt: true);

        // Assert
        window.SaveAttempted.Should().BeTrue("Alt+S should trigger Save button");
    }

    [Fact]
    public void KeyboardShortcut_AltA_TriggersCancelButton()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Simulate Alt+A keypress
        window.SimulateKeyPress(System.Windows.Input.Key.A, alt: true);

        // Assert
        window.CancelAttempted.Should().BeTrue("Alt+A should trigger Cancel button");
    }

    [Fact]
    public void KeyboardShortcut_AltD_TriggersBrowseDataRoot()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Simulate Alt+D keypress
        window.SimulateKeyPress(System.Windows.Input.Key.D, alt: true);

        // Assert
        window.BrowseDataRootAttempted.Should().BeTrue("Alt+D should trigger Browse Data Root button");
    }

    [Fact]
    public void KeyboardShortcut_AltP_TriggersVerifyModel()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Simulate Alt+P keypress
        window.SimulateKeyPress(System.Windows.Input.Key.P, alt: true);

        // Assert
        window.VerifyModelAttempted.Should().BeTrue("Alt+P should trigger Verify Model button");
    }

    [Fact]
    public void KeyboardShortcut_AltN_TriggersChangeHotkey()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = new SettingsWindow(config, "C:\\Test\\config.toml");

        // Act - Simulate Alt+N keypress (for "Ändern..." button)
        window.SimulateKeyPress(System.Windows.Input.Key.N, alt: true);

        // Assert
        window.ChangeHotkeyAttempted.Should().BeTrue("Alt+N should trigger Ändern (Change Hotkey) button");
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
