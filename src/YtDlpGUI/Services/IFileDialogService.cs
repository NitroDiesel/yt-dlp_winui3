namespace YtDlpGUI.Services;

public interface IFileDialogService
{
    Task<string?> PickFolderAsync();

    Task<string?> PickFileAsync(params string[] fileTypes);
}
