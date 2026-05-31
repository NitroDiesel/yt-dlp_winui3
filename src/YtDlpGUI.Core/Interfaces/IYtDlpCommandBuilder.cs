using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Core.Interfaces;

public interface IYtDlpCommandBuilder
{
    ProcessRunRequest BuildProcessRequest(DownloadRequest request, AppSettings settings);

    string BuildCommandPreview(DownloadRequest request, AppSettings settings);
}
