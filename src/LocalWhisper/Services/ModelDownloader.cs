using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LocalWhisper.Core;
using LocalWhisper.Models;

namespace LocalWhisper.Services;

/// <summary>
/// Downloads Whisper model files with retry logic and progress tracking.
/// </summary>
/// <remarks>
/// Implements US-041b: Wizard Step 2 - Model Download
/// - Downloads from HuggingFace or configured URL
/// - Retries up to 3 times with exponential backoff
/// - Reports progress (bytes, percentage, ETA)
/// - Validates SHA-1 hash after download
/// - Supports cancellation
///
/// See: docs/iterations/iteration-05b-download-repair.md (Task 1)
/// </remarks>
public class ModelDownloader
{
    private readonly HttpClient _httpClient;
    private readonly ModelValidator _validator;
    private const int MaxRetries = 3;
    private const int InitialRetryDelayMs = 1000;

    public ModelDownloader() : this(new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
    {
    }

    public ModelDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _validator = new ModelValidator();

        // Ensure httpClient has a reasonable timeout
        if (_httpClient.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }
    }

    /// <summary>
    /// Download model file with retry logic and SHA-1 validation.
    /// </summary>
    /// <param name="model">Model definition with download URL and expected hash</param>
    /// <param name="destinationPath">Destination file path</param>
    /// <param name="progress">Progress reporter (optional)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to downloaded file</returns>
    /// <exception cref="ModelDownloadException">Thrown when download fails after all retries</exception>
    /// <exception cref="OperationCanceledException">Thrown when download is cancelled</exception>
    public async Task<string> DownloadAsync(
        ModelDefinition model,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < MaxRetries)
        {
            try
            {
                AppLogger.LogInformation($"Download attempt {attempt + 1}/{MaxRetries}", new
                {
                    Model = model.Name,
                    URL = model.DownloadURL
                });

                await DownloadFileAsync(model.DownloadURL, destinationPath, progress, ct);

                // Validate SHA-1
                AppLogger.LogInformation("Validating downloaded model", new { Model = model.Name });
                var isValid = await _validator.ValidateAsync(destinationPath, model.SHA1);

                if (!isValid)
                {
                    // Delete invalid file
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }

                    throw new ModelDownloadException("SHA-1 hash mismatch after download");
                }

                AppLogger.LogInformation("Download successful", new
                {
                    Model = model.Name,
                    FilePath = destinationPath
                });

                return destinationPath;
            }
            catch (OperationCanceledException)
            {
                // Cancellation should not retry
                AppLogger.LogInformation("Download cancelled", new { Model = model.Name });

                // Cleanup partial download
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                throw;
            }
            catch (ModelDownloadException)
            {
                // Hash mismatch or model-specific errors should not retry
                // (re-downloading won't fix a bad file on the server)
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (attempt < MaxRetries)
                {
                    var delay = InitialRetryDelayMs * (int)Math.Pow(2, attempt - 1);
                    AppLogger.LogWarning($"Download failed - retrying in {delay}ms", new
                    {
                        Attempt = attempt,
                        Error = ex.Message
                    });

                    await Task.Delay(delay, ct);
                }
            }
        }

        throw new ModelDownloadException($"Download failed after {MaxRetries} attempts", lastException);
    }

    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;
        var startTime = DateTime.Now;

        using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
            downloadedBytes += bytesRead;

            // Report progress
            if (progress != null)
            {
                var elapsed = DateTime.Now - startTime;
                var bytesPerSecond = elapsed.TotalSeconds > 0 ? downloadedBytes / elapsed.TotalSeconds : 0;
                var eta = bytesPerSecond > 0 ? TimeSpan.FromSeconds((totalBytes - downloadedBytes) / bytesPerSecond) : TimeSpan.Zero;

                progress.Report(new DownloadProgress
                {
                    BytesDownloaded = downloadedBytes,
                    TotalBytes = totalBytes,
                    Percentage = totalBytes > 0 ? (int)((downloadedBytes * 100) / totalBytes) : 0,
                    BytesPerSecond = bytesPerSecond,
                    EstimatedTimeRemaining = eta
                });
            }
        }
    }
}
