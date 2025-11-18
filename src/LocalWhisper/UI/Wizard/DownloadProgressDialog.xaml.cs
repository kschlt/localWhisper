using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LocalWhisper.Core;
using LocalWhisper.Models;
using LocalWhisper.Services;

namespace LocalWhisper.UI.Wizard;

/// <summary>
/// Download progress dialog for model downloads.
/// </summary>
/// <remarks>
/// Implements US-041b: Wizard Step 2 - Model Download
/// - Shows progress bar (0-100%)
/// - Reports download speed (MB/s)
/// - Shows ETA
/// - Allows cancellation
///
/// See: docs/iterations/iteration-05b-download-repair.md (Task 2)
/// </remarks>
public partial class DownloadProgressDialog : Window
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ModelDownloader _downloader = new();

    public DownloadProgressDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Download model file with progress tracking.
    /// </summary>
    /// <param name="model">Model to download</param>
    /// <param name="destinationPath">Destination file path</param>
    /// <returns>Path to downloaded file, or null if cancelled/failed</returns>
    public async Task<string?> DownloadModelAsync(ModelDefinition model, string destinationPath)
    {
        TitleText.Text = $"Lädt {model.Name} herunter...";

        var progress = new Progress<DownloadProgress>(p =>
        {
            ProgressBar.Value = p.Percentage;
            DownloadedText.Text = $"{p.BytesDownloaded / 1024 / 1024} MB / {p.TotalBytes / 1024 / 1024} MB";
            SpeedText.Text = $"Geschwindigkeit: {p.BytesPerSecond / 1024 / 1024:F2} MB/s";
            ETAText.Text = $"Verbleibend: {p.EstimatedTimeRemaining:mm\\:ss}";
        });

        try
        {
            var filePath = await _downloader.DownloadAsync(model, destinationPath, progress, _cts.Token);
            DialogResult = true;
            return filePath;
        }
        catch (OperationCanceledException)
        {
            AppLogger.LogInformation("Download cancelled by user");
            DialogResult = false;
            return null;
        }
        catch (ModelDownloadException ex)
        {
            AppLogger.LogError("Download failed", ex);
            MessageBox.Show(
                $"Download fehlgeschlagen:\n\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            DialogResult = false;
            return null;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
    }
}
