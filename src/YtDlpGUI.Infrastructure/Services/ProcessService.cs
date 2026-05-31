using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Infrastructure.Services;

public sealed class ProcessService : IProcessService
{
    public async Task<int> RunAsync(
        ProcessRunRequest request,
        Func<string, Task> onStandardOutput,
        Func<string, Task> onStandardError,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = request.WorkingDirectory,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (request.EnvironmentVariables is not null)
        {
            foreach (var pair in request.EnvironmentVariables)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start yt-dlp process.");
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode is 2 or 3)
        {
            throw new FileNotFoundException(
                $"Unable to start download engine '{request.FileName}'. " +
                "Open Settings > Advanced > Engine Paths and set a valid yt-dlp executable path.",
                request.FileName,
                ex);
        }

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best effort cancellation.
            }
        });

        var stdoutTask = PumpReaderAsync(process.StandardOutput, onStandardOutput);
        var stderrTask = PumpReaderAsync(process.StandardError, onStandardError);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    private static async Task PumpReaderAsync(StreamReader reader, Func<string, Task> callback)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                return;
            }

            await callback(line).ConfigureAwait(false);
        }
    }
}
