using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YtDlpGUI.Core.Enums;
using YtDlpGUI.Core.Models;

namespace YtDlpGUI.ViewModels;

public sealed partial class DownloadJobItemViewModel : ObservableObject
{
    public Guid JobId { get; private set; }

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private QueueItemStatus status;

    [ObservableProperty]
    private int attempt;

    [ObservableProperty]
    private double progressPercent;

    [ObservableProperty]
    private string speed = string.Empty;

    [ObservableProperty]
    private string eta = string.Empty;

    [ObservableProperty]
    private string outputPath = string.Empty;

    [ObservableProperty]
    private string lastError = string.Empty;

    [ObservableProperty]
    private DateTimeOffset createdAtUtc;

    [ObservableProperty]
    private DateTimeOffset? startedAtUtc;

    [ObservableProperty]
    private DateTimeOffset? finishedAtUtc;

    public ObservableCollection<LogEntry> Logs { get; } = [];

    public void UpdateFrom(DownloadJob job)
    {
        JobId = job.Id;
        Url = job.Request.Url;
        Title = string.IsNullOrWhiteSpace(job.CurrentTitle) ? "(pending metadata)" : job.CurrentTitle;
        Status = job.Status;
        Attempt = job.Attempt;
        ProgressPercent = Math.Clamp(job.Progress.Percent ?? 0d, 0d, 100d);
        Speed = job.Progress.SpeedText;
        Eta = job.Progress.EtaText;
        OutputPath = job.OutputPath;
        LastError = job.LastError;
        CreatedAtUtc = job.CreatedAtUtc;
        StartedAtUtc = job.StartedAtUtc;
        FinishedAtUtc = job.FinishedAtUtc;

        Logs.Clear();
        foreach (var log in job.Logs)
        {
            Logs.Add(log);
        }
    }

    public static DownloadJobItemViewModel From(DownloadJob job)
    {
        var vm = new DownloadJobItemViewModel();
        vm.UpdateFrom(job);
        return vm;
    }
}
