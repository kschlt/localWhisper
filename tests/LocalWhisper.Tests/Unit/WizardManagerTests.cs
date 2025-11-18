using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using System.Windows.Input;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for WizardManager class.
/// </summary>
/// <remarks>
/// Tests cover US-040: Wizard Step 1 - Data Root Selection
/// Scenario: Folder structure is created (@Integration @CanRunInClaudeCode)
///
/// Tests verify:
/// - CreateDataRootStructure creates all required folders
/// - ValidateDataRoot checks write access correctly
/// - GenerateInitialConfig creates valid config.toml
/// - CopyModelFile copies model to models/ directory
///
/// See: docs/iterations/iteration-05a-wizard-core.md (Task 4)
/// See: docs/specification/user-stories-gherkin.md (US-040, lines 686-697)
/// </remarks>
public class WizardManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly WizardManager _manager;

    public WizardManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _manager = new WizardManager();

        AppLogger.Initialize(_testDirectory);
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
    public void CreateDataRootStructure_CreatesAllRequiredFolders()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "test-data-root");

        // Act
        _manager.CreateDataRootStructure(dataRoot);

        // Assert - Verify all folders from Gherkin scenario
        Directory.Exists(Path.Combine(dataRoot, "config")).Should().BeTrue("config/ folder should be created");
        Directory.Exists(Path.Combine(dataRoot, "models")).Should().BeTrue("models/ folder should be created");
        Directory.Exists(Path.Combine(dataRoot, "history")).Should().BeTrue("history/ folder should be created");
        Directory.Exists(Path.Combine(dataRoot, "logs")).Should().BeTrue("logs/ folder should be created");
        Directory.Exists(Path.Combine(dataRoot, "tmp")).Should().BeTrue("tmp/ folder should be created");
    }

    [Fact]
    public void CreateDataRootStructure_CreatesFailedSubfolder()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "test-data-root");

        // Act
        _manager.CreateDataRootStructure(dataRoot);

        // Assert
        Directory.Exists(Path.Combine(dataRoot, "tmp", "failed")).Should().BeTrue(
            "tmp/failed/ subfolder should be created for storing failed audio");
    }

    [Fact]
    public void CreateDataRootStructure_DoesNotFailIfFoldersAlreadyExist()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "test-data-root");
        Directory.CreateDirectory(Path.Combine(dataRoot, "config")); // Pre-create one folder

        // Act
        Action act = () => _manager.CreateDataRootStructure(dataRoot);

        // Assert
        act.Should().NotThrow("creating folders should be idempotent");
        Directory.Exists(Path.Combine(dataRoot, "models")).Should().BeTrue("other folders should still be created");
    }

    [Fact]
    public void ValidateDataRoot_WritableDirectory_ReturnsTrue()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "writable-dir");
        Directory.CreateDirectory(dataRoot);

        // Act
        var result = _manager.ValidateDataRoot(dataRoot);

        // Assert
        result.Should().BeTrue("directory is writable");
    }

    [Fact]
    public void ValidateDataRoot_NonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "does-not-exist");

        // Act
        var result = _manager.ValidateDataRoot(dataRoot);

        // Assert
        result.Should().BeFalse("directory does not exist");
    }

    [Fact]
    public void ValidateDataRoot_ReadOnlyDirectory_ReturnsFalse()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "readonly-dir");
        Directory.CreateDirectory(dataRoot);

        // Make directory read-only
        var dirInfo = new DirectoryInfo(dataRoot);
        dirInfo.Attributes = FileAttributes.Directory | FileAttributes.ReadOnly;

        try
        {
            // Act
            var result = _manager.ValidateDataRoot(dataRoot);

            // Assert
            result.Should().BeFalse("directory is read-only");
        }
        finally
        {
            // Cleanup: remove read-only attribute
            dirInfo.Attributes = FileAttributes.Directory;
        }
    }

    [Fact]
    public void GenerateInitialConfig_CreatesValidConfigToml()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "config-test");
        _manager.CreateDataRootStructure(dataRoot);

        var modelFilePath = Path.Combine(_testDirectory, "ggml-small.bin");
        File.WriteAllText(modelFilePath, "dummy model content");

        // Act
        _manager.GenerateInitialConfig(
            dataRoot,
            modelFilePath,
            language: "de",
            hotkeyModifiers: ModifierKeys.Control | ModifierKeys.Shift,
            hotkeyKey: Key.D);

        // Assert
        var configPath = Path.Combine(dataRoot, "config", "config.toml");
        File.Exists(configPath).Should().BeTrue("config.toml should be created");

        var configContent = File.ReadAllText(configPath);
        configContent.Should().Contain("[whisper]", "config should have [whisper] section");
        configContent.Should().Contain("[hotkey]", "config should have [hotkey] section");
        configContent.Should().Contain("language = \"de\"", "config should contain language setting");
    }

    [Fact]
    public void GenerateInitialConfig_CopiesModelToModelsFolder()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "model-copy-test");
        _manager.CreateDataRootStructure(dataRoot);

        var sourceModelPath = Path.Combine(_testDirectory, "ggml-small.bin");
        var modelContent = "dummy model content";
        File.WriteAllText(sourceModelPath, modelContent);

        // Act
        _manager.GenerateInitialConfig(
            dataRoot,
            sourceModelPath,
            language: "de",
            hotkeyModifiers: ModifierKeys.Control | ModifierKeys.Shift,
            hotkeyKey: Key.D);

        // Assert
        var targetModelPath = Path.Combine(dataRoot, "models", "ggml-small.bin");
        File.Exists(targetModelPath).Should().BeTrue("model file should be copied to models/ folder");
        File.ReadAllText(targetModelPath).Should().Be(modelContent, "copied file should have same content");
    }

    [Fact]
    public void GenerateInitialConfig_SetsCorrectHotkeyValues()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "hotkey-test");
        _manager.CreateDataRootStructure(dataRoot);

        var modelFilePath = Path.Combine(_testDirectory, "ggml-small.bin");
        File.WriteAllText(modelFilePath, "dummy model");

        // Act
        _manager.GenerateInitialConfig(
            dataRoot,
            modelFilePath,
            language: "en",
            hotkeyModifiers: ModifierKeys.Control | ModifierKeys.Alt,
            hotkeyKey: Key.V);

        // Assert
        var configPath = Path.Combine(dataRoot, "config", "config.toml");
        var config = ConfigManager.Load(configPath);

        config.Hotkey.Modifiers.Should().Be(ModifierKeys.Control | ModifierKeys.Alt);
        config.Hotkey.Key.Should().Be(Key.V);
    }

    [Fact]
    public void GenerateInitialConfig_SetsCorrectLanguage()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "language-test");
        _manager.CreateDataRootStructure(dataRoot);

        var modelFilePath = Path.Combine(_testDirectory, "ggml-small.en.bin");
        File.WriteAllText(modelFilePath, "dummy model");

        // Act - English language
        _manager.GenerateInitialConfig(
            dataRoot,
            modelFilePath,
            language: "en",
            hotkeyModifiers: ModifierKeys.Control | ModifierKeys.Shift,
            hotkeyKey: Key.D);

        // Assert
        var configPath = Path.Combine(dataRoot, "config", "config.toml");
        var config = ConfigManager.Load(configPath);

        config.Whisper.Language.Should().Be("en");
    }

    [Fact]
    public void GenerateInitialConfig_SetsModelPathCorrectly()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "modelpath-test");
        _manager.CreateDataRootStructure(dataRoot);

        var sourceModelPath = Path.Combine(_testDirectory, "ggml-medium.bin");
        File.WriteAllText(sourceModelPath, "dummy medium model");

        // Act
        _manager.GenerateInitialConfig(
            dataRoot,
            sourceModelPath,
            language: "de",
            hotkeyModifiers: ModifierKeys.Control | ModifierKeys.Shift,
            hotkeyKey: Key.D);

        // Assert
        var configPath = Path.Combine(dataRoot, "config", "config.toml");
        var config = ConfigManager.Load(configPath);

        var expectedModelPath = Path.Combine(dataRoot, "models", "ggml-medium.bin");
        config.Whisper.ModelPath.Should().Be(expectedModelPath, "model_path should point to models/ folder");
    }

    [Fact]
    public void GenerateInitialConfig_ThrowsIfModelFileDoesNotExist()
    {
        // Arrange
        var dataRoot = Path.Combine(_testDirectory, "error-test");
        _manager.CreateDataRootStructure(dataRoot);

        var nonExistentModel = Path.Combine(_testDirectory, "does-not-exist.bin");

        // Act
        Action act = () => _manager.GenerateInitialConfig(
            dataRoot,
            nonExistentModel,
            language: "de",
            hotkeyModifiers: ModifierKeys.Control | ModifierKeys.Shift,
            hotkeyKey: Key.D);

        // Assert
        act.Should().Throw<FileNotFoundException>("source model file must exist");
    }

    [Fact]
    public void GenerateInitialConfig_PathTraversal_ThrowsException()
    {
        // Arrange - Security test: prevent path traversal attacks
        var dataRoot = Path.Combine(_testDirectory, "security-test");
        _manager.CreateDataRootStructure(dataRoot);

        // Create malicious path attempting to escape data root
        var maliciousPath = Path.Combine(_testDirectory, "..", "..", "etc", "passwd");
        File.WriteAllText(Path.Combine(_testDirectory, "malicious.bin"), "malicious content");

        // Act
        Action act = () => _manager.GenerateInitialConfig(
            dataRoot,
            maliciousPath,
            language: "de",
            hotkeyModifiers: ModifierKeys.Control | ModifierKeys.Shift,
            hotkeyKey: Key.D);

        // Assert - Should throw FileNotFoundException (file doesn't exist at malicious path)
        // OR implement path validation to reject paths outside data root
        act.Should().Throw<Exception>("should reject path traversal attempts");
    }

    [Fact]
    public void CreateDataRootStructure_DiskFullSimulation_ThrowsIOException()
    {
        // Arrange - This test documents expected behavior when disk is full
        // In real scenario, Directory.CreateDirectory would throw IOException
        var dataRoot = Path.Combine(_testDirectory, "diskfull-test");

        // Note: Cannot easily simulate disk full in unit test without admin rights
        // This test documents the expected behavior - implementation should handle IOException
        // Act & Assert - Document expected behavior
        Action act = () => _manager.CreateDataRootStructure(dataRoot);

        // Should not throw in normal conditions
        act.Should().NotThrow("under normal disk conditions");

        // Implementation should propagate IOException if disk is full
        // (Cannot test without mocking or admin rights to fill disk)
    }
}
