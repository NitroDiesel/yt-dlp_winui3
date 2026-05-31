namespace YtDlpGUI.Services;

public interface ILauncherService
{
    Task OpenPathAsync(string path);

    Task OpenContainingFolderAsync(string path);
}
