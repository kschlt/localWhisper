using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using LocalWhisper.Models;
using LocalWhisper.Services;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for HistoryWriter class.
/// </summary>
/// <remarks>
/// Tests cover US-031: History File Creation
/// - Markdown file generation with YAML front-matter
/// - Directory structure creation (YYYY/YYYY-MM/YYYY-MM-DD)
/// - Duplicate filename handling
/// - Graceful error handling
///
/// See: docs/iterations/iteration-04-clipboard-history-flyout.md (US-031)
/// See: docs/specification/user-stories-gherkin.md (lines 447-495)
/// </remarks>
public class HistoryWriterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly HistoryWriter _historyWriter;

    public HistoryWriterTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Initialize AppLogger with Error level to reduce test output verbosity
        LocalWhisper.Core.AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);

        _historyWriter = new HistoryWriter(_testDirectory);
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
    public async Task WriteAsync_CreatesFileWithCorrectPath()
    {
        // Arrange
        var entry = new HistoryEntry
        {
            Created = new DateTimeOffset(2025, 9, 17, 14, 30, 22, TimeSpan.Zero),
            Text = "Let me check on that and get back to you tomorrow morning.",
            Language = "en",
            SttModel = "whisper-small",
            DurationSeconds = 4.8,
            PostProcessed = false
        };

        // Act
        var filePath = await _historyWriter.WriteAsync(entry, _testDirectory);

        // Assert
        File.Exists(filePath).Should().BeTrue("history file should exist");

        // Verify path structure matches: history\YYYY\YYYY-MM\YYYY-MM-DD\YYYYMMDD_HHmmssfff_{slug}.md (Windows paths)
        var relativePath = Path.GetRelativePath(_testDirectory, filePath);
        relativePath.Should().MatchRegex(@"^history\\\d{4}\\\d{4}-\d{2}\\\d{4}-\d{2}-\d{2}\\\d{8}_\d{9}_.+\.md$",
            "path should match expected structure");
    }

    [Fact]
    public async Task WriteAsync_FileContainsCorrectFrontMatter()
    {
        // Arrange
        var created = new DateTimeOffset(2025, 9, 17, 14, 30, 22, TimeSpan.Zero);
        var entry = new HistoryEntry
        {
            Created = created,
            Text = "Test transcript",
            Language = "de",
            SttModel = "whisper-large",
            DurationSeconds = 3.5,
            PostProcessed = false
        };

        // Act
        var filePath = await _historyWriter.WriteAsync(entry, _testDirectory);
        var content = File.ReadAllText(filePath);

        // Assert
        content.Should().Contain("---", "should have YAML front-matter delimiters");
        content.Should().Contain($"created: {created:yyyy-MM-ddTHH:mm:ssK}", "should contain ISO 8601 timestamp");
        content.Should().Contain("lang: de", "should contain language");
        content.Should().Contain("stt_model: whisper-large", "should contain model");
        content.Should().Contain("duration_sec: 3.5", "should contain duration");
        content.Should().Contain("post_processed: false", "should contain post-processing flag");
    }

    [Fact]
    public async Task WriteAsync_FileContainsCorrectBody()
    {
        // Arrange
        var entry = new HistoryEntry
        {
            Created = DateTimeOffset.Now,
            Text = "This is the transcript text that should appear in the body.",
            Language = "en",
            SttModel = "whisper-small",
            DurationSeconds = 2.0
        };

        // Act
        var filePath = await _historyWriter.WriteAsync(entry, _testDirectory);
        var content = File.ReadAllText(filePath);

        // Assert
        content.Should().Contain("# Diktat –", "should contain heading");
        content.Should().Contain("This is the transcript text that should appear in the body.",
            "should contain transcript text");
    }

    [Fact]
    public async Task WriteAsync_CreatesDirectoryStructure()
    {
        // Arrange
        var entry = new HistoryEntry
        {
            Created = new DateTimeOffset(2025, 3, 15, 10, 20, 30, TimeSpan.Zero),
            Text = "Directory test",
            Language = "en",
            SttModel = "whisper-small",
            DurationSeconds = 1.0
        };

        // Act
        var filePath = await _historyWriter.WriteAsync(entry, _testDirectory);

        // Assert
        var expectedDir = Path.Combine(_testDirectory, "history", "2025", "2025-03", "2025-03-15");
        Directory.Exists(expectedDir).Should().BeTrue("directory structure should be created");
    }

    [Fact]
    public async Task WriteAsync_DuplicateSlug_AppendsCounter()
    {
        // Arrange
        // Use identical timestamps to ensure filename collision (to test duplicate handling)
        var timestamp = new DateTimeOffset(2025, 9, 17, 14, 30, 22, 500, TimeSpan.Zero);

        var entry1 = new HistoryEntry
        {
            Created = timestamp,
            Text = "Let me check on that",
            Language = "en",
            SttModel = "whisper-small",
            DurationSeconds = 1.0
        };

        var entry2 = new HistoryEntry
        {
            Created = timestamp, // SAME timestamp = same filename prefix
            Text = "Let me check on that", // Same text = same slug
            Language = "en",
            SttModel = "whisper-small",
            DurationSeconds = 1.0
        };

        // Act
        var filePath1 = await _historyWriter.WriteAsync(entry1, _testDirectory);
        var filePath2 = await _historyWriter.WriteAsync(entry2, _testDirectory);

        // Assert
        File.Exists(filePath1).Should().BeTrue("first file should exist");
        File.Exists(filePath2).Should().BeTrue("second file should exist");
        filePath1.Should().NotBe(filePath2, "duplicate slugs should have different filenames");

        var filename2 = Path.GetFileName(filePath2);
        filename2.Should().MatchRegex(@"_\d+\.md$", "duplicate should have counter appended");
    }

    [Fact]
    public async Task WriteAsync_FileIsUTF8Encoded()
    {
        // Arrange
        var entry = new HistoryEntry
        {
            Created = DateTimeOffset.Now,
            Text = "German text: Äpfel, Öl, Übung",
            Language = "de",
            SttModel = "whisper-small",
            DurationSeconds = 1.0
        };

        // Act
        var filePath = await _historyWriter.WriteAsync(entry, _testDirectory);

        // Assert
        var content = File.ReadAllText(filePath);
        content.Should().Contain("Äpfel", "should preserve German umlauts (UTF-8)");
        content.Should().Contain("Öl");
        content.Should().Contain("Übung");
    }

    [Fact]
    public async Task WriteAsync_SlugGeneration_WorksCorrectly()
    {
        // Arrange
        var entry = new HistoryEntry
        {
            Created = DateTimeOffset.Now,
            Text = "Meeting at 3:00 PM",
            Language = "en",
            SttModel = "whisper-small",
            DurationSeconds = 1.0
        };

        // Act
        var filePath = await _historyWriter.WriteAsync(entry, _testDirectory);

        // Assert
        var filename = Path.GetFileName(filePath);
        filename.Should().Contain("meeting-at-3-00-pm", "slug should be generated from text");
    }

    [Fact]
    public async Task WriteAsync_ReturnsAbsolutePath()
    {
        // Arrange
        var entry = new HistoryEntry
        {
            Created = DateTimeOffset.Now,
            Text = "Test",
            Language = "en",
            SttModel = "whisper-small",
            DurationSeconds = 1.0
        };

        // Act
        var filePath = await _historyWriter.WriteAsync(entry, _testDirectory);

        // Assert
        Path.IsPathRooted(filePath).Should().BeTrue("returned path should be absolute");
        filePath.Should().StartWith(_testDirectory, "path should be within data root");
    }

    [Fact]
    public async Task WriteAsync_MultipleEntries_AllSucceed()
    {
        // Arrange
        var entries = Enumerable.Range(1, 5).Select(i => new HistoryEntry
        {
            Created = DateTimeOffset.Now.AddSeconds(i),
            Text = $"Dictation number {i}",
            Language = "en",
            SttModel = "whisper-small",
            DurationSeconds = 1.0
        }).ToList();

        // Act
        var filePaths = new List<string>();
        foreach (var entry in entries)
        {
            var path = await _historyWriter.WriteAsync(entry, _testDirectory);
            filePaths.Add(path);
        }

        // Assert
        filePaths.Should().HaveCount(5, "all entries should be written");
        filePaths.Should().OnlyHaveUniqueItems("all file paths should be unique");
        filePaths.Should().OnlyContain(path => File.Exists(path), "all files should exist");
    }
}
