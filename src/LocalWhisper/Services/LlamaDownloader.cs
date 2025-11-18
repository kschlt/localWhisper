using System.IO;
using System.Net.Http;
using LocalWhisper.Core;
using LocalWhisper.Models;

namespace LocalWhisper.Services;

/// <summary>
/// Downloads llama-cli.exe and Llama model files with retry logic.
/// </summary>
/// <remarks>
/// Iteration 7: US-064 (Wizard - Post-Processing Setup)
/// Downloads:
/// - llama-cli.exe from llama.cpp GitHub releases
/// - Llama 3.2 3B model from Hugging Face
///
/// See: docs/iterations/iteration-07-post-processing-DECISIONS.md
/// </remarks>
public class LlamaDownloader
{
    private readonly HttpClient _httpClient;
    private const int MaxRetries = 3;
    private const int InitialRetryDelayMs = 1000;

    // Download URLs (Iteration 7)
    public const string LlamaCLIWindowsURL = "https://github.com/ggerganov/llama.cpp/releases/latest/download/llama-cli-win-x64.exe";
    public const string LlamaModelURL = "https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf";

    public LlamaDownloader() : this(new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
    {
    }

    public LlamaDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;

        // Ensure httpClient has a reasonable timeout (models are ~2GB)
        if (_httpClient.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            _httpClient.Timeout = TimeSpan.FromMinutes(30);
        }
    }

    /// <summary>
    /// Download file with retry logic and progress tracking.
    /// </summary>
    /// <param name="url">Download URL</param>
    /// <param name="destinationPath">Destination file path</param>
    /// <param name="progress">Progress reporter (optional)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to downloaded file</returns>
    /// <exception cref="HttpRequestException">Thrown when download fails after all retries</exception>
    /// <exception cref="OperationCanceledException">Thrown when download is cancelled</exception>
    public async Task<string> DownloadAsync(
        string url,
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
                    URL = url,
                    Destination = destinationPath
                });

                await DownloadFileAsync(url, destinationPath, progress, ct);

                AppLogger.LogInformation("Download successful", new
                {
                    FilePath = destinationPath,
                    FileSize = new FileInfo(destinationPath).Length
                });

                return destinationPath;
            }
            catch (OperationCanceledException)
            {
                // Cancellation should not retry
                AppLogger.LogInformation("Download cancelled", new { URL = url });

                // Cleanup partial download
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

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

                    // Cleanup partial download before retry
                    if (File.Exists(destinationPath))
                    {
                        try
                        {
                            File.Delete(destinationPath);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }

                    await Task.Delay(delay, ct);
                }
            }
        }

        throw new HttpRequestException($"Download failed after {MaxRetries} attempts: {url}", lastException);
    }

    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        // Ensure destination directory exists
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

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

            // Report progress every 256KB
            if (progress != null && downloadedBytes % (256 * 1024) == 0)
            {
                var elapsed = DateTime.Now - startTime;
                var bytesPerSecond = elapsed.TotalSeconds > 0 ? downloadedBytes / elapsed.TotalSeconds : 0;
                var eta = bytesPerSecond > 0 && totalBytes > 0
                    ? TimeSpan.FromSeconds((totalBytes - downloadedBytes) / bytesPerSecond)
                    : TimeSpan.Zero;

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

        // Final progress report
        if (progress != null)
        {
            progress.Report(new DownloadProgress
            {
                BytesDownloaded = downloadedBytes,
                TotalBytes = totalBytes,
                Percentage = 100,
                BytesPerSecond = 0,
                EstimatedTimeRemaining = TimeSpan.Zero
            });
        }
    }
}
