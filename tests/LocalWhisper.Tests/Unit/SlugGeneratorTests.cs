using FluentAssertions;
using LocalWhisper.Utils;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for SlugGenerator class.
/// </summary>
/// <remarks>
/// Tests cover US-036: Slug Generation for History Filenames
/// - Kebab-case conversion
/// - German umlaut normalization
/// - Special character removal
/// - Truncation to max length
/// - Edge cases (empty, whitespace, special chars)
///
/// See: docs/iterations/iteration-04-clipboard-history-flyout.md (US-036)
/// See: docs/specification/user-stories-gherkin.md (lines 611-652)
/// </remarks>
public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Let me check on that and get back to you", "let-me-check-on-that-and-get-back-to-you")]
    [InlineData("Meeting at 3:00 PM", "meeting-at-3-00-pm")]
    [InlineData("Re: Project Alpha — status update", "re-project-alpha-status-update")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Quick dictation", "quick-dictation")]
    public void Generate_ValidText_ReturnsKebabCase(string input, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(input);

        // Assert
        result.Should().Be(expected, "slug should be in kebab-case");
    }

    [Theory]
    [InlineData("Äpfel, Öl & Übung", "apfel-ol-ubung")]
    [InlineData("Größe und Maß", "grosse-und-mass")]
    [InlineData("Überführung", "uberfuhrung")]
    [InlineData("Ärger mit Öfen", "arger-mit-ofen")]
    public void Generate_GermanUmlauts_NormalizesCorrectly(string input, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(input);

        // Assert
        result.Should().Be(expected, "German umlauts should be normalized (ä→a, ö→o, ü→u, ß→ss)");
    }

    [Fact]
    public void Generate_EmptyString_ReturnsDefaultSlug()
    {
        // Act
        var result = SlugGenerator.Generate("");

        // Assert
        result.Should().Be("transcript", "empty string should return default slug");
    }

    [Fact]
    public void Generate_WhitespaceOnly_ReturnsDefaultSlug()
    {
        // Act
        var result = SlugGenerator.Generate("   ");

        // Assert
        result.Should().Be("transcript", "whitespace-only should return default slug");
    }

    [Fact]
    public void Generate_SpecialCharactersOnly_ReturnsDefaultSlug()
    {
        // Act
        var result = SlugGenerator.Generate("!@#$%^&*()");

        // Assert
        result.Should().Be("transcript", "special characters only should return default slug");
    }

    [Fact]
    public void Generate_LongText_TruncatesToMaxLength()
    {
        // Arrange
        var longText = "This is a very long sentence that definitely exceeds fifty characters and should be truncated to meet the maximum length requirement";

        // Act
        var result = SlugGenerator.Generate(longText, maxLength: 50);

        // Assert
        result.Length.Should().BeLessOrEqualTo(50, "slug should be truncated to max length");
        result.Should().NotEndWith("-", "truncated slug should not end with hyphen");
    }

    [Fact]
    public void Generate_MultipleHyphens_CompressesToSingle()
    {
        // Act
        var result = SlugGenerator.Generate("Hello-----world");

        // Assert
        result.Should().Be("hello-world", "multiple consecutive hyphens should be compressed to single hyphen");
    }

    [Fact]
    public void Generate_LeadingTrailingSpaces_TrimsCorrectly()
    {
        // Act
        var result = SlugGenerator.Generate("  Hello world  ");

        // Assert
        result.Should().Be("hello-world", "leading and trailing spaces should be trimmed");
    }

    [Theory]
    [InlineData("Numbers 123 and 456", "numbers-123-and-456")]
    [InlineData("Version 2.0 Release", "version-2-0-release")]
    [InlineData("Test (2024)", "test-2024")]
    public void Generate_NumbersAndParentheses_HandlesCorrectly(string input, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(input);

        // Assert
        result.Should().Be(expected, "numbers should be preserved, special chars removed");
    }

    [Fact]
    public void Generate_MixedCasing_ConvertsToLowercase()
    {
        // Act
        var result = SlugGenerator.Generate("CamelCase And UPPERCASE");

        // Assert
        result.Should().Be("camelcase-and-uppercase", "all text should be lowercase");
    }

    [Fact]
    public void Generate_WithCustomMaxLength_RespectsLimit()
    {
        // Arrange
        var text = "Short text for testing custom max length parameter";

        // Act
        var result = SlugGenerator.Generate(text, maxLength: 20);

        // Assert
        result.Length.Should().BeLessOrEqualTo(20, "should respect custom max length");
    }

    [Theory]
    [InlineData("Café au lait", "cafe-au-lait")]
    [InlineData("Naïve résumé", "naive-resume")]
    public void Generate_FrenchAccents_Normalizes(string input, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(input);

        // Assert
        result.Should().Be(expected, "French accents should be normalized");
    }

    [Fact]
    public void Generate_OnlyHyphens_ReturnsDefaultSlug()
    {
        // Act
        var result = SlugGenerator.Generate("---");

        // Assert
        result.Should().Be("transcript", "only hyphens should return default slug");
    }

    [Fact]
    public void Generate_EndsWithHyphen_TrimsTrailingHyphen()
    {
        // Act
        var result = SlugGenerator.Generate("Hello world-");

        // Assert
        result.Should().Be("hello-world", "trailing hyphen should be removed");
    }

    [Fact]
    public void Generate_StartsWithHyphen_TrimsLeadingHyphen()
    {
        // Act
        var result = SlugGenerator.Generate("-Hello world");

        // Assert
        result.Should().Be("hello-world", "leading hyphen should be removed");
    }
}
