using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Core.Interfaces;

public interface IProcessService
{
    Task<int> RunAsync(
        ProcessRunRequest request,
        Func<string, Task> onStandardOutput,
        Func<string, Task> onStandardError,
        CancellationToken cancellationToken);
}
