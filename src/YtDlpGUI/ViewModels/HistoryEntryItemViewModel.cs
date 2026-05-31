using CommunityToolkit.Mvvm.ComponentModel;
using YtDlpGUI.Core.Models;

namespace YtDlpGUI.ViewModels;

public sealed partial class HistoryEntryItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid jobId;

    [ObservableProperty]
    private DateTimeOffset completedAtUtc;

    [ObservableProperty]
    private string completedAtText = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string outputPath = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private int attempts;

    [ObservableProperty]
    private string commandPreview = string.Empty;

    public static HistoryEntryItemViewModel From(HistoryEntry entry)
        => new()
        {
            JobId = entry.JobId,
            CompletedAtUtc = entry.CompletedAtUtc,
            CompletedAtText = entry.CompletedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            Status = entry.FinalStatus.ToString(),
            Url = entry.Url,
            Title = entry.Title,
            OutputPath = entry.OutputPath,
            ErrorMessage = entry.ErrorMessage,
            Attempts = entry.Attempts,
            CommandPreview = entry.CommandPreview,
        };
}
