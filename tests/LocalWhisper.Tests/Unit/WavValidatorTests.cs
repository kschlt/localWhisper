using System.IO;
using FluentAssertions;
using LocalWhisper.Utils;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for WavValidator class.
/// </summary>
/// <remarks>
/// Tests cover US-011: WAV File Validation
/// - Valid WAV file passes validation
/// - Corrupted header detection
/// - Wrong format detection (sample rate, channels, bit depth)
/// - Invalid files moved to failed/ subdirectory
///
/// See: docs/iterations/iteration-02-audio-recording.md (US-011)
/// See: docs/specification/user-stories-gherkin.md (lines 184-213)
/// </remarks>
[Trait("Batch", "1")]
public class WavValidatorTests : IDisposable
{
    private readonly string _testDirectory;

    public WavValidatorTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
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
    public void ValidateWavFile_ValidFile_ReturnsTrue()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "test.wav");
        CreateValidWavFile(wavPath);

        // Act
        var result = WavValidator.ValidateWavFile(wavPath, out var errorMessage);

        // Assert
        result.Should().BeTrue("valid WAV file should pass validation");
        errorMessage.Should().BeNullOrEmpty("no error message for valid file");
    }

    [Fact]
    public void ValidateWavFile_CorruptedHeader_ReturnsFalse()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "corrupted.wav");
        // Create 44-byte file with invalid RIFF header (should be "RIFF" but is "XXXX")
        var corruptedData = new byte[44];
        corruptedData[0] = (byte)'X'; // Invalid: should be 'R'
        corruptedData[1] = (byte)'X'; // Invalid: should be 'I'
        corruptedData[2] = (byte)'X'; // Invalid: should be 'F'
        corruptedData[3] = (byte)'X'; // Invalid: should be 'F'
        File.WriteAllBytes(wavPath, corruptedData);

        // Act
        var result = WavValidator.ValidateWavFile(wavPath, out var errorMessage);

        // Assert
        result.Should().BeFalse("corrupted file should fail validation");
        errorMessage.Should().Contain("Invalid WAV file header", "error message should explain the issue");
    }

    [Fact]
    public void ValidateWavFile_WrongSampleRate_ReturnsFalse()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "wrong_samplerate.wav");
        CreateWavFileWithSampleRate(wavPath, 44100); // Wrong sample rate (should be 16000)

        // Act
        var result = WavValidator.ValidateWavFile(wavPath, out var errorMessage);

        // Assert
        result.Should().BeFalse("file with wrong sample rate should fail validation");
        errorMessage.Should().Contain("sample rate", "error message should mention sample rate");
        errorMessage.Should().Contain("16000", "error message should specify expected sample rate");
    }

    [Fact]
    public void ValidateWavFile_WrongChannelCount_ReturnsFalse()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "wrong_channels.wav");
        CreateWavFileWithChannels(wavPath, 2); // Stereo (should be mono = 1)

        // Act
        var result = WavValidator.ValidateWavFile(wavPath, out var errorMessage);

        // Assert
        result.Should().BeFalse("file with wrong channel count should fail validation");
        errorMessage.Should().Contain("channel", "error message should mention channels");
        errorMessage.Should().Contain("1", "error message should specify expected mono");
    }

    [Fact]
    public void ValidateWavFile_WrongBitDepth_ReturnsFalse()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "wrong_bitdepth.wav");
        CreateWavFileWithBitDepth(wavPath, 8); // 8-bit (should be 16-bit)

        // Act
        var result = WavValidator.ValidateWavFile(wavPath, out var errorMessage);

        // Assert
        result.Should().BeFalse("file with wrong bit depth should fail validation");
        errorMessage.Should().Contain("bit", "error message should mention bit depth");
        errorMessage.Should().Contain("16", "error message should specify expected bit depth");
    }

    [Fact]
    public void ValidateWavFile_NonPcmFormat_ReturnsFalse()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "non_pcm.wav");
        CreateWavFileWithFormat(wavPath, 3); // Float format (should be PCM = 1)

        // Act
        var result = WavValidator.ValidateWavFile(wavPath, out var errorMessage);

        // Assert
        result.Should().BeFalse("non-PCM file should fail validation");
        errorMessage.Should().Contain("PCM", "error message should mention PCM format");
    }

    [Fact]
    public void ValidateWavFile_FileTooSmall_ReturnsFalse()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "too_small.wav");
        File.WriteAllBytes(wavPath, new byte[100]); // Too small to be valid WAV

        // Act
        var result = WavValidator.ValidateWavFile(wavPath, out var errorMessage);

        // Assert
        result.Should().BeFalse("file too small should fail validation");
        errorMessage.Should().NotBeNullOrEmpty("should have error message");
    }

    [Fact]
    public void MoveToFailedDirectory_CreatesFailedFolderAndMovesFile()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "invalid.wav");
        File.WriteAllBytes(wavPath, new byte[] { 0x00, 0x01 });

        // Act
        WavValidator.MoveToFailedDirectory(wavPath);

        // Assert
        var failedDir = Path.Combine(_testDirectory, "failed");
        Directory.Exists(failedDir).Should().BeTrue("failed/ directory should be created");

        var movedPath = Path.Combine(failedDir, "invalid.wav");
        File.Exists(movedPath).Should().BeTrue("file should be moved to failed/ directory");
        File.Exists(wavPath).Should().BeFalse("original file should no longer exist");
    }

    [Fact]
    public void MoveToFailedDirectory_PreservesFilename()
    {
        // Arrange
        var wavPath = Path.Combine(_testDirectory, "rec_20250117_123456789.wav");
        File.WriteAllBytes(wavPath, new byte[] { 0x00, 0x01 });

        // Act
        WavValidator.MoveToFailedDirectory(wavPath);

        // Assert
        var movedPath = Path.Combine(_testDirectory, "failed", "rec_20250117_123456789.wav");
        File.Exists(movedPath).Should().BeTrue("filename should be preserved");
    }

    // Helper methods to create test WAV files

    private void CreateValidWavFile(string path)
    {
        CreateWavFile(path, sampleRate: 16000, channels: 1, bitsPerSample: 16, audioFormat: 1);
    }

    private void CreateWavFileWithSampleRate(string path, int sampleRate)
    {
        CreateWavFile(path, sampleRate: sampleRate, channels: 1, bitsPerSample: 16, audioFormat: 1);
    }

    private void CreateWavFileWithChannels(string path, short channels)
    {
        CreateWavFile(path, sampleRate: 16000, channels: channels, bitsPerSample: 16, audioFormat: 1);
    }

    private void CreateWavFileWithBitDepth(string path, short bitsPerSample)
    {
        CreateWavFile(path, sampleRate: 16000, channels: 1, bitsPerSample: bitsPerSample, audioFormat: 1);
    }

    private void CreateWavFileWithFormat(string path, short audioFormat)
    {
        CreateWavFile(path, sampleRate: 16000, channels: 1, bitsPerSample: 16, audioFormat: audioFormat);
    }

    private void CreateWavFile(string path, int sampleRate, short channels, short bitsPerSample, short audioFormat)
    {
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            // RIFF header
            bw.Write(new char[] { 'R', 'I', 'F', 'F' });
            bw.Write(0); // File size - 8 (placeholder)
            bw.Write(new char[] { 'W', 'A', 'V', 'E' });

            // fmt chunk
            bw.Write(new char[] { 'f', 'm', 't', ' ' });
            bw.Write(16); // fmt chunk size
            bw.Write(audioFormat); // Audio format (1 = PCM)
            bw.Write(channels); // Channels
            bw.Write(sampleRate); // Sample rate
            bw.Write(sampleRate * channels * bitsPerSample / 8); // Byte rate
            bw.Write((short)(channels * bitsPerSample / 8)); // Block align
            bw.Write(bitsPerSample); // Bits per sample

            // data chunk
            bw.Write(new char[] { 'd', 'a', 't', 'a' });
            bw.Write(1000); // Data size (dummy data)
            bw.Write(new byte[1000]); // Dummy audio data

            // Update file size in header
            var fileSize = (int)fs.Length - 8;
            fs.Seek(4, SeekOrigin.Begin);
            bw.Write(fileSize);
        }
    }
}
