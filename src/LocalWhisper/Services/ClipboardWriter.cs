using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using LocalWhisper.Core;
using LocalWhisper.Models;

namespace LocalWhisper.Services;

/// <summary>
/// Writes text to Windows clipboard with retry logic.
/// </summary>
/// <remarks>
/// Implements US-030: Clipboard Write
/// - Writes transcript to clipboard using WPF Clipboard API
/// - Retries if clipboard is locked by another application
/// - Runs on STA thread (required by WPF Clipboard API)
/// - Logs success/failure with timing
///
/// See: docs/iterations/iteration-04-clipboard-history-flyout.md (US-030)
/// See: docs/specification/functional-requirements.md (FR-013)
/// </remarks>
public class ClipboardWriter
{
    private const int DefaultMaxRetries = 1;
    private const int DefaultRetryDelayMs = 100;

    /// <summary>
    /// Write text to clipboard with retry logic.
    /// </summary>
    /// <param name="text">Text to write to clipboard</param>
    /// <param name="maxRetries">Maximum retry attempts (default 1)</param>
    /// <param name="retryDelayMs">Delay between retries in milliseconds (default 100)</param>
    /// <exception cref="ClipboardLockedException">If clipboard remains locked after retries</exception>
    public async Task WriteAsync(string text, int maxRetries = DefaultMaxRetries, int retryDelayMs = DefaultRetryDelayMs)
    {
        if (string.IsNullOrEmpty(text))
        {
            AppLogger.LogWarning("Attempted to write empty text to clipboard");
            return;
        }

        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            try
            {
                // WPF Clipboard API requires STA thread
                // Application.Current.Dispatcher is already on STA thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Clipboard.SetText(text);
                });

                AppLogger.LogInformation("Clipboard write succeeded", new
                {
                    TextLength = text.Length,
                    Attempt = attempt + 1
                });

                return; // Success
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x800401D0)) // CLIPBRD_E_CANT_OPEN
            {
                lastException = ex;
                attempt++;

                AppLogger.LogWarning($"Clipboard locked - retry attempt {attempt}/{maxRetries + 1}", new
                {
                    HResult = ex.HResult,
                    RetryDelay_Ms = retryDelayMs
                });

                if (attempt <= maxRetries)
                {
                    await Task.Delay(retryDelayMs);
                }
            }
            catch (Exception ex)
            {
                // Other unexpected exceptions
                AppLogger.LogError("Clipboard write failed with unexpected error", ex);
                throw;
            }
        }

        // All retries exhausted
        AppLogger.LogError("Clipboard write failed - locked after all retries", lastException, new
        {
            MaxRetries = maxRetries,
            TextLength = text.Length
        });

        throw new ClipboardLockedException(
            $"Zwischenablage ist gesperrt (Versuche: {maxRetries + 1})",
            maxRetries,
            lastException!
        );
    }
}
