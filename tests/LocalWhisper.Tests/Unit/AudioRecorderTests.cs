using System.IO;
using FluentAssertions;
using LocalWhisper.Services;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for AudioRecorder class.
/// </summary>
/// <remarks>
/// Tests cover US-010: Audio Recording (WASAPI)
/// - Start/stop recording
/// - WAV file creation with correct format
/// - Timestamp filename generation
/// - Microphone availability check
/// - Error handling for missing microphone
///
/// See: docs/iterations/iteration-02-audio-recording.md (US-010)
/// See: docs/specification/user-stories-gherkin.md (lines 146-182)
/// </remarks>
public class AudioRecorderTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly AudioRecorder _audioRecorder;

    public AudioRecorderTests()
    {
        // Create temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Initialize AudioRecorder
        _audioRecorder = new AudioRecorder();
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
    public void IsMicrophoneAvailable_ReturnsTrueWhenDevicePresent()
    {
        // Act
        var isAvailable = _audioRecorder.IsMicrophoneAvailable();

        // Assert
        // Note: This test may fail in CI environment without audio device
        // Mark as [Fact(Skip = "Requires audio device")] if needed
        isAvailable.Should().BeTrue("default audio input device should be available");
    }

    [Fact]
    public void StartRecording_CreatesRecordingSession()
    {
        // Arrange
        if (!_audioRecorder.IsMicrophoneAvailable())
        {
            // Skip test if no microphone available
            return;
        }

        // Act
        Action act = () => _audioRecorder.StartRecording(_testDirectory);

        // Assert
        act.Should().NotThrow("starting recording should succeed when microphone is available");
        _audioRecorder.IsRecording.Should().BeTrue("IsRecording flag should be set after start");
    }

    [Fact]
    public void StopRecording_SavesWavFile()
    {
        // Arrange
        if (!_audioRecorder.IsMicrophoneAvailable())
        {
            return; // Skip if no microphone
        }

        _audioRecorder.StartRecording(_testDirectory);

        // Simulate short recording (100ms)
        Thread.Sleep(100);

        // Act
        var wavFilePath = _audioRecorder.StopRecording();

        // Assert
        wavFilePath.Should().NotBeNullOrEmpty("StopRecording should return WAV file path");
        File.Exists(wavFilePath).Should().BeTrue("WAV file should exist after recording");
        _audioRecorder.IsRecording.Should().BeFalse("IsRecording flag should be cleared after stop");
    }

    [Fact]
    public void SavedWavFile_HasTimestampFilename()
    {
        // Arrange
        if (!_audioRecorder.IsMicrophoneAvailable())
        {
            return;
        }

        _audioRecorder.StartRecording(_testDirectory);
        Thread.Sleep(50);

        // Act
        var wavFilePath = _audioRecorder.StopRecording();

        // Assert
        var filename = Path.GetFileName(wavFilePath);
        filename.Should().MatchRegex(@"^rec_\d{8}_\d{9}\.wav$",
            "filename should match pattern rec_YYYYMMDD_HHmmssfff.wav");
    }

    [Fact]
    public void SavedWavFile_HasCorrectFormat()
    {
        // Arrange
        if (!_audioRecorder.IsMicrophoneAvailable())
        {
            return;
        }

        _audioRecorder.StartRecording(_testDirectory);
        Thread.Sleep(500); // Record for 500ms to get measurable data

        // Act
        var wavFilePath = _audioRecorder.StopRecording();

        // Assert - Read WAV header and verify format
        using (var fs = new FileStream(wavFilePath, FileMode.Open, FileAccess.Read))
        using (var br = new BinaryReader(fs))
        {
            // RIFF header
            var riffHeader = new string(br.ReadChars(4));
            riffHeader.Should().Be("RIFF", "WAV file should start with RIFF header");

            var fileSize = br.ReadInt32(); // File size - 8
            fileSize.Should().BeGreaterThan(0);

            var waveHeader = new string(br.ReadChars(4));
            waveHeader.Should().Be("WAVE", "WAV file should have WAVE format");

            // fmt chunk
            var fmtHeader = new string(br.ReadChars(4));
            fmtHeader.Should().Be("fmt ", "WAV file should have fmt chunk");

            var fmtChunkSize = br.ReadInt32();
            fmtChunkSize.Should().Be(16, "PCM format should have 16-byte fmt chunk");

            var audioFormat = br.ReadInt16();
            audioFormat.Should().Be(1, "audio format should be 1 (PCM)");

            var channels = br.ReadInt16();
            channels.Should().Be(1, "should be mono (1 channel)");

            var sampleRate = br.ReadInt32();
            sampleRate.Should().Be(16000, "sample rate should be 16000 Hz for Whisper");

            var byteRate = br.ReadInt32();
            byteRate.Should().Be(32000, "byte rate should be 32000 (16000 Hz * 1 ch * 16 bits / 8)");

            var blockAlign = br.ReadInt16();
            blockAlign.Should().Be(2, "block align should be 2 (1 ch * 16 bits / 8)");

            var bitsPerSample = br.ReadInt16();
            bitsPerSample.Should().Be(16, "bit depth should be 16 bits");
        }
    }

    [Fact]
    public void SavedWavFile_HasReasonableSize()
    {
        // Arrange
        if (!_audioRecorder.IsMicrophoneAvailable())
        {
            return;
        }

        _audioRecorder.StartRecording(_testDirectory);
        Thread.Sleep(1000); // Record for 1 second

        // Act
        var wavFilePath = _audioRecorder.StopRecording();

        // Assert
        var fileInfo = new FileInfo(wavFilePath);

        // Expected size for 1 second: ~32 KB data + 44 bytes header = ~32 KB total
        // Allow tolerance for timing variance: 25-40 KB
        fileInfo.Length.Should().BeInRange(25 * 1024, 40 * 1024,
            "1-second recording should be approximately 32 KB (16kHz * 1ch * 16bit / 8)");
    }

    [Fact]
    public void StopRecording_WithoutStart_ThrowsInvalidOperationException()
    {
        // Act
        Action act = () => _audioRecorder.StopRecording();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not recording*", "stopping without starting should throw");
    }

    [Fact]
    public void StartRecording_WhenAlreadyRecording_ThrowsInvalidOperationException()
    {
        // Arrange
        if (!_audioRecorder.IsMicrophoneAvailable())
        {
            return;
        }

        _audioRecorder.StartRecording(_testDirectory);

        // Act
        Action act = () => _audioRecorder.StartRecording(_testDirectory);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already recording*", "starting when already recording should throw");

        // Cleanup
        _audioRecorder.StopRecording();
    }

    [Fact]
    public void SavedWavFile_IsInCorrectDirectory()
    {
        // Arrange
        if (!_audioRecorder.IsMicrophoneAvailable())
        {
            return;
        }

        _audioRecorder.StartRecording(_testDirectory);
        Thread.Sleep(50);

        // Act
        var wavFilePath = _audioRecorder.StopRecording();

        // Assert
        var directory = Path.GetDirectoryName(wavFilePath);
        directory.Should().Be(_testDirectory, "WAV file should be saved in specified directory");
    }
}
