using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Services;
using System.Windows.Input;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for DataRootValidator class.
/// </summary>
/// <remarks>
/// Tests cover US-043: Repair Flow (Data Root Missing)
/// Scenario: App detects missing data root on startup
/// Scenario: User re-links to moved folder
///
/// Tests verify:
/// - Validates data root existence
/// - Checks folder structure (config/, models/, history/, logs/, tmp/)
/// - Verifies config.toml exists
/// - Verifies model file exists
/// - Returns detailed validation results with errors and warnings
///
/// See: docs/iterations/iteration-05b-download-repair.md (Task 3)
/// See: docs/specification/user-stories-gherkin.md (US-043, lines 872-895)
/// </remarks>
[Trait("Batch", "2")]
public class DataRootValidatorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly DataRootValidator _validator;

    public DataRootValidatorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _validator = new DataRootValidator();

        // Initialize with Error level to reduce test output verbosity
        AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Expected if AppLogger still has log file open
            }
        }
    }

    [Fact]
    public void Validate_ValidDataRoot_ReturnsValid()
    {
        // Arrange
        var dataRoot = CreateValidDataRoot();
        var config = CreateTestConfig(dataRoot);

        // Act
        var result = _validator.Validate(dataRoot, config);

        // Assert
        result.IsValid.Should().BeTrue("data root has all required folders and files");
        result.Errors.Should().BeEmpty("no errors should be present");
    }

    [Fact]
    public void Validate_DataRootDoesNotExist_ReturnsInvalid()
    {
        // Arrange
        var nonExistentRoot = Path.Combine(_testDirectory, "does-not-exist");
        var config = new AppConfig();

        // Act
        var result = _validator.Validate(nonExistentRoot, config);

        // Assert
        result.IsValid.Should().BeFalse("data root does not exist");
        result.Errors.Should().Contain(e => e.Contains("does not exist"), "error should mention missing directory");
    }

    [Fact]
    public void Validate_MissingConfigToml_ReturnsInvalid()
    {
        // Arrange
        var dataRoot = CreateDataRootWithoutConfig();
        var config = CreateTestConfig(dataRoot);

        // Act
        var result = _validator.Validate(dataRoot, config);

        // Assert
        result.IsValid.Should().BeFalse("config.toml is missing");
        result.Errors.Should().Contain(e => e.Contains("config.toml not found"), "error should mention missing config");
    }

    [Fact]
    public void Validate_MissingModelFile_ReturnsInvalid()
    {
        // Arrange
        var dataRoot = CreateDataRootWithoutModel();
        var config = CreateTestConfig(dataRoot);

        // Act
        var result = _validator.Validate(dataRoot, config);

        // Assert
        result.IsValid.Should().BeFalse("model file is missing");
        result.Errors.Should().Contain(e => e.Contains("Model file not found"), "error should mention missing model");
    }

    [Fact]
    public void Validate_MissingFolders_ReturnsWarnings()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "partial-root");
        Directory.CreateDirectory(dataRoot);

        // Create only config/ and models/
        Directory.CreateDirectory(Path.Combine(dataRoot, "config"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "models"));

        // Create config.toml
        var configPath = Path.Combine(dataRoot, "config", "config.toml");
        File.WriteAllText(configPath, "[whisper]\nlanguage = \"de\"\nmodel_path = \"" +
            Path.Combine(dataRoot, "models", "ggml-small.bin").Replace("\\", "\\\\") + "\"\n\n[hotkey]\nmodifiers = 3\nkey = 33");

        // Create model file
        var modelPath = Path.Combine(dataRoot, "models", "ggml-small.bin");
        File.WriteAllText(modelPath, "dummy model");

        var config = ConfigManager.Load(configPath);

        // Act
        var result = _validator.Validate(dataRoot, config);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("history"), "history/ folder is missing");
        result.Warnings.Should().Contain(w => w.Contains("logs"), "logs/ folder is missing");
        result.Warnings.Should().Contain(w => w.Contains("tmp"), "tmp/ folder is missing");
    }

    [Fact]
    public void Validate_AllFoldersPresent_NoWarnings()
    {
        // Arrange
        var dataRoot = CreateValidDataRoot();
        var config = CreateTestConfig(dataRoot);

        // Act
        var result = _validator.Validate(dataRoot, config);

        // Assert
        result.Warnings.Should().BeEmpty("all required folders are present");
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "broken-root");
        Directory.CreateDirectory(dataRoot);
        Directory.CreateDirectory(Path.Combine(dataRoot, "config"));
        // No config.toml, no models/ folder

        var config = new AppConfig
        {
            Whisper = new WhisperConfig
            {
                ModelPath = Path.Combine(dataRoot, "models", "ggml-small.bin")
            }
        };

        // Act
        var result = _validator.Validate(dataRoot, config);

        // Assert
        result.IsValid.Should().BeFalse("multiple issues exist");
        result.Errors.Should().HaveCountGreaterOrEqualTo(2, "both config.toml and model file are missing");
    }

    [Fact(Skip = "Requires Windows elevation or specific filesystem support")]
    public void Validate_SymbolicLinkDataRoot_HandlesCorrectly()
    {
        // Arrange - Test handling of symbolic links / junction points (common when folders are moved)
        // Note: Creating symbolic links on Windows requires admin rights or developer mode
        // This test documents expected behavior

        var actualDataRoot = CreateValidDataRoot();
        var config = CreateTestConfig(actualDataRoot);

        // In real scenario: mklink /J link_path actual_path
        // For now, document expected behavior:
        // - Validator should follow symlink and validate target
        // - Should return valid if target is valid

        // Act
        var result = _validator.Validate(actualDataRoot, config);

        // Assert
        result.IsValid.Should().BeTrue("should handle symbolic links correctly");

        // TODO: If symbolic link support is critical, implement test with admin rights
        // or mock filesystem abstraction
    }

    // Helper methods

    private string CreateValidDataRoot()
    {
        var dataRoot = Path.Combine(_testDirectory, Guid.NewGuid().ToString());

        // Create folder structure
        Directory.CreateDirectory(Path.Combine(dataRoot, "config"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "models"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "history"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "logs"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "tmp"));

        // Create config.toml
        var configPath = Path.Combine(dataRoot, "config", "config.toml");
        File.WriteAllText(configPath, "[whisper]\nlanguage = \"de\"\nmodel_path = \"" +
            Path.Combine(dataRoot, "models", "ggml-small.bin").Replace("\\", "\\\\") + "\"\n\n[hotkey]\nmodifiers = 3\nkey = 33");

        // Create model file
        var modelPath = Path.Combine(dataRoot, "models", "ggml-small.bin");
        File.WriteAllText(modelPath, "dummy model content");

        return dataRoot;
    }

    private string CreateDataRootWithoutConfig()
    {
        var dataRoot = Path.Combine(_testDirectory, Guid.NewGuid().ToString());

        // Create folders
        Directory.CreateDirectory(Path.Combine(dataRoot, "config"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "models"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "history"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "logs"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "tmp"));

        // Create model file
        var modelPath = Path.Combine(dataRoot, "models", "ggml-small.bin");
        File.WriteAllText(modelPath, "dummy model");

        // NO config.toml created

        return dataRoot;
    }

    private string CreateDataRootWithoutModel()
    {
        var dataRoot = Path.Combine(_testDirectory, Guid.NewGuid().ToString());

        // Create folders
        Directory.CreateDirectory(Path.Combine(dataRoot, "config"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "models"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "history"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "logs"));
        Directory.CreateDirectory(Path.Combine(dataRoot, "tmp"));

        // Create config.toml
        var configPath = Path.Combine(dataRoot, "config", "config.toml");
        File.WriteAllText(configPath, "[whisper]\nlanguage = \"de\"\nmodel_path = \"" +
            Path.Combine(dataRoot, "models", "ggml-small.bin").Replace("\\", "\\\\") + "\"\n\n[hotkey]\nmodifiers = 3\nkey = 33");

        // NO model file created

        return dataRoot;
    }

    private AppConfig CreateTestConfig(string dataRoot)
    {
        var configPath = Path.Combine(dataRoot, "config", "config.toml");

        if (File.Exists(configPath))
        {
            return ConfigManager.Load(configPath);
        }

        // Fallback
        return new AppConfig
        {
            Whisper = new WhisperConfig
            {
                Language = "de",
                ModelPath = Path.Combine(dataRoot, "models", "ggml-small.bin")
            },
            Hotkey = new HotkeyConfig
            {
                Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Key = Key.D
            }
        };
    }
}
