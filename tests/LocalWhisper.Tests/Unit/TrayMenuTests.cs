using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.UI.Settings;
using LocalWhisper.UI.TrayIcon;
using Moq;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for tray menu integration with Settings.
/// </summary>
/// <remarks>
/// Tests for US-054: Settings Window - Access and Navigation
/// See: docs/iterations/iteration-06-settings.md (TrayMenuTests section)
/// See: docs/ui/settings-window-specification.md (Access section)
/// </remarks>
public class TrayMenuTests
{
    public TrayMenuTests()
    {
        // Initialize AppLogger with Error level to reduce test output verbosity
        var testDir = Path.Combine(Path.GetTempPath(), "LocalWhisperTests_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        LocalWhisper.Core.AppLogger.Initialize(testDir, Serilog.Events.LogEventLevel.Error);
    }

    [Fact]
    public void RightClickTray_ShowsMenu()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");

        // Act
        var contextMenu = trayManager.GetContextMenu();

        // Assert
        contextMenu.Should().NotBeNull();
        contextMenu.Items.Count.Should().Be(3, "menu should have Settings, History, Exit");
    }

    [Fact]
    public void TrayMenu_HasCorrectMenuItems()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");

        // Act
        var contextMenu = trayManager.GetContextMenu();
        var menuItemHeaders = contextMenu.Items.Cast<System.Windows.Controls.MenuItem>()
            .Select(item => item.Header.ToString())
            .ToList();

        // Assert
        menuItemHeaders.Should().Contain("Einstellungen", "Settings menu item should exist");
        menuItemHeaders.Should().Contain("History", "History menu item should exist");
        menuItemHeaders.Should().Contain("Beenden", "Exit menu item should exist");
    }

    [Fact]
    public void ClickSettings_OpensSettingsWindow()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");
        var config = CreateDefaultConfig();

        SettingsWindow? openedWindow = null;
        trayManager.OnSettingsOpened += (window) => openedWindow = window;

        // Act
        trayManager.OpenSettings(config, "C:\\Test\\config.toml");

        // Assert
        openedWindow.Should().NotBeNull("Settings window should be opened");
        openedWindow!.Title.Should().Contain("Einstellungen");
    }

    [Fact]
    public void ClickHistory_OpensExplorerToHistoryFolder()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");
        var historyPath = "C:\\Test\\Data\\history";

        string? openedPath = null;
        trayManager.OnExplorerOpened += (path) => openedPath = path;

        // Act
        trayManager.OpenHistory(historyPath);

        // Assert
        openedPath.Should().Be(historyPath, "Explorer should open to history folder");
    }

    [Fact]
    public void ClickExit_ClosesApp()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");

        var exitCalled = false;
        trayManager.OnExitRequested += () => exitCalled = true;

        // Act
        trayManager.Exit();

        // Assert
        exitCalled.Should().BeTrue("app exit should be triggered");
    }

    [Fact]
    public void TrayMenu_MenuOrder_IsCorrect()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");

        // Act
        var contextMenu = trayManager.GetContextMenu();
        var menuItems = contextMenu.Items.Cast<System.Windows.Controls.MenuItem>().ToList();

        // Assert
        menuItems[0].Header.ToString().Should().Be("Einstellungen", "first item should be Settings");
        menuItems[1].Header.ToString().Should().Be("History", "second item should be History");
        menuItems[2].Header.ToString().Should().Be("Beenden", "third item should be Exit");
    }

    [Fact]
    public void OpenSettings_MultipleTimes_OnlyOneWindowOpen()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");
        var config = CreateDefaultConfig();

        // Act
        var window1 = trayManager.OpenSettings(config, "C:\\Test\\config.toml");
        var window2 = trayManager.OpenSettings(config, "C:\\Test\\config.toml");

        // Assert
        window1.Should().BeSameAs(window2, "should return same window instance if already open");
    }

    [Fact]
    public void HistoryMenuItem_DataRootNotConfigured_DisablesMenuItem()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");
        trayManager.SetDataRoot(null); // No data root configured

        // Act
        var contextMenu = trayManager.GetContextMenu();
        var historyMenuItem = contextMenu.Items.Cast<System.Windows.Controls.MenuItem>()
            .First(item => item.Header.ToString() == "History");

        // Assert
        historyMenuItem.IsEnabled.Should().BeFalse("History menu item should be disabled when data root is not configured");
    }

    [Fact]
    public void SettingsMenuItem_AlwaysEnabled()
    {
        // Arrange
        var mockStateMachine = new Mock<StateMachine>();
        var trayManager = new TrayIconManager(mockStateMachine.Object, "C:\\Test\\config.toml", "C:\\Test\\Data");

        // Act
        var contextMenu = trayManager.GetContextMenu();
        var settingsMenuItem = contextMenu.Items.Cast<System.Windows.Controls.MenuItem>()
            .First(item => item.Header.ToString() == "Einstellungen");

        // Assert
        settingsMenuItem.IsEnabled.Should().BeTrue("Settings menu item should always be enabled");
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
