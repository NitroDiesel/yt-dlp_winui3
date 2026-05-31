namespace YtDlpGUI.Core.Models;

public sealed record DownloadProgressSnapshot
{
    public string Status { get; init; } = string.Empty;

    public double? Percent { get; init; }

    public long? DownloadedBytes { get; init; }

    public long? TotalBytes { get; init; }

    public long? TotalBytesEstimate { get; init; }

    public double? SpeedBytesPerSecond { get; init; }

    public string SpeedText { get; init; } = string.Empty;

    public TimeSpan? Eta { get; init; }

    public string EtaText { get; init; } = string.Empty;

    public string CurrentFile { get; init; } = string.Empty;

    public string CurrentTitle { get; init; } = string.Empty;

    public static DownloadProgressSnapshot Empty { get; } = new();
}
