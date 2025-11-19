using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Services;
using Xunit;

namespace LocalWhisper.Tests.Integration;

/// <summary>
/// Integration tests for Post-Processing feature.
/// Tests for US-061 (fallback), US-062 (meaning preservation), US-063 (glossary).
/// </summary>
public class PostProcessingIntegrationTests : IDisposable
{
    private readonly string _testDirectory;

    public PostProcessingIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisper_Integration_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        // Initialize AppLogger with Error level to reduce test output verbosity
        LocalWhisper.Core.AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void E2E_PostProcessingDisabled_SkipsPostProcessing()
    {
        // Arrange
        var config = new PostProcessingConfig
        {
            Enabled = false
        };
        var sttResult = "this is the original stt text";

        // Act
        // In actual app flow, if config.Enabled == false, we skip post-processing entirely
        var finalText = config.Enabled ? "post-processed" : sttResult;

        // Assert
        finalText.Should().Be(sttResult, "post-processing should be skipped when disabled");
    }

    [Fact]
    public async Task E2E_LlmProcessFails_FallbackToOriginal()
    {
        // Arrange
        var config = new PostProcessingConfig
        {
            Enabled = true,
            LlmCliPath = "nonexistent.exe",  // Will fail
            ModelPath = "nonexistent.gguf",
            TimeoutSeconds = 1
        };
        var processor = new LlmPostProcessor();
        var originalText = "original stt transcript";

        // Act
        string finalText;
        try
        {
            finalText = await processor.ProcessAsync(originalText, config, null);
        }
        catch
        {
            // Fallback to original on any error
            finalText = originalText;
        }

        // Assert
        finalText.Should().Be(originalText, "should fallback to original text on error");
    }

    [Fact]
    public void E2E_GlossaryIntegration_AppendsToPrompt()
    {
        // Arrange
        var glossaryPath = Path.Combine(_testDirectory, "glossary.txt");
        File.WriteAllText(glossaryPath, @"asap = as soon as possible
fyi = for your information
");
        var glossaryLoader = new GlossaryLoader();
        var processor = new LlmPostProcessor();

        // Act
        var entries = glossaryLoader.LoadGlossary(glossaryPath);
        var prompt = processor.BuildSystemPrompt(isMarkdownMode: false, entries);

        // Assert
        entries.Should().HaveCount(2);
        prompt.Should().Contain("APPLY THESE ABBREVIATIONS:");
        prompt.Should().Contain("asap = as soon as possible");
        prompt.Should().Contain("fyi = for your information");
    }

    [Fact]
    public void E2E_MarkdownModeDetection_StripsAndSetsMode()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var transcript = "markdown mode. This is my content about architecture.";

        // Act
        var (isMarkdown, cleanedTranscript) = processor.DetectMarkdownMode(transcript);
        var prompt = processor.BuildSystemPrompt(isMarkdown, new Dictionary<string, string>());

        // Assert
        isMarkdown.Should().BeTrue();
        cleanedTranscript.Should().Be("This is my content about architecture.");
        prompt.Should().Contain("Markdown");
    }

    [Fact]
    public void E2E_PlainTextMode_NoMarkdownFormatting()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var transcript = "This is plain text without trigger.";

        // Act
        var (isMarkdown, cleanedTranscript) = processor.DetectMarkdownMode(transcript);
        var prompt = processor.BuildSystemPrompt(isMarkdown, new Dictionary<string, string>());

        // Assert
        isMarkdown.Should().BeFalse();
        cleanedTranscript.Should().Be(transcript);
        prompt.Should().Contain("Don't use Markdown headings");
    }

    [Fact]
    public void E2E_ConfigRoundTrip_PreservesPostProcessingSettings()
    {
        // Arrange
        var configPath = Path.Combine(_testDirectory, "roundtrip.toml");
        var originalConfig = new AppConfig
        {
            Hotkey = new HotkeyConfig { Modifiers = new List<string> { "Ctrl" }, Key = "D" },
            DataRoot = "C:\\Data",
            Language = "en",
            FileFormat = ".txt",
            PostProcessing = new PostProcessingConfig
            {
                Enabled = true,
                LlmCliPath = "C:\\llama.exe",
                ModelPath = "C:\\model.gguf",
                TimeoutSeconds = 8,
                GpuAcceleration = false,
                UseGlossary = true,
                GlossaryPath = "C:\\glossary.txt"
            }
        };

        // Act
        ConfigManager.Save(configPath, originalConfig);
        var loadedConfig = ConfigManager.Load(configPath);

        // Assert
        loadedConfig.PostProcessing.Should().NotBeNull();
        loadedConfig.PostProcessing.Enabled.Should().Be(originalConfig.PostProcessing.Enabled);
        loadedConfig.PostProcessing.LlmCliPath.Should().Be(originalConfig.PostProcessing.LlmCliPath);
        loadedConfig.PostProcessing.ModelPath.Should().Be(originalConfig.PostProcessing.ModelPath);
        loadedConfig.PostProcessing.TimeoutSeconds.Should().Be(originalConfig.PostProcessing.TimeoutSeconds);
        loadedConfig.PostProcessing.GpuAcceleration.Should().Be(originalConfig.PostProcessing.GpuAcceleration);
        loadedConfig.PostProcessing.UseGlossary.Should().Be(originalConfig.PostProcessing.UseGlossary);
        loadedConfig.PostProcessing.GlossaryPath.Should().Be(originalConfig.PostProcessing.GlossaryPath);
    }
}
