using System.Diagnostics;

namespace YtDlpGUI.Services;

public sealed class LauncherService : ILauncherService
{
    public Task OpenPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.CompletedTask;
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return Task.CompletedTask;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });

        return Task.CompletedTask;
    }

    public Task OpenContainingFolderAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.CompletedTask;
        }

        var folderPath = File.Exists(path)
            ? Path.GetDirectoryName(path)
            : path;

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return Task.CompletedTask;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true,
        });

        return Task.CompletedTask;
    }
}
