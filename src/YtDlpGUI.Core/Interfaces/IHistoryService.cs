using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Core.Interfaces;

public interface IHistoryService
{
    Task<IReadOnlyList<HistoryEntry>> LoadAsync(CancellationToken cancellationToken = default);

    Task AppendAsync(HistoryEntry entry, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
