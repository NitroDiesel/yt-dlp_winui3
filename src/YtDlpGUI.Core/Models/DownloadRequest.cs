using YtDlpGUI.Core.Enums;

namespace YtDlpGUI.Core.Models;

public sealed record DownloadRequest
{
    public string Url { get; init; } = string.Empty;

    public DownloadMode Mode { get; init; }

    public QualityPreset QualityPreset { get; init; }

    public string CustomFormat { get; init; } = string.Empty;

    public PlaylistMode PlaylistMode { get; init; }

    public bool DownloadSubtitles { get; init; }

    public bool DownloadAutoSubtitles { get; init; }

    public bool EmbedSubtitles { get; init; }

    public string SubtitleLanguages { get; init; } = "en.*";

    public string SubtitleFormat { get; init; } = "best";

    public bool EmbedMetadata { get; init; }

    public bool EmbedThumbnail { get; init; }

    public bool EmbedChapters { get; init; } = true;

    public string AudioFormat { get; init; } = "mp3";

    public string AudioQuality { get; init; } = "5";

    public string OutputDirectory { get; init; } = string.Empty;

    public string OutputTemplate { get; init; } = "%(title)s [%(id)s].%(ext)s";

    public int Retries { get; init; } = 10;

    public string RateLimit { get; init; } = string.Empty;

    public string ProxyUrl { get; init; } = string.Empty;

    public string UserAgent { get; init; } = string.Empty;

    public string Referer { get; init; } = string.Empty;

    public bool UseDownloadArchive { get; init; }

    public string DownloadArchiveFile { get; init; } = string.Empty;

    public bool UseCookiesFile { get; init; }

    public string CookiesFilePath { get; init; } = string.Empty;

    public bool UseCookiesFromBrowser { get; init; }

    public string CookiesFromBrowser { get; init; } = string.Empty;

    public string FormatSort { get; init; } = string.Empty;

    public bool SponsorBlockMark { get; init; }

    public string SponsorBlockMarkCategories { get; init; } = "default";

    public bool SponsorBlockRemove { get; init; }

    public string SponsorBlockRemoveCategories { get; init; } = "default";

    public string ExtraArguments { get; init; } = string.Empty;

    public static DownloadRequest CreateDefault(AppSettings settings) =>
        new()
        {
            Mode = settings.DefaultDownloadMode,
            QualityPreset = settings.DefaultQualityPreset,
            DownloadSubtitles = settings.DefaultDownloadSubtitles,
            DownloadAutoSubtitles = settings.DefaultDownloadAutoSubtitles,
            EmbedSubtitles = settings.DefaultEmbedSubtitles,
            EmbedMetadata = settings.DefaultEmbedMetadata,
            EmbedThumbnail = settings.DefaultEmbedThumbnail,
            Retries = settings.DefaultRetries,
            RateLimit = settings.DefaultRateLimit,
            ProxyUrl = settings.DefaultProxy,
            UserAgent = settings.DefaultUserAgent,
            Referer = settings.DefaultReferer,
            UseDownloadArchive = settings.DefaultUseDownloadArchive,
            DownloadArchiveFile = settings.DefaultDownloadArchivePath,
            UseCookiesFromBrowser = settings.DefaultUseCookiesFromBrowser,
            CookiesFromBrowser = settings.DefaultCookiesFromBrowser,
            UseCookiesFile = settings.DefaultUseCookiesFile,
            CookiesFilePath = settings.DefaultCookiesFilePath,
            OutputDirectory = settings.DefaultOutputDirectory,
            OutputTemplate = settings.DefaultOutputTemplate,
            ExtraArguments = settings.DefaultExtraArguments,
        };
}
