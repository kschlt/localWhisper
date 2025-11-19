using FluentAssertions;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Security.Cryptography;
using Xunit;

namespace LocalWhisper.Tests.Unit;

/// <summary>
/// Unit tests for ModelDownloader class.
/// </summary>
/// <remarks>
/// Tests cover US-041b: Wizard Step 2 - Model Download
/// Scenario: Download retry on failure (@Integration @CanRunInClaudeCode)
/// Scenario: SHA-1 verification after download (@Contract @CanRunInClaudeCode)
///
/// Tests verify:
/// - HTTP download succeeds on first attempt
/// - Retry logic (up to 3 attempts with exponential backoff)
/// - Cancellation via CancellationToken
/// - Progress reporting (bytes, percentage, speed, ETA)
/// - SHA-1 validation after download
///
/// See: docs/iterations/iteration-05b-download-repair.md (Task 1)
/// See: docs/specification/user-stories-gherkin.md (US-041b, lines 802-820)
/// </remarks>
[Trait("Batch", "4")]
public class ModelDownloaderTests : IDisposable
{
    private readonly string _testDirectory;

    public ModelDownloaderTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LocalWhisperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Initialize with Error level to reduce test output verbosity
        AppLogger.Initialize(_testDirectory, Serilog.Events.LogEventLevel.Error);
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
    public async Task DownloadAsync_SuccessfulDownload_ReturnsFilePath()
    {
        // Arrange
        var testContent = "Test model content";
        var testContentBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
        var expectedHash = ComputeSHA1(testContentBytes);

        var httpMessageHandler = CreateMockHttpHandler(HttpStatusCode.OK, testContentBytes);
        var httpClient = new HttpClient(httpMessageHandler.Object);
        var downloader = new ModelDownloader(httpClient);

        var model = new ModelDefinition
        {
            Name = "small",
            FileName = "ggml-small.bin",
            SHA1 = expectedHash,
            DownloadURL = "https://example.com/ggml-small.bin"
        };

        var destinationPath = Path.Combine(_testDirectory, "ggml-small.bin");
        var progress = new Progress<DownloadProgress>();

        // Act
        var result = await downloader.DownloadAsync(model, destinationPath, progress, CancellationToken.None);

        // Assert
        result.Should().Be(destinationPath, "download should return destination path");
        File.Exists(destinationPath).Should().BeTrue("file should be created");
        File.ReadAllText(destinationPath).Should().Be(testContent, "file content should match");
    }

