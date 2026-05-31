using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Core.Interfaces;

public interface IQueueService
{
    event EventHandler<DownloadJob>? JobAdded;

    event EventHandler<DownloadJob>? JobUpdated;

    event EventHandler<Guid>? JobRemoved;

    IReadOnlyList<DownloadJob> GetJobs();

    bool IsRunning { get; }

    Task<Guid> EnqueueAsync(DownloadRequest request, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid jobId);

    Task RetryAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task MoveAsync(Guid jobId, int targetIndex);

    Task RemoveAsync(Guid jobId);

    Task ClearCompletedAsync();

    Task StartAsync(CancellationToken cancellationToken = default);

    Task PauseAsync();
}
