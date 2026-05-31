using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;
using YtDlpGUI.Services;

namespace YtDlpGUI.ViewModels;

public sealed partial class QueueViewModel : ViewModelBase
{
    private readonly IQueueService _queueService;
    private readonly IUiDispatcher _uiDispatcher;

    public QueueViewModel(IQueueService queueService, IUiDispatcher uiDispatcher)
    {
        _queueService = queueService;
        _uiDispatcher = uiDispatcher;

        StartQueueCommand = new AsyncRelayCommand(StartQueueAsync);
        PauseQueueCommand = new AsyncRelayCommand(PauseQueueAsync);
        CancelJobCommand = new AsyncRelayCommand<Guid>(CancelJobAsync);
        RetryJobCommand = new AsyncRelayCommand<Guid>(RetryJobAsync);
        RemoveJobCommand = new AsyncRelayCommand<Guid>(RemoveJobAsync);
        MoveUpJobCommand = new AsyncRelayCommand<Guid>(MoveUpJobAsync);
        MoveDownJobCommand = new AsyncRelayCommand<Guid>(MoveDownJobAsync);
        ClearCompletedCommand = new AsyncRelayCommand(ClearCompletedAsync);

        foreach (var job in _queueService.GetJobs())
        {
            Jobs.Add(DownloadJobItemViewModel.From(job));
        }

        IsQueueRunning = _queueService.IsRunning;

        _queueService.JobAdded += HandleJobAdded;
        _queueService.JobUpdated += HandleJobUpdated;
        _queueService.JobRemoved += HandleJobRemoved;
    }

    public ObservableCollection<DownloadJobItemViewModel> Jobs { get; } = [];

    [ObservableProperty]
    private DownloadJobItemViewModel? selectedJob;

    [ObservableProperty]
    private bool isQueueRunning;

    public IAsyncRelayCommand StartQueueCommand { get; }

    public IAsyncRelayCommand PauseQueueCommand { get; }

    public IAsyncRelayCommand<Guid> CancelJobCommand { get; }

    public IAsyncRelayCommand<Guid> RetryJobCommand { get; }

    public IAsyncRelayCommand<Guid> RemoveJobCommand { get; }

    public IAsyncRelayCommand<Guid> MoveUpJobCommand { get; }

    public IAsyncRelayCommand<Guid> MoveDownJobCommand { get; }

    public IAsyncRelayCommand ClearCompletedCommand { get; }

    private async Task StartQueueAsync()
    {
        await _queueService.StartAsync().ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() => IsQueueRunning = true).ConfigureAwait(false);
    }

    private async Task PauseQueueAsync()
    {
        await _queueService.PauseAsync().ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() => IsQueueRunning = false).ConfigureAwait(false);
    }

    private Task CancelJobAsync(Guid jobId) => _queueService.CancelAsync(jobId);

    private Task RetryJobAsync(Guid jobId) => _queueService.RetryAsync(jobId);

    private Task RemoveJobAsync(Guid jobId) => _queueService.RemoveAsync(jobId);

    private async Task MoveUpJobAsync(Guid jobId)
    {
        var index = Jobs.ToList().FindIndex(x => x.JobId == jobId);
        if (index <= 0)
        {
            return;
        }

        await _queueService.MoveAsync(jobId, index - 1).ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() => Jobs.Move(index, index - 1)).ConfigureAwait(false);
    }

    private async Task MoveDownJobAsync(Guid jobId)
    {
        var index = Jobs.ToList().FindIndex(x => x.JobId == jobId);
        if (index < 0 || index >= Jobs.Count - 1)
        {
            return;
        }

        await _queueService.MoveAsync(jobId, index + 1).ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() => Jobs.Move(index, index + 1)).ConfigureAwait(false);
    }

    private Task ClearCompletedAsync() => _queueService.ClearCompletedAsync();

    private void HandleJobAdded(object? sender, DownloadJob job)
    {
        _ = _uiDispatcher.EnqueueAsync(() => Jobs.Add(DownloadJobItemViewModel.From(job)));
    }

    private void HandleJobUpdated(object? sender, DownloadJob job)
    {
        _ = _uiDispatcher.EnqueueAsync(() =>
        {
            var existing = Jobs.FirstOrDefault(x => x.JobId == job.Id);
            if (existing is null)
            {
                Jobs.Add(DownloadJobItemViewModel.From(job));
                return;
            }

            existing.UpdateFrom(job);
        });
    }

    private void HandleJobRemoved(object? sender, Guid jobId)
    {
        _ = _uiDispatcher.EnqueueAsync(() =>
        {
            var existing = Jobs.FirstOrDefault(x => x.JobId == jobId);
            if (existing is not null)
            {
                Jobs.Remove(existing);
            }
        });
    }
}
