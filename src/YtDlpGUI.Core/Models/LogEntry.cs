using YtDlpGUI.Core.Enums;

namespace YtDlpGUI.Core.Models;

public sealed record LogEntry(DateTimeOffset Timestamp, LogSeverity Severity, string Message);
