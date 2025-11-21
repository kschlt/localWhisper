using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LocalWhisper.Adapters;
using LocalWhisper.Core;
using LocalWhisper.Models;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for WhisperCLIAdapter class.
/// </summary>
/// <remarks>
/// Tests cover US-020, US-021, US-022: STT via Whisper CLI
/// - CLI invocation with correct arguments
/// - JSON output parsing
/// - Exit code error mapping
/// - Timeout handling
///
/// See: docs/iterations/iteration-03-stt-whisper.md
/// </remarks>
public class WhisperCLIAdapterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly WhisperConfig _config;

    public WhisperCLIAdapterTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Initialize AppLogger for tests with Error level to reduce test output verbosity
        AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);

        // Create test configuration
        _config = new WhisperConfig
        {
            CLIPath = "whisper-cli-mock",
            ModelPath = Path.Combine(_testDirectory, "model.bin"),
            Language = "de",
            TimeoutSeconds = 60
        };
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public void ParseJSONOutput_ValidJSON_ReturnsSTTResult()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDirectory, "result.json");
        var jsonContent = @"{
            ""text"": ""Dies ist ein Test."",
            ""language"": ""de"",
            ""duration_sec"": 2.5
        }";
        File.WriteAllText(jsonPath, jsonContent);

        var adapter = new WhisperCLIAdapter(_config);

        // Act
        var result = adapter.ParseJSONOutput(jsonPath);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Dies ist ein Test.");
        result.Language.Should().Be("de");
        result.DurationSeconds.Should().Be(2.5);
        result.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ParseJSONOutput_EmptyText_ReturnsEmptyResult()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDirectory, "empty.json");
        var jsonContent = @"{
            ""text"": """",
            ""language"": ""de"",
            ""duration_sec"": 1.0
        }";
        File.WriteAllText(jsonPath, jsonContent);

        var adapter = new WhisperCLIAdapter(_config);

        // Act
        var result = adapter.ParseJSONOutput(jsonPath);

        // Assert
        result.IsEmpty.Should().BeTrue("empty text should be detected");
    }

    [Fact]
    public void ParseJSONOutput_WhitespaceOnlyText_ReturnsEmptyResult()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDirectory, "whitespace.json");
        var jsonContent = @"{
            ""text"": ""   "",
            ""language"": ""de"",
            ""duration_sec"": 1.0
        }";
        File.WriteAllText(jsonPath, jsonContent);

        var adapter = new WhisperCLIAdapter(_config);

        // Act
        var result = adapter.ParseJSONOutput(jsonPath);

        // Assert
        result.IsEmpty.Should().BeTrue("whitespace-only text should be treated as empty");
    }

    [Fact]
    public void ParseJSONOutput_MalformedJSON_ThrowsSTTException()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDirectory, "malformed.json");
        File.WriteAllText(jsonPath, "{ invalid json }");

        var adapter = new WhisperCLIAdapter(_config);

        // Act
        Action act = () => adapter.ParseJSONOutput(jsonPath);

        // Assert
        act.Should().Throw<STTException>()
            .WithMessage("*Invalid JSON format*");
    }

    [Fact]
    public void ParseJSONOutput_MissingFile_ThrowsSTTException()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDirectory, "nonexistent.json");
        var adapter = new WhisperCLIAdapter(_config);

        // Act
        Action act = () => adapter.ParseJSONOutput(jsonPath);

        // Assert
        act.Should().Throw<STTException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void ParseJSONOutput_WithSegments_ParsesSegmentsCorrectly()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDirectory, "segments.json");
        var jsonContent = @"{
            ""text"": ""Hello world."",
            ""language"": ""en"",
            ""duration_sec"": 2.0,
            ""segments"": [
                { ""start"": 0.0, ""end"": 1.0, ""text"": ""Hello"" },
                { ""start"": 1.0, ""end"": 2.0, ""text"": ""world."" }
            ]
        }";
        File.WriteAllText(jsonPath, jsonContent);

        var adapter = new WhisperCLIAdapter(_config);

        // Act
        var result = adapter.ParseJSONOutput(jsonPath);

        // Assert
        result.Segments.Should().NotBeNull();
        result.Segments.Should().HaveCount(2);
        result.Segments![0].Text.Should().Be("Hello");
        result.Segments[1].Text.Should().Be("world.");
    }

    [Fact]
    public void HandleExitCode_ExitCode0_DoesNotThrow()
    {
        // Arrange
        var adapter = new WhisperCLIAdapter(_config);

        // Act
        Action act = () => adapter.HandleExitCode(0, "");

        // Assert
        act.Should().NotThrow("exit code 0 indicates success");
    }

    [Fact]
    public void HandleExitCode_ExitCode1_ThrowsSTTException()
    {
        // Arrange
        var adapter = new WhisperCLIAdapter(_config);

        // Act
        Action act = () => adapter.HandleExitCode(1, "General error");

        // Assert
        act.Should().Throw<STTException>()
            .WithMessage("*Fehler bei Transkription*");
    }

    [Fact]
    public void HandleExitCode_ExitCode2_ThrowsModelNotFoundException()
    {
        // Arrange
        var adapter = new WhisperCLIAdapter(_config);

        // Act
        Action act = () => adapter.HandleExitCode(2, "Model not found");

        // Assert
        act.Should().Throw<ModelNotFoundException>()
            .WithMessage("*Modell nicht gefunden*");
    }

    [Fact]
    public void HandleExitCode_ExitCode3_ThrowsAudioDeviceException()
    {
        // Arrange
        var adapter = new WhisperCLIAdapter(_config);

        // Act
        Action act = () => adapter.HandleExitCode(3, "Audio device error");

        // Assert
        act.Should().Throw<AudioDeviceException>()
            .WithMessage("*Audio-Gerät nicht verfügbar*");
    }

    [Fact]
    public void HandleExitCode_ExitCode4_ThrowsSTTTimeoutException()
    {
        // Arrange
        var adapter = new WhisperCLIAdapter(_config);

        // Act
        Action act = () => adapter.HandleExitCode(4, "Timeout");

        // Assert
        act.Should().Throw<STTTimeoutException>()
            .WithMessage("*Transkription dauerte zu lange*");
    }

    [Fact]
    public void HandleExitCode_ExitCode5_ThrowsInvalidAudioException()
    {
        // Arrange
        var adapter = new WhisperCLIAdapter(_config);

        // Act
        Action act = () => adapter.HandleExitCode(5, "Invalid audio format");

        // Assert
        act.Should().Throw<InvalidAudioException>()
            .WithMessage("*Ungültige Audiodatei*");
    }

    [Fact]
    public void BuildCommandArguments_IncludesAllRequiredParameters()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "test.wav");
        var adapter = new WhisperCLIAdapter(_config);

        // Act
        var args = adapter.BuildCommandArguments(wavPath);

        // Assert
        args.Should().Contain($"--model");
        args.Should().Contain(_config.ModelPath);
        args.Should().Contain("--language");
        args.Should().Contain("de");
        args.Should().Contain("--output-format");
        args.Should().Contain("json");
        args.Should().Contain(wavPath);
    }
}
