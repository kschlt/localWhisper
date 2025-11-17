using Serilog;
using Serilog.Events;

namespace LocalWhisper.Core;

/// <summary>
/// Centralized logging wrapper using Serilog.
/// </summary>
/// <remarks>
/// Provides structured logging with automatic context enrichment.
/// Logs are written to {DataRoot}/logs/app.log
///
/// See: docs/specification/functional-requirements.md (FR-023)
/// See: docs/specification/non-functional-requirements.md (NFR-006)
/// </remarks>
public static class AppLogger
{
    private static ILogger? _logger;
    private static bool _isInitialized;

    /// <summary>
    /// Initialize the logger with the specified data root path.
    /// </summary>
    /// <param name="dataRoot">Root directory for application data</param>
    /// <param name="minimumLevel">Minimum log level (default: Information)</param>
    public static void Initialize(string dataRoot, LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        if (_isInitialized)
        {
            return;
        }

        var logPath = Path.Combine(dataRoot, "logs", "app.log");

        // Ensure logs directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        _logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .WriteTo.File(
                path: logPath,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Infinite,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 5
            )
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LocalWhisper")
            .Enrich.WithProperty("Version", "0.1.0")
            .CreateLogger();

        _isInitialized = true;

        LogInformation("AppLogger initialized", new { DataRoot = dataRoot, LogLevel = minimumLevel });
    }

    /// <summary>
    /// Log informational message with structured data.
    /// </summary>
    public static void LogInformation(string message, object? context = null)
    {
        EnsureInitialized();
        if (context != null)
        {
            _logger!.Information("{Message} {@Context}", message, context);
        }
        else
        {
            _logger!.Information("{Message}", message);
        }
    }

    /// <summary>
    /// Log warning message with structured data.
    /// </summary>
    public static void LogWarning(string message, object? context = null)
    {
        EnsureInitialized();
        if (context != null)
        {
            _logger!.Warning("{Message} {@Context}", message, context);
        }
        else
        {
            _logger!.Warning("{Message}", message);
        }
    }

    /// <summary>
    /// Log error message with exception and structured data.
    /// </summary>
    public static void LogError(string message, Exception? exception = null, object? context = null)
    {
        EnsureInitialized();
        if (context != null)
        {
            _logger!.Error(exception, "{Message} {@Context}", message, context);
        }
        else
        {
            _logger!.Error(exception, "{Message}", message);
        }
    }

    /// <summary>
    /// Log debug message (only if log level is Debug).
    /// </summary>
    public static void LogDebug(string message, object? context = null)
    {
        EnsureInitialized();
        if (context != null)
        {
            _logger!.Debug("{Message} {@Context}", message, context);
        }
        else
        {
            _logger!.Debug("{Message}", message);
        }
    }

    /// <summary>
    /// Flush logs and close the logger.
    /// Call this on application shutdown.
    /// </summary>
    public static void Shutdown()
    {
        if (_isInitialized)
        {
            LogInformation("Application shutting down");
            Log.CloseAndFlush();
            _isInitialized = false;
        }
    }

    private static void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("AppLogger must be initialized before use. Call AppLogger.Initialize() first.");
        }
    }
}
