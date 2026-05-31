using YtDlpGUI.Core.Enums;

namespace YtDlpGUI.Core.Models;

public sealed record DownloadJob(
    Guid Id,
    DownloadRequest Request,
    QueueItemStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    int Attempt,
    DownloadProgressSnapshot Progress,
    string CurrentTitle,
    string OutputPath,
    string LastError,
    IReadOnlyList<LogEntry> Logs);
