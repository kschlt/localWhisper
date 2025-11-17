using System;
using System.IO;
using LocalWhisper.Core;
using NAudio.Wave;

namespace LocalWhisper.Services;

/// <summary>
/// Audio recording service using Windows audio APIs.
/// </summary>
/// <remarks>
/// Implements US-010: Audio Recording (WASAPI)
/// - Records audio from default microphone
/// - Saves WAV files in 16kHz, mono, 16-bit PCM format (Whisper requirement)
/// - Generates timestamp-based filenames
///
/// See: docs/iterations/iteration-02-audio-recording.md (US-010)
/// See: docs/specification/functional-requirements.md (FR-011)
/// </remarks>
public class AudioRecorder : IDisposable
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _waveWriter;
    private string? _outputFilePath;
    private bool _isRecording;

    // Target format for Whisper: 16kHz, mono, 16-bit PCM
    private static readonly WaveFormat TargetFormat = new WaveFormat(16000, 16, 1);

    /// <summary>
    /// Gets whether recording is currently active.
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// Check if a microphone (default audio input device) is available.
    /// </summary>
    /// <returns>True if microphone is available, false otherwise</returns>
    public bool IsMicrophoneAvailable()
    {
        try
        {
            // Try to enumerate capture devices
            var deviceCount = WaveIn.DeviceCount;
            return deviceCount > 0;
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to check microphone availability", new { Exception = ex.Message });
            return false;
        }
    }

    /// <summary>
    /// Start audio recording from the default microphone.
    /// </summary>
    /// <param name="outputDirectory">Directory to save WAV file</param>
    /// <exception cref="InvalidOperationException">If already recording or no microphone available</exception>
    public void StartRecording(string outputDirectory)
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Already recording. Stop the current recording before starting a new one.");
        }

        if (!IsMicrophoneAvailable())
        {
            throw new InvalidOperationException("No microphone available. Please connect an audio input device.");
        }

        try
        {
            // Generate timestamp-based filename: rec_YYYYMMDD_HHmmssfff.wav
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            var filename = $"rec_{timestamp}.wav";
            _outputFilePath = Path.Combine(outputDirectory, filename);

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Initialize WaveInEvent with target format: 16kHz, mono, 16-bit
            _waveIn = new WaveInEvent
            {
                WaveFormat = TargetFormat,
                DeviceNumber = 0, // Default device
                BufferMilliseconds = 50 // Low latency buffer
            };

            // Create WaveFileWriter with target format
            _waveWriter = new WaveFileWriter(_outputFilePath, TargetFormat);

            // Attach data available event
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            // Start capturing
            _waveIn.StartRecording();
            _isRecording = true;

            AppLogger.LogInformation("Audio recording started", new
            {
                OutputFile = _outputFilePath,
                SampleRate = TargetFormat.SampleRate,
                Channels = TargetFormat.Channels,
                BitsPerSample = TargetFormat.BitsPerSample
            });
        }
        catch (Exception ex)
        {
            // Cleanup on error
            Cleanup();
            AppLogger.LogError("Failed to start audio recording", ex);
            throw new InvalidOperationException($"Failed to start recording: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Stop audio recording and finalize WAV file.
    /// </summary>
    /// <returns>Path to the saved WAV file</returns>
    /// <exception cref="InvalidOperationException">If not currently recording</exception>
    public string StopRecording()
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Not currently recording. Call StartRecording() first.");
        }

        try
        {
            // Stop capturing
            _waveIn?.StopRecording();
            _isRecording = false;

            // Give a moment for final buffer to flush
            System.Threading.Thread.Sleep(50);

            // Flush and close WAV file
            _waveWriter?.Flush();
            _waveWriter?.Dispose();
            _waveWriter = null;

            var savedFilePath = _outputFilePath!;

            var fileInfo = new FileInfo(savedFilePath);
            AppLogger.LogInformation("Audio recording stopped", new
            {
                SavedFile = savedFilePath,
                FileSize = fileInfo.Length
            });

            // Cleanup capture device
            Cleanup();

            return savedFilePath;
        }
        catch (Exception ex)
        {
            Cleanup();
            AppLogger.LogError("Failed to stop audio recording", ex);
            throw new InvalidOperationException($"Failed to stop recording: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Event handler for captured audio data.
    /// </summary>
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_waveWriter != null && e.BytesRecorded > 0)
        {
            // Write captured audio data to WAV file
            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    /// <summary>
    /// Event handler for recording stopped.
    /// </summary>
    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            AppLogger.LogError("Recording stopped due to error", e.Exception);
        }
    }

    /// <summary>
    /// Cleanup resources.
    /// </summary>
    private void Cleanup()
    {
        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.RecordingStopped -= OnRecordingStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }

        _waveWriter?.Dispose();
        _waveWriter = null;
        _isRecording = false;
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        if (_isRecording)
        {
            try
            {
                StopRecording();
            }
            catch
            {
                // Best effort cleanup
            }
        }

        Cleanup();
        GC.SuppressFinalize(this);
    }
}
