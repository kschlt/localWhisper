using FluentAssertions;
using LocalWhisper.Models;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for PostProcessingConfig model.
/// Tests for US-060 (config structure).
/// </summary>
public class PostProcessingConfigTests
{
    [Fact]
    public void PostProcessingConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new PostProcessingConfig();

        // Assert
        config.Enabled.Should().BeFalse("post-processing should be disabled by default");
        config.LlmCliPath.Should().BeNullOrEmpty();
        config.ModelPath.Should().BeNullOrEmpty();
        config.TimeoutSeconds.Should().Be(5, "default timeout is 5 seconds");
        config.GpuAcceleration.Should().BeTrue("GPU should be enabled by default for auto-detect");
        config.UseGlossary.Should().BeFalse("glossary should be disabled by default");
        config.GlossaryPath.Should().BeNullOrEmpty();
        config.Temperature.Should().Be(0.0f, "deterministic output");
        config.TopP.Should().Be(0.25f);
        config.RepeatPenalty.Should().Be(1.05f);
        config.MaxTokens.Should().Be(512);
    }

    [Fact]
    public void PostProcessingConfig_CanSetAllProperties()
    {
        // Arrange
        var config = new PostProcessingConfig
        {
            Enabled = true,
            LlmCliPath = "C:\\Tools\\llama-cli.exe",
            ModelPath = "C:\\Models\\llama.gguf",
            TimeoutSeconds = 10,
            GpuAcceleration = false,
            UseGlossary = true,
            GlossaryPath = "C:\\glossary.txt",
            Temperature = 0.1f,
            TopP = 0.3f,
            RepeatPenalty = 1.1f,
            MaxTokens = 1024
        };

        // Assert
        config.Enabled.Should().BeTrue();
        config.LlmCliPath.Should().Be("C:\\Tools\\llama-cli.exe");
        config.ModelPath.Should().Be("C:\\Models\\llama.gguf");
        config.TimeoutSeconds.Should().Be(10);
        config.GpuAcceleration.Should().BeFalse();
        config.UseGlossary.Should().BeTrue();
        config.GlossaryPath.Should().Be("C:\\glossary.txt");
        config.Temperature.Should().Be(0.1f);
        config.TopP.Should().Be(0.3f);
        config.RepeatPenalty.Should().Be(1.1f);
        config.MaxTokens.Should().Be(1024);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    public void PostProcessingConfig_InvalidTimeout_ThrowsArgumentException(int timeout)
    {
        // Arrange & Act
        var act = () => new PostProcessingConfig { TimeoutSeconds = timeout };

        // Assert
        act.Should().Throw<ArgumentException>("timeout must be 1-30 seconds");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(30)]
    public void PostProcessingConfig_ValidTimeout_Succeeds(int timeout)
    {
        // Arrange & Act
        var config = new PostProcessingConfig { TimeoutSeconds = timeout };

        // Assert
        config.TimeoutSeconds.Should().Be(timeout);
    }
}
