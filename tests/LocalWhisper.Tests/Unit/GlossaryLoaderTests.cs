using FluentAssertions;
using LocalWhisper.Services;
using Serilog.Events;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for GlossaryLoader service.
/// Tests for US-063 (Glossary Support).
/// </summary>
public class GlossaryLoaderTests : IDisposable
{
    private readonly string _testDirectory;

    public GlossaryLoaderTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisper_GlossaryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
        // Initialize with Error level to reduce test output verbosity
        LocalWhisper.Core.AppLogger.Initialize(_testDirectory, LogEventLevel.Error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void LoadGlossary_ValidFile_ReturnsEntries()
    {
        // Arrange
        var glossaryPath = Path.Combine(_testDirectory, "glossary.txt");
        File.WriteAllText(glossaryPath, @"# Comment line
asap = as soon as possible
fyi = for your information
imho = in my humble opinion
");
        var loader = new GlossaryLoader();

        // Act
        var entries = loader.LoadGlossary(glossaryPath);

        // Assert
        entries.Should().HaveCount(3);
        entries.Should().ContainKey("asap").WhoseValue.Should().Be("as soon as possible");
        entries.Should().ContainKey("fyi").WhoseValue.Should().Be("for your information");
        entries.Should().ContainKey("imho").WhoseValue.Should().Be("in my humble opinion");
    }

    [Fact]
    public void LoadGlossary_EmptyFile_ReturnsEmptyDictionary()
    {
        // Arrange
        var glossaryPath = Path.Combine(_testDirectory, "empty.txt");
        File.WriteAllText(glossaryPath, "");
        var loader = new GlossaryLoader();

        // Act
        var entries = loader.LoadGlossary(glossaryPath);

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void LoadGlossary_FileDoesNotExist_ReturnsEmptyDictionary()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "doesnotexist.txt");
        var loader = new GlossaryLoader();

        // Act
        var entries = loader.LoadGlossary(nonExistentPath);

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void LoadGlossary_SkipsCommentLines()
    {
        // Arrange
        var glossaryPath = Path.Combine(_testDirectory, "comments.txt");
        File.WriteAllText(glossaryPath, @"# This is a comment
# Another comment
asap = as soon as possible
# More comments
fyi = for your information
");
        var loader = new GlossaryLoader();

        // Act
        var entries = loader.LoadGlossary(glossaryPath);

        // Assert
        entries.Should().HaveCount(2);
        entries.Should().ContainKey("asap");
        entries.Should().ContainKey("fyi");
    }

    [Fact]
    public void LoadGlossary_SkipsInvalidLines()
    {
        // Arrange
        var glossaryPath = Path.Combine(_testDirectory, "invalid.txt");
        File.WriteAllText(glossaryPath, @"asap = as soon as possible
invalid line without equals sign
fyi = for your information
another invalid line
");
        var loader = new GlossaryLoader();

        // Act
        var entries = loader.LoadGlossary(glossaryPath);

        // Assert
        entries.Should().HaveCount(2, "invalid lines should be skipped");
        entries.Should().ContainKey("asap");
        entries.Should().ContainKey("fyi");
    }

    [Fact]
    public void LoadGlossary_TruncatesAt500Entries()
    {
        // Arrange
        var glossaryPath = Path.Combine(_testDirectory, "large.txt");
        var lines = new List<string>();
        for (int i = 1; i <= 600; i++)
        {
            lines.Add($"abbr{i} = expansion {i}");
        }
        File.WriteAllText(glossaryPath, string.Join("\n", lines));
        var loader = new GlossaryLoader();

        // Act
        var entries = loader.LoadGlossary(glossaryPath);

        // Assert
        entries.Should().HaveCount(500, "glossary should be truncated at 500 entries");
        entries.Should().ContainKey("abbr1");
        entries.Should().ContainKey("abbr500");
        entries.Should().NotContainKey("abbr501");
    }

    [Fact]
    public void FormatGlossaryForPrompt_EmptyGlossary_ReturnsEmptyString()
    {
        // Arrange
        var loader = new GlossaryLoader();
        var entries = new Dictionary<string, string>();

        // Act
        var formatted = loader.FormatGlossaryForPrompt(entries);

        // Assert
        formatted.Should().BeEmpty();
    }

    [Fact]
    public void FormatGlossaryForPrompt_ValidEntries_FormatsCorrectly()
    {
        // Arrange
        var loader = new GlossaryLoader();
        var entries = new Dictionary<string, string>
        {
            { "asap", "as soon as possible" },
            { "fyi", "for your information" },
            { "imho", "in my humble opinion" }
        };

        // Act
        var formatted = loader.FormatGlossaryForPrompt(entries);

        // Assert
        formatted.Should().Contain("APPLY THESE ABBREVIATIONS:");
        formatted.Should().Contain("asap = as soon as possible");
        formatted.Should().Contain("fyi = for your information");
        formatted.Should().Contain("imho = in my humble opinion");
        formatted.Should().NotBeNullOrWhiteSpace("formatted glossary should not be empty");
    }

    [Fact]
    public void LoadGlossary_TrimsWhitespace()
    {
        // Arrange
        var glossaryPath = Path.Combine(_testDirectory, "whitespace.txt");
        File.WriteAllText(glossaryPath, @"  asap  =  as soon as possible
fyi=for your information
  # comment with spaces
");
        var loader = new GlossaryLoader();

        // Act
        var entries = loader.LoadGlossary(glossaryPath);

        // Assert
        entries.Should().HaveCount(2);
        entries.Should().ContainKey("asap").WhoseValue.Should().Be("as soon as possible");
        entries.Should().ContainKey("fyi").WhoseValue.Should().Be("for your information");
    }
}
