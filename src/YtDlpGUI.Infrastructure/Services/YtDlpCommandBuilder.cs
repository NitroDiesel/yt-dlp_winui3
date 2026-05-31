using System.Globalization;
using YtDlpGUI.Core.Enums;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;
using YtDlpGUI.Infrastructure.Helpers;

namespace YtDlpGUI.Infrastructure.Services;

public sealed class YtDlpCommandBuilder : IYtDlpCommandBuilder
{
    private const string ProgressPrefix = "__YTDLP_PROGRESS__";
    private const string PostProcessPrefix = "__YTDLP_POSTPROCESS__";
    private const string InfoBeforePrefix = "__YTDLP_INFO_BEFORE__";
    private const string InfoAfterPrefix = "__YTDLP_INFO_AFTER__";

    public ProcessRunRequest BuildProcessRequest(DownloadRequest request, AppSettings settings)
    {
        var arguments = new List<string>();
        var executable = ResolveExecutable(settings, arguments);

        arguments.Add("--ignore-config");
        arguments.Add("--newline");
        arguments.Add("--no-color");
        arguments.Add("--progress");
        arguments.Add("--progress-template");
        arguments.Add($"download:{ProgressPrefix}%(progress)j");
        arguments.Add("--progress-template");
        arguments.Add($"postprocess:{PostProcessPrefix}%(progress)j");
        arguments.Add("--print");
        arguments.Add($"before_dl:{InfoBeforePrefix}%(.{{id,title,extractor,webpage_url,playlist,playlist_index,playlist_count,duration}})j");
        arguments.Add("--print");
        arguments.Add($"after_move:{InfoAfterPrefix}%(.{{id,title,ext,filepath,filename}})j");

        if (settings.VerboseLogging)
        {
            arguments.Add("--verbose");
        }

        AddIfNotEmpty(arguments, "--ffmpeg-location", ResolveFfmpegLocation(settings.FfmpegLocation));

        if (request.Mode == DownloadMode.AudioOnly)
        {
            arguments.Add("-x");
            arguments.Add("-f");
            arguments.Add("ba/b");
            AddIfNotEmpty(arguments, "--audio-format", request.AudioFormat);
            AddIfNotEmpty(arguments, "--audio-quality", request.AudioQuality);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.CustomFormat))
            {
                arguments.Add("-f");
                arguments.Add(request.CustomFormat);
            }

            var effectiveSort = request.FormatSort;
            if (string.IsNullOrWhiteSpace(effectiveSort))
            {
                effectiveSort = request.QualityPreset switch
                {
                    QualityPreset.UpTo1080p => "res:1080",
                    QualityPreset.UpTo720p => "res:720",
                    _ => string.Empty,
                };
            }

