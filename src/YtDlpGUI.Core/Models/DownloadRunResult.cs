namespace YtDlpGUI.Core.Models;

public sealed record DownloadRunResult(
    int ExitCode,
    bool IsCanceled,
    string? ErrorMessage,
    string? OutputPath,
    IReadOnlyList<LogEntry> Logs)
{
    public bool IsSuccess => ExitCode == 0 && !IsCanceled;
}
