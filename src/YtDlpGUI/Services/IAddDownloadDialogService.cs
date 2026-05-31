namespace YtDlpGUI.Services;

public interface IAddDownloadDialogService
{
    Task<bool> ShowAsync(XamlRoot xamlRoot, string? seedUrl = null, CancellationToken cancellationToken = default);
}
