using YtDlpGUI.Core.Enums;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Infrastructure.Services;

public sealed class QueueService(
    IYtDlpService ytDlpService,
    ISettingsService settingsService,
    IHistoryService historyService,
    IYtDlpCommandBuilder commandBuilder) : IQueueService, IDisposable
{
    private readonly object _sync = new();
    private readonly List<DownloadJob> _jobs = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _activeTokens = [];
    private readonly SemaphoreSlim _pendingSignal = new(0);
    private readonly CancellationTokenSource _workerCts = new();
    private Task? _workerTask;

    public event EventHandler<DownloadJob>? JobAdded;

    public event EventHandler<DownloadJob>? JobUpdated;

    public event EventHandler<Guid>? JobRemoved;

    public bool IsRunning { get; private set; }

    public IReadOnlyList<DownloadJob> GetJobs()
    {
        lock (_sync)
        {
            return _jobs.ToList();
        }
    }

    public async Task<Guid> EnqueueAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        var job = CreateJob(request, attempt: 1);

        lock (_sync)
        {
            _jobs.Add(job);
        }

        JobAdded?.Invoke(this, job);

        if (settings.AutoStartQueue)
        {
            await StartAsync(cancellationToken).ConfigureAwait(false);
        }

        _pendingSignal.Release();
        return job.Id;
    }

    public Task CancelAsync(Guid jobId)
    {
        DownloadJob? updated = null;
        CancellationTokenSource? token = null;

        lock (_sync)
        {
            var index = _jobs.FindIndex(x => x.Id == jobId);
            if (index < 0)
            {
                return Task.CompletedTask;
            }

            var current = _jobs[index];
            if (current.Status == QueueItemStatus.Pending)
            {
                updated = current with
                {
                    Status = QueueItemStatus.Canceled,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    LastError = "Canceled",
                    Logs = AppendLog(current.Logs, LogSeverity.Warning, "Download canceled before start."),
                };
                _jobs[index] = updated;
            }
            else if (current.Status == QueueItemStatus.Running && _activeTokens.TryGetValue(jobId, out var cts))
            {
                token = cts;
            }
        }

        if (updated is not null)
        {
            JobUpdated?.Invoke(this, updated);
        }

        token?.Cancel();
        return Task.CompletedTask;
    }

    public async Task RetryAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        DownloadJob? original;
        lock (_sync)
        {
            original = _jobs.FirstOrDefault(x => x.Id == jobId);
        }

        if (original is null || original.Status == QueueItemStatus.Running)
        {
            return;
        }

        var retryJob = CreateJob(original.Request, original.Attempt + 1);
        lock (_sync)
        {
            _jobs.Add(retryJob);
        }

        JobAdded?.Invoke(this, retryJob);

        if (!IsRunning)
        {
            await StartAsync(cancellationToken).ConfigureAwait(false);
        }

        _pendingSignal.Release();
    }

    public Task MoveAsync(Guid jobId, int targetIndex)
    {
        DownloadJob? moved = null;

        lock (_sync)
        {
            var currentIndex = _jobs.FindIndex(x => x.Id == jobId);
            if (currentIndex < 0)
            {
                return Task.CompletedTask;
            }

            targetIndex = Math.Clamp(targetIndex, 0, _jobs.Count - 1);
            if (currentIndex == targetIndex)
            {
                return Task.CompletedTask;
            }

            moved = _jobs[currentIndex];
            _jobs.RemoveAt(currentIndex);
            _jobs.Insert(targetIndex, moved);
        }

        if (moved is not null)
        {
            JobUpdated?.Invoke(this, moved);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid jobId)
    {
        var removed = false;

        lock (_sync)
        {
            var index = _jobs.FindIndex(x => x.Id == jobId);
            if (index < 0)
            {
                return Task.CompletedTask;
            }

            if (_jobs[index].Status == QueueItemStatus.Running)
            {
                return Task.CompletedTask;
            }

            _jobs.RemoveAt(index);
            removed = true;
        }

        if (removed)
        {
            JobRemoved?.Invoke(this, jobId);
        }

        return Task.CompletedTask;
    }

    public Task ClearCompletedAsync()
    {
        List<Guid> removedIds;
        lock (_sync)
        {
            removedIds = _jobs
                .Where(x => x.Status is QueueItemStatus.Completed or QueueItemStatus.Failed or QueueItemStatus.Canceled)
                .Select(x => x.Id)
                .ToList();
            _jobs.RemoveAll(x => removedIds.Contains(x.Id));
        }

        foreach (var id in removedIds)
        {
            JobRemoved?.Invoke(this, id);
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        EnsureWorker();
        IsRunning = true;

        // Nudge the worker if there are pending items.
        _pendingSignal.Release();
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    private void EnsureWorker()
    {
        if (_workerTask is not null)
        {
            return;
        }

        _workerTask = Task.Run(() => WorkerLoopAsync(_workerCts.Token));
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _pendingSignal.WaitAsync(cancellationToken).ConfigureAwait(false);

            while (!IsRunning)
            {
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }

            var next = GetNextPending();
            if (next is null)
            {
                continue;
            }

            await ExecuteJobAsync(next.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        AppSettings settings;
        try
        {
            settings = await settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            settings = new AppSettings();
        }

        DownloadJob? snapshot = null;

        lock (_sync)
        {
            var index = _jobs.FindIndex(x => x.Id == jobId);
            if (index < 0)
            {
                return;
            }

            var current = _jobs[index];
            var running = current with
            {
                Status = QueueItemStatus.Running,
                StartedAtUtc = DateTimeOffset.UtcNow,
                LastError = string.Empty,
                Logs = AppendLog(current.Logs, LogSeverity.Info, $"Starting attempt {current.Attempt}."),
            };
            _jobs[index] = running;
            snapshot = running;

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeTokens[jobId] = linkedCts;
        }

        if (snapshot is not null)
        {
            JobUpdated?.Invoke(this, snapshot);
        }

        var jobCts = _activeTokens[jobId];
        var result = await ytDlpService.RunDownloadAsync(
            snapshot!.Request,
            settings,
            output =>
            {
                DownloadJob? updated = null;
                lock (_sync)
                {
                    var index = _jobs.FindIndex(x => x.Id == jobId);
                    if (index < 0)
                    {
                        return Task.CompletedTask;
                    }

                    var current = _jobs[index];
                    var progress = output.Progress ?? current.Progress;
                    var title = current.CurrentTitle;
                    var outputPath = current.OutputPath;
                    var lastError = current.LastError;
                    var logs = current.Logs;

                    if (output.Type == OutputLineType.InfoBefore && !string.IsNullOrWhiteSpace(output.Title))
                    {
                        title = output.Title;
                    }

                    if ((output.Type == OutputLineType.InfoAfter || output.Type == OutputLineType.Destination)
                        && !string.IsNullOrWhiteSpace(output.OutputPath))
                    {
                        outputPath = output.OutputPath;
                    }

                    if (output.Type == OutputLineType.Error)
                    {
                        lastError = output.Message ?? output.RawLine;
                    }

                    if (output.Type != OutputLineType.Progress && !string.IsNullOrWhiteSpace(output.RawLine))
                    {
                        var severity = output.Type switch
                        {
                            OutputLineType.Warning => LogSeverity.Warning,
                            OutputLineType.Error => LogSeverity.Error,
                            _ => LogSeverity.Info,
                        };
                        logs = AppendLog(logs, severity, output.RawLine);
                    }

                    updated = current with
                    {
                        Progress = progress,
                        CurrentTitle = title,
                        OutputPath = outputPath,
                        LastError = lastError,
                        Logs = logs,
                    };
                    _jobs[index] = updated;
                }

                if (updated is not null)
                {
                    JobUpdated?.Invoke(this, updated);
                }

                return Task.CompletedTask;
            },
            jobCts.Token).ConfigureAwait(false);

        DownloadJob? finalized = null;

        lock (_sync)
        {
            _activeTokens.Remove(jobId);

            var index = _jobs.FindIndex(x => x.Id == jobId);
            if (index < 0)
            {
                return;
            }

            var current = _jobs[index];
            var status = result.IsCanceled
                ? QueueItemStatus.Canceled
                : result.IsSuccess
                    ? QueueItemStatus.Completed
                    : QueueItemStatus.Failed;

            var error = status == QueueItemStatus.Failed
                ? result.ErrorMessage ?? current.LastError
                : current.LastError;

            finalized = current with
            {
                Status = status,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                OutputPath = string.IsNullOrWhiteSpace(result.OutputPath) ? current.OutputPath : result.OutputPath,
                LastError = error ?? string.Empty,
                Logs = result.IsSuccess
                    ? AppendLog(current.Logs, LogSeverity.Info, "Download completed successfully.")
                    : AppendLog(current.Logs, status == QueueItemStatus.Canceled ? LogSeverity.Warning : LogSeverity.Error, result.ErrorMessage ?? "Download failed."),
            };

            _jobs[index] = finalized;
        }

        if (finalized is not null)
        {
            JobUpdated?.Invoke(this, finalized);
            await SaveHistoryAsync(finalized, settings, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SaveHistoryAsync(DownloadJob job, AppSettings settings, CancellationToken cancellationToken)
    {
        var duration = job.StartedAtUtc.HasValue && job.FinishedAtUtc.HasValue
            ? (job.FinishedAtUtc.Value - job.StartedAtUtc.Value).TotalSeconds
            : (double?)null;

        var entry = new HistoryEntry(
            job.Id,
            job.FinishedAtUtc ?? DateTimeOffset.UtcNow,
            job.Status,
            job.Request.Url,
            string.IsNullOrWhiteSpace(job.CurrentTitle) ? "(unknown title)" : job.CurrentTitle,
            job.OutputPath,
            job.LastError,
            job.Attempt,
            commandBuilder.BuildCommandPreview(job.Request, settings),
            job.Progress.TotalBytes,
            duration);

        await historyService.AppendAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private Guid? GetNextPending()
    {
        lock (_sync)
        {
            return _jobs.FirstOrDefault(x => x.Status == QueueItemStatus.Pending)?.Id;
        }
    }

    private static DownloadJob CreateJob(DownloadRequest request, int attempt)
        => new(
            Guid.NewGuid(),
            request,
            QueueItemStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            attempt,
            DownloadProgressSnapshot.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            []);

    private static IReadOnlyList<LogEntry> AppendLog(IReadOnlyList<LogEntry> logs, LogSeverity severity, string message)
    {
        var copy = logs.ToList();
        copy.Add(new LogEntry(DateTimeOffset.UtcNow, severity, message));
        if (copy.Count > 500)
        {
            copy = copy[^500..];
        }

        return copy;
    }

    public void Dispose()
    {
        _workerCts.Cancel();
        foreach (var cts in _activeTokens.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _pendingSignal.Dispose();
        _workerCts.Dispose();
    }
}