            AddIfNotEmpty(arguments, "-S", effectiveSort);
        }

        switch (request.PlaylistMode)
        {
            case PlaylistMode.SingleVideo:
                arguments.Add("--no-playlist");
                break;
            case PlaylistMode.EntirePlaylist:
                arguments.Add("--yes-playlist");
                break;
        }

        if (request.DownloadSubtitles)
        {
            arguments.Add("--write-subs");
        }

        if (request.DownloadAutoSubtitles)
        {
            arguments.Add("--write-auto-subs");
        }

        if (request.DownloadSubtitles || request.DownloadAutoSubtitles)
        {
            AddIfNotEmpty(arguments, "--sub-langs", request.SubtitleLanguages);
            AddIfNotEmpty(arguments, "--sub-format", request.SubtitleFormat);
        }

        if (request.EmbedSubtitles)
        {
            arguments.Add("--embed-subs");
        }

        if (request.EmbedMetadata)
        {
            arguments.Add("--embed-metadata");
        }

        if (request.EmbedThumbnail)
        {
            arguments.Add("--embed-thumbnail");
        }

        if (!request.EmbedChapters)
        {
            arguments.Add("--no-embed-chapters");
        }

        if (request.SponsorBlockMark)
        {
            AddIfNotEmpty(arguments, "--sponsorblock-mark", request.SponsorBlockMarkCategories);
        }

        if (request.SponsorBlockRemove)
        {
            AddIfNotEmpty(arguments, "--sponsorblock-remove", request.SponsorBlockRemoveCategories);
        }

        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            Directory.CreateDirectory(request.OutputDirectory);
            arguments.Add("-P");
            arguments.Add(request.OutputDirectory);
        }

        var outputTemplate = string.IsNullOrWhiteSpace(request.OutputTemplate)
            ? settings.DefaultOutputTemplate
            : request.OutputTemplate;
        AddIfNotEmpty(arguments, "-o", outputTemplate);

        if (request.Retries > 0)
        {
            arguments.Add("--retries");
            arguments.Add(request.Retries.ToString(CultureInfo.InvariantCulture));
        }

        AddIfNotEmpty(arguments, "--limit-rate", request.RateLimit);
        AddIfNotEmpty(arguments, "--proxy", request.ProxyUrl);
        AddIfNotEmpty(arguments, "--user-agent", request.UserAgent);
        AddIfNotEmpty(arguments, "--referer", request.Referer);

        if (request.UseDownloadArchive)
        {
            AddIfNotEmpty(arguments, "--download-archive", request.DownloadArchiveFile);
        }

        if (request.UseCookiesFile)
        {
            AddIfNotEmpty(arguments, "--cookies", request.CookiesFilePath);
        }

        if (request.UseCookiesFromBrowser)
        {
            AddIfNotEmpty(arguments, "--cookies-from-browser", request.CookiesFromBrowser);
        }

        foreach (var token in CommandLineTokenizer.Tokenize(request.ExtraArguments))
        {
            arguments.Add(token);
        }

        arguments.Add(request.Url.Trim());

        return new ProcessRunRequest(executable, arguments, Directory.GetCurrentDirectory());
    }

    public string BuildCommandPreview(DownloadRequest request, AppSettings settings)
    {
        var processRequest = BuildProcessRequest(request, settings);
        var allTokens = new List<string> { processRequest.FileName };
        allTokens.AddRange(processRequest.Arguments);
        return string.Join(' ', allTokens.Select(QuoteIfNeeded));
    }

    private static string ResolveExecutable(AppSettings settings, List<string> arguments)
    {
        if (settings.UsePythonModule)
        {
            arguments.Add("-m");
            arguments.Add("yt_dlp");
            return string.IsNullOrWhiteSpace(settings.PythonExecutablePath) ? "python" : settings.PythonExecutablePath.Trim();
        }

        return ResolveYtDlpExecutable(settings.YtDlpExecutablePath);
    }

    private static string ResolveYtDlpExecutable(string configuredPath)
    {
        var configured = (configuredPath ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var expandedConfigured = Environment.ExpandEnvironmentVariables(configured);
            if (LooksLikePath(expandedConfigured))
            {
                var absoluteConfigured = ToAbsolutePath(expandedConfigured);
                if (File.Exists(absoluteConfigured))
                {
                    return absoluteConfigured;
                }
            }
            else if (!IsAutoYtDlpCommand(expandedConfigured))
            {
                // Custom command names should still be honored (e.g., aliases on PATH).
                return expandedConfigured;
            }
        }

        foreach (var candidate in EnumerateYtDlpCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return "yt-dlp";
    }

    private static IEnumerable<string> EnumerateYtDlpCandidates()
    {
        var baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "yt-dlp.exe");
        yield return Path.Combine(baseDir, "tools", "yt-dlp.exe");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "yt-dlp.exe");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YtDlpGUI",
            "tools",
            "yt-dlp.exe");
    }

    private static string ToAbsolutePath(string path)
        => Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path, AppContext.BaseDirectory);

    private static bool LooksLikePath(string value)
        => Path.IsPathRooted(value)
           || value.Contains('\\')
           || value.Contains('/')
           || value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
           || value.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
           || value.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

    private static bool IsAutoYtDlpCommand(string value)
        => string.Equals(value, "yt-dlp", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "yt-dlp.exe", StringComparison.OrdinalIgnoreCase);

    private static string ResolveFfmpegLocation(string configuredPath)
    {
        var configured = (configuredPath ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var expandedConfigured = Environment.ExpandEnvironmentVariables(configured);
            if (LooksLikePath(expandedConfigured))
            {
                var absoluteConfigured = ToAbsolutePath(expandedConfigured);
                if (Directory.Exists(absoluteConfigured) || File.Exists(absoluteConfigured))
                {
                    return absoluteConfigured;
                }
            }
            else
            {
                return expandedConfigured;
            }
        }

        foreach (var candidate in EnumerateFfmpegCandidates())
        {
            if (HasFfmpegPair(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> EnumerateFfmpegCandidates()
    {
        var baseDir = AppContext.BaseDirectory;
        yield return baseDir;
        yield return Path.Combine(baseDir, "tools");
        yield return Directory.GetCurrentDirectory();
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YtDlpGUI",
            "tools");
    }

    private static bool HasFfmpegPair(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return false;
        }

        var ffmpegExe = Path.Combine(folderPath, "ffmpeg.exe");
        var ffprobeExe = Path.Combine(folderPath, "ffprobe.exe");
        return File.Exists(ffmpegExe) && File.Exists(ffprobeExe);
    }

    private static void AddIfNotEmpty(List<string> arguments, string option, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        arguments.Add(option);
        arguments.Add(value.Trim());
    }

    private static string QuoteIfNeeded(string value)
        => value.Any(char.IsWhiteSpace) ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
}
