using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Core.Interfaces;

public interface IYtDlpService
{
    Task<DownloadRunResult> RunDownloadAsync(
        DownloadRequest request,
        AppSettings settings,
        Func<OutputParseResult, Task> onOutput,
        CancellationToken cancellationToken);
}
