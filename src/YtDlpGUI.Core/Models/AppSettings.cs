using YtDlpGUI.Core.Enums;

namespace YtDlpGUI.Core.Models;

public sealed record AppSettings
{
    public string YtDlpExecutablePath { get; init; } = "yt-dlp";

    public bool UsePythonModule { get; init; }

    public string PythonExecutablePath { get; init; } = "python";

    public string FfmpegLocation { get; init; } = string.Empty;

    public string DefaultOutputDirectory { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    public string DefaultOutputTemplate { get; init; } = "%(title)s [%(id)s].%(ext)s";

    public DownloadMode DefaultDownloadMode { get; init; } = DownloadMode.Video;

    public QualityPreset DefaultQualityPreset { get; init; } = QualityPreset.Best;

    public bool DefaultDownloadSubtitles { get; init; }

    public bool DefaultDownloadAutoSubtitles { get; init; }

    public bool DefaultEmbedSubtitles { get; init; }

    public bool DefaultEmbedMetadata { get; init; } = true;

    public bool DefaultEmbedThumbnail { get; init; }

    public int DefaultRetries { get; init; } = 10;

    public string DefaultRateLimit { get; init; } = string.Empty;

    public string DefaultProxy { get; init; } = string.Empty;

    public string DefaultUserAgent { get; init; } = string.Empty;

    public string DefaultReferer { get; init; } = string.Empty;

    public string DefaultExtraArguments { get; init; } = string.Empty;

    public bool DefaultUseDownloadArchive { get; init; }

    public string DefaultDownloadArchivePath { get; init; } = string.Empty;

    public bool DefaultUseCookiesFromBrowser { get; init; }

    public string DefaultCookiesFromBrowser { get; init; } = string.Empty;

    public bool DefaultUseCookiesFile { get; init; }

    public string DefaultCookiesFilePath { get; init; } = string.Empty;

    public bool AutoStartQueue { get; init; } = true;

    public bool AutoFillAddDialogFromClipboard { get; init; } = true;

    public bool StartQueueAfterAddDialogSubmit { get; init; } = true;

    public bool VerboseLogging { get; init; }
}
