using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.UI.Settings;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for hotkey modification in Settings window.
/// </summary>
/// <remarks>
/// Tests for US-050: Settings - Hotkey Change
/// See: docs/iterations/iteration-06-settings.md (HotkeyChangeTests section)
/// See: docs/ui/settings-window-specification.md (Hotkey Section)
/// </remarks>
public class HotkeyChangeTests : IDisposable
{
    private readonly List<System.Windows.Window> _windows = new();
    public HotkeyChangeTests()
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
    public void ChangeHotkey_Valid_UpdatesField()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act
        window.SetHotkey("Ctrl", "Alt", "D");

        // Assert
        window.CurrentHotkey.Should().Be("Ctrl+Alt+D");
        window.HotkeyTextBox.Text.Should().Be("Ctrl+Alt+D");
        window.HasHotkeyConflict.Should().BeFalse("no conflict detected");
    }

    [StaFact]
    public void ChangeHotkey_Conflict_ShowsWarning()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act - Simulate hotkey conflict (e.g., Ctrl+Alt+Del is system hotkey)
        window.SetHotkey("Ctrl", "Alt", "Del");

        // Assert
        window.HasHotkeyConflict.Should().BeTrue();
        window.HotkeyWarningText.Text.Should().Contain("Hotkey bereits belegt");
        window.HotkeyWarningText.Visibility.Should().Be(System.Windows.Visibility.Visible);
        // Warning should NOT disable Save button (it's a warning, not an error)
        window.SaveButton.IsEnabled.Should().BeTrue();
    }

    [StaFact]
    public void ChangeHotkey_NoModifier_ShowsError()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act - Try to set hotkey without modifier
        window.SetHotkey(null, null, "D");

        // Assert
        window.HasValidationErrors.Should().BeTrue();
        window.HotkeyErrorText.Text.Should().Contain("Mindestens ein Modifier");
        window.HotkeyErrorText.Visibility.Should().Be(System.Windows.Visibility.Visible);
        window.SaveButton.IsEnabled.Should().BeFalse("validation error exists");
    }

    [StaFact]
    public void SaveHotkeyChange_RequiresRestart()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);
        window.SetHotkey("Ctrl", "Alt", "V");

        // Act
        var requiresRestart = window.RequiresRestart();

        // Assert
        requiresRestart.Should().BeTrue("hotkey change requires restart");
    }

    [StaFact]
    public void HotkeyChange_FromCtrlShiftD_ToCtrlAltD_UpdatesConfig()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Hotkey.Modifiers = new List<string> { "Ctrl", "Shift" };
        config.Hotkey.Key = "D";
        var window = CreateWindow(config);

        // Act
        window.SetHotkey("Ctrl", "Alt", "D");
        var updatedConfig = window.BuildConfig();

        // Assert
        updatedConfig.Hotkey.Modifiers.Should().BeEquivalentTo(new[] { "Ctrl", "Alt" });
        updatedConfig.Hotkey.Key.Should().Be("D");
    }

    [StaFact]
    public void HotkeyTextBox_DisplaysFormatted_WithPlusSigns()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Hotkey.Modifiers = new List<string> { "Ctrl", "Shift", "Alt" };
        config.Hotkey.Key = "F12";
        var window = CreateWindow(config);

        // Act & Assert
        window.HotkeyTextBox.Text.Should().Be("Ctrl+Shift+Alt+F12");
    }

    // =============================================================================
    // ENHANCEMENT TESTS (US-057): In-Place Hotkey Capture
    // =============================================================================

    [StaFact]
    public void HotkeyCapture_EntersCaptureMode()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var window = CreateWindow(config);

        // Act
        window.EnterHotkeyCaptureMode();

        // Assert
        window.IsHotkeyCaptureMode.Should().BeTrue();
        window.HotkeyTextBox.Background.Should().Be(System.Windows.Media.Brushes.LightYellow);
        window.HotkeyTextBox.Text.Should().Be("Drücke Tastenkombination...");
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
