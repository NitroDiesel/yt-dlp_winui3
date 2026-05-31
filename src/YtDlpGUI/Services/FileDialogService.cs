using WinRT.Interop;

namespace YtDlpGUI.Services;

public sealed class FileDialogService : IFileDialogService
{
    public async Task<string?> PickFolderAsync()
    {
        if (App.MainWindow is null)
        {
            return null;
        }

        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    public async Task<string?> PickFileAsync(params string[] fileTypes)
    {
        if (App.MainWindow is null)
        {
            return null;
        }

        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        if (fileTypes.Length == 0)
        {
            picker.FileTypeFilter.Add("*");
        }
        else
        {
            foreach (var fileType in fileTypes)
            {
                picker.FileTypeFilter.Add(fileType);
            }
        }

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
