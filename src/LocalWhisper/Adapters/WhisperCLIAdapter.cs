using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LocalWhisper.Core;
using LocalWhisper.Models;

namespace LocalWhisper.Adapters;

/// <summary>
/// Adapter for invoking Whisper CLI as a subprocess for speech-to-text.
/// </summary>
/// <remarks>
/// Implements US-020: STT via Whisper CLI
/// - Invokes Whisper CLI with proper arguments
/// - Parses JSON output
/// - Maps exit codes to exceptions
/// - Enforces timeout
///
/// See: docs/iterations/iteration-03-stt-whisper.md
/// See: docs/specification/functional-requirements.md (FR-012)
/// </remarks>
public class WhisperCLIAdapter
{
    private readonly WhisperConfig _config;

    public WhisperCLIAdapter(WhisperConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Transcribe audio file using Whisper CLI.
    /// </summary>
    /// <param name="wavFilePath">Path to WAV file to transcribe</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>STT result with transcript</returns>
    /// <exception cref="STTException">If transcription fails</exception>
    public async Task<STTResult> TranscribeAsync(string wavFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(wavFilePath))
        {
            throw new STTException($"WAV file not found: {wavFilePath}");
        }

        // Generate output JSON path
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        var outputDirectory = Path.GetDirectoryName(wavFilePath) ?? throw new InvalidOperationException("Cannot determine output directory");
        var outputJsonPath = Path.Combine(outputDirectory, $"stt_result_{timestamp}.json");

        // Build command arguments
        var arguments = BuildCommandArguments(wavFilePath, outputJsonPath);

        AppLogger.LogInformation("Invoking Whisper CLI", new
        {
            CLIPath = _config.CLIPath,
            Arguments = arguments,
            InputFile = wavFilePath,
            OutputFile = outputJsonPath,
            Timeout = _config.TimeoutSeconds
        });

        // Execute Whisper CLI subprocess
        var (exitCode, stdout, stderr) = await ExecuteProcessAsync(
            _config.CLIPath,
            arguments,
            TimeSpan.FromSeconds(_config.TimeoutSeconds),
            cancellationToken
        );

        // Handle exit code
        HandleExitCode(exitCode, stderr);

        // Parse JSON output
        var result = ParseJSONOutput(outputJsonPath);

        AppLogger.LogInformation("Transcription completed", new
        {
            Text = result.Text,
            Language = result.Language,
            Duration = result.DurationSeconds,
            IsEmpty = result.IsEmpty
        });

        return result;
    }

    /// <summary>
    /// Build command-line arguments for Whisper CLI.
    /// </summary>
    public string BuildCommandArguments(string wavFilePath, string? outputJsonPath = null)
    {
        var args = new StringBuilder();

        // Model path
        args.Append($"--model \"{_config.ModelPath}\" ");

        // Language
        args.Append($"--language {_config.Language} ");

        // Output format
        args.Append("--output-format json ");

        // Output file (if specified)
        if (!string.IsNullOrEmpty(outputJsonPath))
        {
            args.Append($"--output-file \"{outputJsonPath}\" ");
        }

        // Input WAV file
        args.Append($"\"{wavFilePath}\"");

        return args.ToString();
    }

    /// <summary>
    /// Parse Whisper CLI JSON output.
    /// </summary>
    /// <param name="jsonPath">Path to JSON output file</param>
    /// <returns>Parsed STT result</returns>
    /// <exception cref="STTException">If JSON is invalid or file not found</exception>
    public STTResult ParseJSONOutput(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new STTException($"STT output file not found: {jsonPath}");
        }

        try
        {
            var jsonContent = File.ReadAllText(jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<STTResult>(jsonContent, options);

            if (result == null)
            {
                throw new STTException("Failed to deserialize STT result: null result");
            }

            return result;
        }
        catch (JsonException ex)
        {
            var fileContent = File.ReadAllText(jsonPath);
            AppLogger.LogError("Invalid JSON format in STT output", ex, new { FilePath = jsonPath, Content = fileContent });
            throw new STTException($"Invalid JSON format in STT output: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not STTException)
        {
            AppLogger.LogError("Failed to parse STT output", ex);
            throw new STTException($"Failed to parse STT output: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Handle Whisper CLI exit code and throw appropriate exception.
    /// </summary>
    /// <param name="exitCode">Process exit code</param>
    /// <param name="stderr">Standard error output</param>
    /// <exception cref="STTException">Appropriate exception based on exit code</exception>
    public void HandleExitCode(int exitCode, string stderr)
    {
        switch (exitCode)
        {
            case 0:
                // Success
                return;

            case 1:
                AppLogger.LogError("Whisper CLI general error", null, new { ExitCode = exitCode, StdErr = stderr });
                throw new STTException("Fehler bei Transkription", exitCode, stderr);

            case 2:
                AppLogger.LogError("Whisper model not found", null, new { ExitCode = exitCode, StdErr = stderr });
                throw new ModelNotFoundException("Modell nicht gefunden", exitCode, stderr);

            case 3:
                AppLogger.LogError("Whisper audio device error", null, new { ExitCode = exitCode, StdErr = stderr });
                throw new AudioDeviceException("Audio-Gerät nicht verfügbar", exitCode, stderr);

            case 4:
                AppLogger.LogError("Whisper timeout", null, new { ExitCode = exitCode, StdErr = stderr });
                throw new STTTimeoutException("Transkription dauerte zu lange", exitCode, stderr);

            case 5:
                AppLogger.LogError("Whisper invalid audio format", null, new { ExitCode = exitCode, StdErr = stderr });
                throw new InvalidAudioException("Ungültige Audiodatei", exitCode, stderr);

            default:
                AppLogger.LogError("Whisper CLI unknown exit code", null, new { ExitCode = exitCode, StdErr = stderr });
                throw new STTException($"Unbekannter Fehler (Exit Code: {exitCode})", exitCode, stderr);
        }
    }

    /// <summary>
    /// Execute process asynchronously with timeout.
    /// </summary>
    /// <param name="fileName">Executable file name</param>
    /// <param name="arguments">Command-line arguments</param>
    /// <param name="timeout">Execution timeout</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (exit code, stdout, stderr)</returns>
    private async Task<(int ExitCode, string StdOut, string StdErr)> ExecuteProcessAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            }
        };

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
                stdoutBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
                stderrBuilder.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process with timeout
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Timeout or cancellation - kill process
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    AppLogger.LogWarning("Whisper CLI process killed due to timeout or cancellation");
                }

                throw new STTTimeoutException(
                    $"Transkription dauerte zu lange und wurde abgebrochen (Timeout: {timeout.TotalSeconds}s)",
                    timeout
                );
            }

            var stdout = stdoutBuilder.ToString();
            var stderr = stderrBuilder.ToString();

            return (process.ExitCode, stdout, stderr);
        }
        catch (Exception ex) when (ex is not STTException)
        {
            AppLogger.LogError("Failed to execute Whisper CLI process", ex);
            throw new STTException($"Failed to execute Whisper CLI: {ex.Message}", ex);
        }
    }
}
