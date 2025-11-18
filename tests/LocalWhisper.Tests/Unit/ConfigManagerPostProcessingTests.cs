using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for ConfigManager with PostProcessing configuration.
/// Tests for US-060 (config parsing).
/// </summary>
public class ConfigManagerPostProcessingTests : IDisposable
{
    private readonly string _testDirectory;

    public ConfigManagerPostProcessingTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisper_ConfigTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_ConfigWithPostProcessingSection_ParsesCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config.toml");
        File.WriteAllText(configPath, @"
[hotkey]
modifiers = [""Ctrl"", ""Shift""]
key = ""D""

[postprocessing]
enabled = true
llm_cli_path = ""C:\\Tools\\llama-cli.exe""
model_path = ""C:\\Models\\llama.gguf""
timeout_seconds = 10
gpu_acceleration = false
use_glossary = true
glossary_path = ""C:\\glossary.txt""
");

        // Act
        var config = ConfigManager.Load(configPath);

        // Assert
        config.PostProcessing.Should().NotBeNull();
        config.PostProcessing.Enabled.Should().BeTrue();
        config.PostProcessing.LlmCliPath.Should().Be("C:\\Tools\\llama-cli.exe");
        config.PostProcessing.ModelPath.Should().Be("C:\\Models\\llama.gguf");
        config.PostProcessing.TimeoutSeconds.Should().Be(10);
        config.PostProcessing.GpuAcceleration.Should().BeFalse();
        config.PostProcessing.UseGlossary.Should().BeTrue();
        config.PostProcessing.GlossaryPath.Should().Be("C:\\glossary.txt");
    }

    [Fact]
    public void Load_ConfigWithoutPostProcessingSection_UsesDefaults()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config_no_pp.toml");
        File.WriteAllText(configPath, @"
[hotkey]
modifiers = [""Ctrl"", ""Shift""]
key = ""D""
");

        // Act
        var config = ConfigManager.Load(configPath);

        // Assert
        config.PostProcessing.Should().NotBeNull("should provide default config");
        config.PostProcessing.Enabled.Should().BeFalse("default is disabled");
        config.PostProcessing.TimeoutSeconds.Should().Be(5, "default timeout");
    }

    [Fact]
    public void Save_ConfigWithPostProcessing_WritesCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config_save.toml");
        var config = new AppConfig
        {
            Hotkey = new HotkeyConfig
            {
                Modifiers = new List<string> { "Ctrl", "Shift" },
                Key = "D"
            },
            DataRoot = "C:\\Data",
            Language = "de",
            FileFormat = ".md",
            PostProcessing = new PostProcessingConfig
            {
                Enabled = true,
                LlmCliPath = "C:\\llama.exe",
                ModelPath = "C:\\model.gguf",
                TimeoutSeconds = 7,
                GpuAcceleration = true,
                UseGlossary = false
            }
        };

        // Act
        ConfigManager.Save(config, configPath);

        // Assert
        var savedContent = File.ReadAllText(configPath);
        savedContent.Should().Contain("[postprocessing]");
        savedContent.Should().Contain("enabled = true");
        savedContent.Should().Contain("llm_cli_path = \"C:\\llama.exe\"");
        savedContent.Should().Contain("model_path = \"C:\\model.gguf\"");
        savedContent.Should().Contain("timeout_seconds = 7");
    }

    [Fact]
    public void Load_InvalidTimeoutValue_ThrowsValidationException()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config_invalid.toml");
        File.WriteAllText(configPath, @"
[hotkey]
modifiers = [""Ctrl""]
key = ""D""

[postprocessing]
enabled = true
timeout_seconds = 100
");

        // Act
        Action act = () => ConfigManager.Load(configPath);

        // Assert
        act.Should().Throw<ArgumentException>("timeout must be 1-30");
    }

    [Fact]
    public void Load_PartialPostProcessingConfig_UsesDefaults()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "config_partial.toml");
        File.WriteAllText(configPath, @"
[hotkey]
modifiers = [""Ctrl""]
key = ""D""

[postprocessing]
enabled = true
llm_cli_path = ""C:\\llama.exe""
# Missing other fields - should use defaults
");

        // Act
        var config = ConfigManager.Load(configPath);

        // Assert
        config.PostProcessing.Enabled.Should().BeTrue();
        config.PostProcessing.LlmCliPath.Should().Be("C:\\llama.exe");
        config.PostProcessing.TimeoutSeconds.Should().Be(5, "default timeout");
        config.PostProcessing.GpuAcceleration.Should().BeTrue("default GPU enabled");
        config.PostProcessing.UseGlossary.Should().BeFalse("default glossary disabled");
    }
}
