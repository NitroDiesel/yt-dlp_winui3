namespace YtDlpGUI.Core.Models;

public sealed record OutputParseResult(
    OutputLineType Type,
    string RawLine,
    DownloadProgressSnapshot? Progress = null,
    string? Message = null,
    string? ItemId = null,
    string? Title = null,
    string? OutputPath = null,
    string? Url = null);
