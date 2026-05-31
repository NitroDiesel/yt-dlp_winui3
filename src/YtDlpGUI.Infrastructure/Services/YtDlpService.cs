using YtDlpGUI.Core.Enums;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Infrastructure.Services;

public sealed class YtDlpService(
    IYtDlpCommandBuilder commandBuilder,
    IProcessService processService,
    IOutputParserService outputParser) : IYtDlpService
{
    public async Task<DownloadRunResult> RunDownloadAsync(
        DownloadRequest request,
        AppSettings settings,
        Func<OutputParseResult, Task> onOutput,
        CancellationToken cancellationToken)
    {
        var collectedLogs = new List<LogEntry>();
        string? finalOutputPath = null;
        string? lastError = null;

        var processRequest = commandBuilder.BuildProcessRequest(request, settings);

        async Task HandleLineAsync(string line, bool isStdErr)
        {
            var parsed = outputParser.Parse(line, isStdErr);

            if (parsed.Type == OutputLineType.InfoAfter || parsed.Type == OutputLineType.Destination)
            {
                if (!string.IsNullOrWhiteSpace(parsed.OutputPath))
                {
                    finalOutputPath = parsed.OutputPath;
                }
            }

            if (parsed.Type == OutputLineType.Error)
            {
                lastError = parsed.Message;
            }

            var severity = parsed.Type switch
            {
                OutputLineType.Warning => LogSeverity.Warning,
                OutputLineType.Error => LogSeverity.Error,
                _ => LogSeverity.Info,
            };

            if (!string.IsNullOrWhiteSpace(parsed.Message))
            {
                lock (collectedLogs)
                {
                    collectedLogs.Add(new LogEntry(DateTimeOffset.UtcNow, severity, parsed.Message));
                }
            }

            await onOutput(parsed).ConfigureAwait(false);
        }

        try
        {
            var exitCode = await processService.RunAsync(
                processRequest,
                line => HandleLineAsync(line, isStdErr: false),
                line => HandleLineAsync(line, isStdErr: true),
                cancellationToken).ConfigureAwait(false);

            return new DownloadRunResult(exitCode, IsCanceled: false, lastError, finalOutputPath, collectedLogs);
        }
        catch (OperationCanceledException)
        {
            return new DownloadRunResult(-1, IsCanceled: true, "Download canceled", finalOutputPath, collectedLogs);
        }
        catch (Exception ex)
        {
            return new DownloadRunResult(-1, IsCanceled: false, ex.Message, finalOutputPath, collectedLogs);
        }
    }
}
