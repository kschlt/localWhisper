using System;

namespace LocalWhisper.Services;

/// <summary>
/// Progress information for model download.
/// </summary>
/// <remarks>
/// Used for reporting download progress in DownloadProgressDialog.
/// See: docs/iterations/iteration-05b-download-repair.md (Task 2)
/// </remarks>
public class DownloadProgress
{
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public int Percentage { get; set; }
    public double BytesPerSecond { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
}
