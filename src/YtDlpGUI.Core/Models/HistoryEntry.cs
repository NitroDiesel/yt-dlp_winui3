using YtDlpGUI.Core.Enums;

namespace YtDlpGUI.Core.Models;

public sealed record HistoryEntry(
    Guid JobId,
    DateTimeOffset CompletedAtUtc,
    QueueItemStatus FinalStatus,
    string Url,
    string Title,
    string OutputPath,
    string ErrorMessage,
    int Attempts,
    string CommandPreview,
    long? TotalBytes,
    double? DurationSeconds);
