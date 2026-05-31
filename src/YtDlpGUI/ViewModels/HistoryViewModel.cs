using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;
using YtDlpGUI.Services;

namespace YtDlpGUI.ViewModels;

public sealed partial class HistoryViewModel : ViewModelBase
{
    private readonly IHistoryService _historyService;
    private readonly IQueueService _queueService;
    private readonly ISettingsService _settingsService;
    private readonly ILauncherService _launcherService;
    private readonly IClipboardService _clipboardService;
    private readonly IUiDispatcher _uiDispatcher;

    public HistoryViewModel(
        IHistoryService historyService,
        IQueueService queueService,
        ISettingsService settingsService,
        ILauncherService launcherService,
        IClipboardService clipboardService,
        IUiDispatcher uiDispatcher)
    {
        _historyService = historyService;
        _queueService = queueService;
        _settingsService = settingsService;
        _launcherService = launcherService;
        _clipboardService = clipboardService;
        _uiDispatcher = uiDispatcher;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ClearHistoryCommand = new AsyncRelayCommand(ClearHistoryAsync);
        RequeueCommand = new AsyncRelayCommand<HistoryEntryItemViewModel>(RequeueAsync);
        OpenOutputCommand = new AsyncRelayCommand<HistoryEntryItemViewModel>(OpenOutputAsync);
        OpenFolderCommand = new AsyncRelayCommand<HistoryEntryItemViewModel>(OpenFolderAsync);
        CopyCommandPreviewCommand = new AsyncRelayCommand<HistoryEntryItemViewModel>(CopyCommandPreviewAsync);

        _ = RefreshAsync();
    }

    public ObservableCollection<HistoryEntryItemViewModel> Entries { get; } = [];

    [ObservableProperty]
    private HistoryEntryItemViewModel? selectedEntry;

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand ClearHistoryCommand { get; }

    public IAsyncRelayCommand<HistoryEntryItemViewModel> RequeueCommand { get; }

    public IAsyncRelayCommand<HistoryEntryItemViewModel> OpenOutputCommand { get; }

    public IAsyncRelayCommand<HistoryEntryItemViewModel> OpenFolderCommand { get; }

    public IAsyncRelayCommand<HistoryEntryItemViewModel> CopyCommandPreviewCommand { get; }

    private async Task RefreshAsync()
    {
        var entries = await _historyService.LoadAsync().ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() =>
        {
            Entries.Clear();
            foreach (var entry in entries)
            {
                Entries.Add(HistoryEntryItemViewModel.From(entry));
            }

            StatusMessage = Entries.Count == 0 ? "No completed downloads yet." : string.Empty;
        }).ConfigureAwait(false);
    }

    private async Task ClearHistoryAsync()
    {
        await _historyService.ClearAsync().ConfigureAwait(false);
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task RequeueAsync(HistoryEntryItemViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        var settings = await _settingsService.LoadAsync().ConfigureAwait(false);
        var request = DownloadRequest.CreateDefault(settings) with { Url = entry.Url };
        await _queueService.EnqueueAsync(request).ConfigureAwait(false);
        await _queueService.StartAsync().ConfigureAwait(false);

        await _uiDispatcher.EnqueueAsync(() => StatusMessage = "History item requeued.").ConfigureAwait(false);
    }

    private Task OpenOutputAsync(HistoryEntryItemViewModel? entry)
        => entry is null ? Task.CompletedTask : _launcherService.OpenPathAsync(entry.OutputPath);

    private Task OpenFolderAsync(HistoryEntryItemViewModel? entry)
        => entry is null ? Task.CompletedTask : _launcherService.OpenContainingFolderAsync(entry.OutputPath);

    private async Task CopyCommandPreviewAsync(HistoryEntryItemViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        await _clipboardService.SetTextAsync(entry.CommandPreview).ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() => StatusMessage = "Command copied to clipboard.").ConfigureAwait(false);
    }
}
