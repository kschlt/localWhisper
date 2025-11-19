using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for ConfigManager class.
/// </summary>
/// <remarks>
/// Tests cover minimal config.toml schema for Iteration 1:
/// - Load config from TOML file
/// - Save config to TOML file
/// - Handle missing file (return defaults)
/// - Validate config on load
/// - Handle invalid TOML syntax
///
/// See: docs/iterations/iteration-01-hotkey-skeleton.md (Config File section)
/// See: docs/specification/data-structures.md (lines 49-110)
/// </remarks>
[Trait("Batch", "2")]
public class ConfigManagerTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly string _testDirectory;

    public ConfigManagerTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _testConfigPath = Path.Combine(_testDirectory, "config.toml");

        // Initialize AppLogger for tests with Error level to reduce test output verbosity
        AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);
    }

    public void Dispose()
    {
        // Note: Don't call AppLogger.Shutdown() here - it's a static singleton and would
        // interfere with other tests running in parallel (xUnit default behavior).
        // Let Serilog auto-flush and release handles naturally.

        // Cleanup test directory - may fail if AppLogger still has file handles
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Expected if AppLogger still has log file open
                // Temp directories will be cleaned by OS eventually
            }
        }
    }

    [Fact]
    public void Load_ValidToml_ReturnsConfig()
    {
        // Arrange
        var tomlContent = @"
[hotkey]
modifiers = [""Ctrl"", ""Shift""]
key = ""D""
";
        File.WriteAllText(_testConfigPath, tomlContent);

        // Act
        var config = ConfigManager.Load(_testConfigPath);

        // Assert
        config.Should().NotBeNull();
        config.Hotkey.Should().NotBeNull();
        config.Hotkey.Modifiers.Should().BeEquivalentTo(new[] { "Ctrl", "Shift" });
        config.Hotkey.Key.Should().Be("D");
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaultConfig()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.toml");

        // Act
        var config = ConfigManager.Load(nonExistentPath);

        // Assert
        config.Should().NotBeNull();
        config.Hotkey.Should().NotBeNull();
        config.Hotkey.Modifiers.Should().BeEquivalentTo(new[] { "Ctrl", "Shift" }, "should return default modifiers");
        config.Hotkey.Key.Should().Be("D", "should return default key");
    }

    [Fact]
    public void Save_WritesTomlFile()
    {
        // Arrange
        var config = new AppConfig
        {
            Hotkey = new HotkeyConfig
            {
                Modifiers = new List<string> { "Ctrl", "Alt" },
                Key = "R"
            }
        };

        // Act
        ConfigManager.Save(_testConfigPath, config);

        // Assert
        File.Exists(_testConfigPath).Should().BeTrue();

        var savedContent = File.ReadAllText(_testConfigPath);
        savedContent.Should().Contain("[hotkey]");
        savedContent.Should().Contain("modifiers = [\"Ctrl\", \"Alt\"]");
        savedContent.Should().Contain("key = \"R\"");
    }

    [Fact]
    public void Load_ThenSave_PreservesData()
    {
        // Arrange
        var originalConfig = new AppConfig
        {
            Hotkey = new HotkeyConfig
            {
                Modifiers = new List<string> { "Shift", "Win" },
                Key = "F12"
            }
        };

        ConfigManager.Save(_testConfigPath, originalConfig);

        // Act
        var loadedConfig = ConfigManager.Load(_testConfigPath);

        // Assert
        loadedConfig.Hotkey.Modifiers.Should().BeEquivalentTo(new[] { "Shift", "Win" });
        loadedConfig.Hotkey.Key.Should().Be("F12");
    }

    [Fact]
    public void Load_InvalidTomlSyntax_ThrowsException()
    {
        // Arrange
        var invalidToml = @"
[hotkey
modifiers = unclosed string""
";
        File.WriteAllText(_testConfigPath, invalidToml);

        // Act
        Action act = () => ConfigManager.Load(_testConfigPath);

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("*config.toml*", "should mention file name in error");
    }

    [Fact]
    public void Load_EmptyModifiers_ThrowsValidationException()
    {
        // Arrange
        var tomlContent = @"
[hotkey]
modifiers = []
key = ""D""
";
        File.WriteAllText(_testConfigPath, tomlContent);

        // Act
        Action act = () => ConfigManager.Load(_testConfigPath);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one modifier*");
    }

    [Fact]
    public void Load_MissingKey_ThrowsValidationException()
    {
        // Arrange
        var tomlContent = @"
[hotkey]
modifiers = [""Ctrl""]
key = """"
";
        File.WriteAllText(_testConfigPath, tomlContent);

        // Act
        Action act = () => ConfigManager.Load(_testConfigPath);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*main key*");
    }

    [Fact]
    public void Load_InvalidModifier_ThrowsValidationException()
    {
        // Arrange
        var tomlContent = @"
[hotkey]
modifiers = [""Ctrl"", ""InvalidKey""]
key = ""D""
";
        File.WriteAllText(_testConfigPath, tomlContent);

        // Act
        Action act = () => ConfigManager.Load(_testConfigPath);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid modifier*InvalidKey*");
    }

    [Fact]
    public void GetDefault_ReturnsDefaultConfiguration()
    {
        // Act
        var config = ConfigManager.GetDefault();

        // Assert
        config.Should().NotBeNull();
        config.Hotkey.Modifiers.Should().BeEquivalentTo(new[] { "Ctrl", "Shift" });
        config.Hotkey.Key.Should().Be("D");
    }
}
