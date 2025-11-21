using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.Services;
using Moq;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for LlmPostProcessor service.
/// Tests for US-060, US-061, US-062 (Post-processing core logic).
/// </summary>
public class LlmPostProcessorTests
{
    private readonly string _testDirectory;

    public LlmPostProcessorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisper_LlmTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        // Initialize AppLogger with Error level to reduce test output verbosity
        LocalWhisper.Core.AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);
    }

    [Fact]
    public void DetectMarkdownMode_TranscriptStartsWithTrigger_ReturnsTrue()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var transcript = "markdown mode. Let's discuss the architecture.";

        // Act
        var (isMarkdown, cleaned) = processor.DetectMarkdownMode(transcript);

        // Assert
        isMarkdown.Should().BeTrue();
        cleaned.Should().Be("Let's discuss the architecture.");
    }

    [Fact]
    public void DetectMarkdownMode_TranscriptEndsWithTrigger_ReturnsTrue()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var transcript = "Let's discuss the architecture. markdown mode";

        // Act
        var (isMarkdown, cleaned) = processor.DetectMarkdownMode(transcript);

        // Assert
        isMarkdown.Should().BeTrue();
        cleaned.Should().Be("Let's discuss the architecture.");
    }

    [Fact]
    public void DetectMarkdownMode_TriggerInMiddle_ReturnsFalse()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var transcript = "Let's markdown this mode of thinking.";

        // Act
        var (isMarkdown, cleaned) = processor.DetectMarkdownMode(transcript);

        // Assert
        isMarkdown.Should().BeFalse();
        cleaned.Should().Be(transcript);
    }

    [Fact]
    public void DetectMarkdownMode_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var transcript = "MARKDOWN MODE. This is a test.";

        // Act
        var (isMarkdown, cleaned) = processor.DetectMarkdownMode(transcript);

        // Assert
        isMarkdown.Should().BeTrue();
        cleaned.Should().Be("This is a test.");
    }

    [Fact]
    public void DetectMarkdownMode_NoTrigger_ReturnsFalse()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var transcript = "This is a normal transcript.";

        // Act
        var (isMarkdown, cleaned) = processor.DetectMarkdownMode(transcript);

        // Assert
        isMarkdown.Should().BeFalse();
        cleaned.Should().Be(transcript);
    }

    [Fact]
    public void BuildSystemPrompt_PlainTextMode_ReturnsPlainTextPrompt()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var glossary = new Dictionary<string, string>();

        // Act
        var prompt = processor.BuildSystemPrompt(isMarkdownMode: false, glossary);

        // Assert
        prompt.Should().Contain("transcript formatter");
        prompt.Should().Contain("Fix grammar, punctuation, capitalization");
        prompt.Should().Contain("Don't use Markdown headings");
        prompt.Should().NotContain("APPLY THESE ABBREVIATIONS");
    }

    [Fact]
    public void BuildSystemPrompt_MarkdownMode_ReturnsMarkdownPrompt()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var glossary = new Dictionary<string, string>();

        // Act
        var prompt = processor.BuildSystemPrompt(isMarkdownMode: true, glossary);

        // Assert
        prompt.Should().Contain("Markdown");
        prompt.Should().Contain("## Heading");
        prompt.Should().Contain("Fix grammar, punctuation, capitalization");
    }

    [Fact]
    public void BuildSystemPrompt_WithGlossary_AppendsGlossary()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var glossary = new Dictionary<string, string>
        {
            { "asap", "as soon as possible" },
            { "fyi", "for your information" }
        };

        // Act
        var prompt = processor.BuildSystemPrompt(isMarkdownMode: false, glossary);

        // Assert
        prompt.Should().Contain("APPLY THESE ABBREVIATIONS:");
        prompt.Should().Contain("asap = as soon as possible");
        prompt.Should().Contain("fyi = for your information");
    }

    [Fact]
    public void BuildCommandLineArgs_IncludesAllRequiredParameters()
    {
        // Arrange
        var config = new PostProcessingConfig
        {
            LlmCliPath = "C:\\Tools\\llama-cli.exe",
            ModelPath = "C:\\Models\\llama.gguf",
            Temperature = 0.0f,
            TopP = 0.25f,
            RepeatPenalty = 1.05f,
            MaxTokens = 512,
            GpuAcceleration = true
        };
        var processor = new LlmPostProcessor();
        var systemPrompt = "You are a formatter.";
        var transcript = "test transcript";

        // Act
        var args = processor.BuildCommandLineArgs(config, systemPrompt, transcript);

        // Assert
        args.Should().Contain("-m \"C:\\Models\\llama.gguf\"");
        args.Should().Contain("-sys \"You are a formatter.\"");
        args.Should().Contain("-p \""); // Prompt flag present
        args.Should().Contain("User: test transcript"); // Transcript wrapped in conversation format
        args.Should().Contain("Assistant:"); // Completion trigger
        args.Should().Contain("--temp 0.0");
        args.Should().Contain("--top-p 0.25");
        args.Should().Contain("--repeat-penalty 1.05");
        args.Should().Contain("-n 512");
        args.Should().Contain("-ngl 99");
        args.Should().Contain("--log-disable");
    }

    [Fact]
    public void BuildCommandLineArgs_GpuDisabled_NoGpuLayers()
    {
        // Arrange
        var config = new PostProcessingConfig
        {
            LlmCliPath = "C:\\Tools\\llama-cli.exe",
            ModelPath = "C:\\Models\\llama.gguf",
            GpuAcceleration = false
        };
        var processor = new LlmPostProcessor();

        // Act
        var args = processor.BuildCommandLineArgs(config, "prompt", "transcript");

        // Assert
        args.Should().NotContain("-ngl");
    }

    // Note: ProcessAsync requires actual llama-cli.exe process execution
    // Full E2E testing is done in integration tests, not unit tests

    [Fact]
    public void ProcessAsync_TimeoutExceeded_ThrowsTimeoutException()
    {
        // Arrange
        var config = new PostProcessingConfig
        {
            LlmCliPath = "nonexistent.exe",
            ModelPath = "model.gguf",
            TimeoutSeconds = 1
        };
        var processor = new LlmPostProcessor();
        var transcript = "test";
        var glossary = new Dictionary<string, string>();  // Fix: was null

        // Act
        Func<Task> act = async () => await processor.ProcessAsync(transcript, config, glossary);

        // Assert
        act.Should().ThrowAsync<TimeoutException>("timeout should be enforced");
    }

    [Fact]
    public void StripMarkdownTrigger_StartsWithTrigger_RemovesTrigger()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var text = "markdown mode. This is the content.";

        // Act
        var cleaned = processor.StripMarkdownTrigger(text);

        // Assert
        cleaned.Should().Be("This is the content.");
    }

    [Fact]
    public void StripMarkdownTrigger_EndsWithTrigger_RemovesTrigger()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var text = "This is the content. markdown mode";

        // Act
        var cleaned = processor.StripMarkdownTrigger(text);

        // Assert
        cleaned.Should().Be("This is the content.");
    }

    [Fact]
    public void StripMarkdownTrigger_NoTrigger_ReturnsOriginal()
    {
        // Arrange
        var processor = new LlmPostProcessor();
        var text = "This is normal text.";

        // Act
        var cleaned = processor.StripMarkdownTrigger(text);

        // Assert
        cleaned.Should().Be(text);
    }
}