    [Fact]
    public async Task DownloadAsync_NetworkError_RetriesThreeTimes()
    {
        // Arrange - Fail twice, succeed on 3rd attempt
        var testContent = "Test model content";
        var testContentBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
        var expectedHash = ComputeSHA1(testContentBytes);

        var callCount = 0;
        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    // Fail first 2 attempts
                    throw new HttpRequestException("Network error");
                }
                // Succeed on 3rd attempt
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(testContentBytes)
                };
            });

        var httpClient = new HttpClient(httpMessageHandler.Object);
        var downloader = new ModelDownloader(httpClient);

        var model = new ModelDefinition
        {
            Name = "small",
            FileName = "ggml-small.bin",
            SHA1 = expectedHash,
            DownloadURL = "https://example.com/ggml-small.bin"
        };

        var destinationPath = Path.Combine(_testDirectory, "ggml-small.bin");
        var progress = new Progress<DownloadProgress>();

        // Act
        var result = await downloader.DownloadAsync(model, destinationPath, progress, CancellationToken.None);

        // Assert
        callCount.Should().Be(3, "should retry twice and succeed on 3rd attempt");
        File.Exists(destinationPath).Should().BeTrue("file should be created after retries");
    }

    [Fact]
    public async Task DownloadAsync_ExponentialBackoff_UsesCorrectDelays()
    {
        // Arrange - Track retry timestamps to verify exponential backoff (1s, 2s, 4s)
        var retryTimestamps = new List<DateTime>();
        var testContent = "Test model content";
        var testContentBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
        var expectedHash = ComputeSHA1(testContentBytes);

        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                retryTimestamps.Add(DateTime.Now);

                if (retryTimestamps.Count < 3)
                {
                    // Fail first 2 attempts
                    throw new HttpRequestException("Network error");
                }

                // Succeed on 3rd attempt
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(testContentBytes)
                };
            });

        var httpClient = new HttpClient(httpMessageHandler.Object);
        var downloader = new ModelDownloader(httpClient);

        var model = new ModelDefinition
        {
            Name = "small",
            FileName = "ggml-small.bin",
            SHA1 = expectedHash,
            DownloadURL = "https://example.com/ggml-small.bin"
        };

        var destinationPath = Path.Combine(_testDirectory, "ggml-small.bin");
        var progress = new Progress<DownloadProgress>();

        // Act
        await downloader.DownloadAsync(model, destinationPath, progress, CancellationToken.None);

        // Assert - Verify exponential backoff timing (1s, 2s)
        retryTimestamps.Should().HaveCount(3, "should have 3 attempts total");

        // Delay between attempt 1 and 2 should be ~1000ms
        var delay1 = (retryTimestamps[1] - retryTimestamps[0]).TotalMilliseconds;
        delay1.Should().BeInRange(900, 1200, "first retry delay should be ~1000ms (1s)");

        // Delay between attempt 2 and 3 should be ~2000ms
        var delay2 = (retryTimestamps[2] - retryTimestamps[1]).TotalMilliseconds;
        delay2.Should().BeInRange(1800, 2400, "second retry delay should be ~2000ms (2s)");
    }

    [Fact]
    public async Task DownloadAsync_AllRetriesFail_ThrowsModelDownloadException()
    {
        // Arrange - Fail all 3 attempts
        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(httpMessageHandler.Object);
        var downloader = new ModelDownloader(httpClient);

        var model = new ModelDefinition
        {
            Name = "small",
            FileName = "ggml-small.bin",
            SHA1 = "dummy",
            DownloadURL = "https://example.com/ggml-small.bin"
        };

        var destinationPath = Path.Combine(_testDirectory, "ggml-small.bin");
        var progress = new Progress<DownloadProgress>();

        // Act
        Func<Task> act = async () => await downloader.DownloadAsync(model, destinationPath, progress, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ModelDownloadException>(
            "should throw after all retries fail");
    }

    [Fact]
    public async Task DownloadAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(new byte[1000000]) // Large content to allow cancellation
            });

        var httpClient = new HttpClient(httpMessageHandler.Object);
        var downloader = new ModelDownloader(httpClient);

        var model = new ModelDefinition
        {
            Name = "small",
            FileName = "ggml-small.bin",
            SHA1 = "dummy",
            DownloadURL = "https://example.com/ggml-small.bin"
        };

        var destinationPath = Path.Combine(_testDirectory, "ggml-small.bin");
        var progress = new Progress<DownloadProgress>();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await downloader.DownloadAsync(model, destinationPath, progress, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>(
            "download should respect cancellation token");
    }

    [Fact]
    public async Task DownloadAsync_HashMismatch_ThrowsModelDownloadException()
    {
        // Arrange
        var testContent = "Test model content";
        var testContentBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
        var wrongHash = "0000000000000000000000000000000000000000";

        var httpMessageHandler = CreateMockHttpHandler(HttpStatusCode.OK, testContentBytes);
        var httpClient = new HttpClient(httpMessageHandler.Object);
        var downloader = new ModelDownloader(httpClient);

        var model = new ModelDefinition
        {
            Name = "small",
            FileName = "ggml-small.bin",
            SHA1 = wrongHash, // Wrong hash!
            DownloadURL = "https://example.com/ggml-small.bin"
        };

        var destinationPath = Path.Combine(_testDirectory, "ggml-small.bin");
        var progress = new Progress<DownloadProgress>();

        // Act
        Func<Task> act = async () => await downloader.DownloadAsync(model, destinationPath, progress, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ModelDownloadException>(
            "download should fail when SHA-1 hash doesn't match")
            .WithMessage("*SHA-1 hash mismatch*");
    }

    [Fact]
    public async Task DownloadAsync_ReportsProgress()
    {
        // Arrange
        var testContent = new byte[10000]; // 10 KB
        Array.Fill(testContent, (byte)0x42);
        var expectedHash = ComputeSHA1(testContent);

        var httpMessageHandler = CreateMockHttpHandler(HttpStatusCode.OK, testContent);
        var httpClient = new HttpClient(httpMessageHandler.Object);
        var downloader = new ModelDownloader(httpClient);

        var model = new ModelDefinition
        {
            Name = "small",
            FileName = "ggml-small.bin",
            SHA1 = expectedHash,
            DownloadURL = "https://example.com/ggml-small.bin"
        };

        var destinationPath = Path.Combine(_testDirectory, "ggml-small.bin");

        var progressReports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => progressReports.Add(p));

        // Act
        await downloader.DownloadAsync(model, destinationPath, progress, CancellationToken.None);

        // Assert
        progressReports.Should().NotBeEmpty("progress should be reported during download");
        progressReports.Last().Percentage.Should().Be(100, "final progress should be 100%");
        progressReports.Last().BytesDownloaded.Should().Be(testContent.Length, "final bytes should match content length");
    }

    // Helper methods

    private Mock<HttpMessageHandler> CreateMockHttpHandler(HttpStatusCode statusCode, byte[] content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new ByteArrayContent(content)
            });

        return handler;
    }

    private string ComputeSHA1(byte[] data)
    {
        using var sha1 = SHA1.Create();
        var hashBytes = sha1.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
