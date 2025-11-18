using FluentAssertions;
using LocalWhisper.Services;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for ModelValidator class.
/// </summary>
/// <remarks>
/// Tests cover US-041a: Wizard Step 2 - Model Verification (File Selection)
/// Scenario: Model file hash verification (SHA-1) (@Contract @CanRunInClaudeCode)
///
/// Tests verify:
/// - SHA-1 hash verification succeeds when hash matches
/// - SHA-1 hash verification fails when hash doesn't match
/// - Handles missing files gracefully
/// - Handles empty files correctly
///
/// See: docs/iterations/iteration-05a-wizard-core.md (Task 6)
/// See: docs/specification/user-stories-gherkin.md (US-041a, lines 726-733)
/// See: docs/reference/whisper-models.md (SHA-1 hashes)
/// </remarks>
public class ModelValidatorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ModelValidator _validator;

    public ModelValidatorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _validator = new ModelValidator();

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
    public async Task ValidateAsync_MatchingHash_ReturnsTrue()
    {
        // Arrange
        var testFile = CreateTestFileWithContent("Hello, Whisper!");
        var expectedHash = ComputeSHA1(testFile);

        // Act
        var result = await _validator.ValidateAsync(testFile, expectedHash);

        // Assert
        result.Should().BeTrue("the SHA-1 hash matches the expected value");
    }

    [Fact]
    public async Task ValidateAsync_MismatchingHash_ReturnsFalse()
    {
        // Arrange
        var testFile = CreateTestFileWithContent("Hello, Whisper!");
        var wrongHash = "0000000000000000000000000000000000000000";

        // Act
        var result = await _validator.ValidateAsync(testFile, wrongHash);

        // Assert
        result.Should().BeFalse("the SHA-1 hash does not match the expected value");
    }

    [Fact]
    public async Task ValidateAsync_EmptyFile_ComputesHashCorrectly()
    {
        // Arrange
        var testFile = CreateTestFileWithContent("");
        var expectedHash = "da39a3ee5e6b4b0d3255bfef95601890afd80709"; // SHA-1 of empty string

        // Act
        var result = await _validator.ValidateAsync(testFile, expectedHash);

        // Assert
        result.Should().BeTrue("empty file has known SHA-1 hash");
    }

    [Fact]
    public async Task ValidateAsync_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "does-not-exist.bin");
        var someHash = "0000000000000000000000000000000000000000";

        // Act
        Func<Task> act = async () => await _validator.ValidateAsync(nonExistentFile, someHash);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>(
            "validation should fail when file does not exist");
    }

    [Fact]
    public async Task ValidateAsync_HashComparison_IsCaseInsensitive()
    {
        // Arrange
        var testFile = CreateTestFileWithContent("Test content");
        var expectedHash = ComputeSHA1(testFile);

        // Act - use uppercase hash
        var resultUppercase = await _validator.ValidateAsync(testFile, expectedHash.ToUpperInvariant());
        // Act - use lowercase hash
        var resultLowercase = await _validator.ValidateAsync(testFile, expectedHash.ToLowerInvariant());

        // Assert
        resultUppercase.Should().BeTrue("hash comparison should be case-insensitive (uppercase)");
        resultLowercase.Should().BeTrue("hash comparison should be case-insensitive (lowercase)");
    }

    [Fact]
    public async Task ValidateAsync_RealModelHash_MatchesDocumentation()
    {
        // Arrange - Create a test file that simulates ggml-small.bin (first 1KB)
        // This tests that we're using SHA-1 correctly, NOT SHA-256
        var testFile = CreateTestFileWithContent("Simulated model content for hash testing");
        var computedHash = ComputeSHA1(testFile);

        // Act
        var result = await _validator.ValidateAsync(testFile, computedHash);

        // Assert
        result.Should().BeTrue("validation uses SHA-1 algorithm as documented");
        computedHash.Length.Should().Be(40, "SHA-1 produces 40-character hex string (160 bits / 4 bits per hex char)");
    }

    [Fact]
    public async Task ValidateAsync_LargeFile_CompletesWithinReasonableTime()
    {
        // Arrange - Create a 10 MB file to simulate model validation
        var testFile = CreateLargeTestFile(10 * 1024 * 1024); // 10 MB
        var expectedHash = ComputeSHA1(testFile);

        var startTime = DateTime.Now;

        // Act
        var result = await _validator.ValidateAsync(testFile, expectedHash);

        // Assert
        var elapsed = DateTime.Now - startTime;
        result.Should().BeTrue("validation should succeed for large files");
        elapsed.TotalSeconds.Should().BeLessThan(5, "SHA-1 computation for 10 MB should complete within 5 seconds");
    }

    [Fact]
    public async Task ValidateAsync_DummyFileWithRealModelHash_ReturnsFalse()
    {
        // Arrange - Test with actual ggml-small.bin hash from documentation
        // Create a dummy file that does NOT match the real model
        var knownHash = "55356645c2b361a969dfd0ef2c5a50d530afd8d5"; // Real ggml-small.bin hash
        var testFile = CreateTestFileWithContent("Not the real model");

        // Act
        var result = await _validator.ValidateAsync(testFile, knownHash);

        // Assert
        result.Should().BeFalse("dummy file does not match real model hash");
    }

    // Helper methods

    private string CreateTestFileWithContent(string content)
    {
        var filePath = Path.Combine(_testDirectory, $"test-{Guid.NewGuid()}.bin");
        File.WriteAllText(filePath, content);
        return filePath;
    }

    private string CreateLargeTestFile(int sizeInBytes)
    {
        var filePath = Path.Combine(_testDirectory, $"large-test-{Guid.NewGuid()}.bin");
        var random = new Random(42); // Seed for reproducibility
        var buffer = new byte[8192];

        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            int remaining = sizeInBytes;
            while (remaining > 0)
            {
                int toWrite = Math.Min(buffer.Length, remaining);
                random.NextBytes(buffer);
                fs.Write(buffer, 0, toWrite);
                remaining -= toWrite;
            }
        }

        return filePath;
    }

    private string ComputeSHA1(string filePath)
    {
        using var sha1 = SHA1.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = sha1.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
