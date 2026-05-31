using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGUI.Core.Enums;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;
using YtDlpGUI.Services;

namespace YtDlpGUI.ViewModels;

public sealed partial class DownloadComposerViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<string> AudioOnlyFormats = ["mp3", "m4a", "aac", "opus", "flac"];
    private static readonly IReadOnlyList<string> VideoContainerFormats = ["best", "mp4", "mkv", "webm", "mov", "flv"];
    private static readonly IReadOnlyList<string> VideoQualityOptions = ["Best", "Up To 1080p", "Up To 720p", "Custom"];
    private static readonly IReadOnlyList<string> AudioQualityOptions = ["Best", "128 kbps", "192 kbps", "256 kbps", "320 kbps"];

    private readonly IQueueService _queueService;
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IUiDispatcher _uiDispatcher;

    private Guid? _trackedJobId;

    public DownloadComposerViewModel(
        IQueueService queueService,
        ISettingsService settingsService,
        IFileDialogService fileDialogService,
        IUiDispatcher uiDispatcher)
    {
        _queueService = queueService;
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        _uiDispatcher = uiDispatcher;

        DownloadNowCommand = new AsyncRelayCommand(DownloadNowAsync, CanQueue);
        AddToQueueCommand = new AsyncRelayCommand(AddToQueueAsync, CanQueue);
        CancelCurrentCommand = new AsyncRelayCommand(CancelCurrentAsync, () => _trackedJobId.HasValue);
        BrowseOutputFolderCommand = new AsyncRelayCommand(BrowseOutputFolderAsync);
        BrowseArchiveFileCommand = new AsyncRelayCommand(BrowseArchiveFileAsync);
        BrowseCookiesFileCommand = new AsyncRelayCommand(BrowseCookiesFileAsync);

        _queueService.JobAdded += HandleJobAdded;
        _queueService.JobUpdated += HandleJobUpdated;

        _ = LoadSettingsAsync();
    }

    public IReadOnlyList<DownloadMode> DownloadModes { get; } = Enum.GetValues<DownloadMode>();

    public IReadOnlyList<QualityPreset> QualityPresets { get; } = Enum.GetValues<QualityPreset>();

    public IReadOnlyList<PlaylistMode> PlaylistModes { get; } = Enum.GetValues<PlaylistMode>();

    public IReadOnlyList<string> FormatOptions
        => SelectedMode == DownloadMode.AudioOnly ? AudioOnlyFormats : VideoContainerFormats;

    public string FormatLabel
        => SelectedMode == DownloadMode.AudioOnly ? "Audio format" : "Video format";

    public IReadOnlyList<string> QualityOptions
        => SelectedMode == DownloadMode.AudioOnly ? AudioQualityOptions : VideoQualityOptions;

    public string QualityLabel
        => SelectedMode == DownloadMode.AudioOnly ? "Audio quality" : "Quality";

    public bool IsCustomVideoQuality
        => SelectedMode == DownloadMode.Video && SelectedQuality == QualityPreset.Custom;

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private DownloadMode selectedMode;

    [ObservableProperty]
    private QualityPreset selectedQuality;

    [ObservableProperty]
    private string selectedQualityOption = "Best";

    [ObservableProperty]
    private string customFormat = string.Empty;

    [ObservableProperty]
    private PlaylistMode selectedPlaylistMode;

    [ObservableProperty]
    private bool downloadSubtitles;

    [ObservableProperty]
    private bool downloadAutoSubtitles;

    [ObservableProperty]
    private bool embedSubtitles;

    [ObservableProperty]
    private string subtitleLanguages = "en.*";

    [ObservableProperty]
    private string subtitleFormat = "best";

    [ObservableProperty]
    private bool embedMetadata = true;

    [ObservableProperty]
    private bool embedThumbnail;

    [ObservableProperty]
    private bool embedChapters = true;

    [ObservableProperty]
    private string audioFormat = "best";

    [ObservableProperty]
    private string audioQuality = string.Empty;

    [ObservableProperty]
    private string outputDirectory = string.Empty;

    [ObservableProperty]
    private string outputTemplate = "%(title)s [%(id)s].%(ext)s";

    [ObservableProperty]
    private int retries = 10;

    [ObservableProperty]
    private string rateLimit = string.Empty;

    [ObservableProperty]
    private string proxyUrl = string.Empty;

    [ObservableProperty]
    private string userAgent = string.Empty;

    [ObservableProperty]
    private string referer = string.Empty;

    [ObservableProperty]
    private bool useDownloadArchive;

    [ObservableProperty]
    private string downloadArchiveFile = string.Empty;

    [ObservableProperty]
    private bool useCookiesFile;

    [ObservableProperty]
    private string cookiesFilePath = string.Empty;

    [ObservableProperty]
    private bool useCookiesFromBrowser;

    [ObservableProperty]
    private string cookiesFromBrowser = string.Empty;

    [ObservableProperty]
    private string formatSort = string.Empty;

    [ObservableProperty]
    private bool sponsorBlockMark;

    [ObservableProperty]
    private string sponsorBlockMarkCategories = "default";

    [ObservableProperty]
    private bool sponsorBlockRemove;

    [ObservableProperty]
    private string sponsorBlockRemoveCategories = "default";

    [ObservableProperty]
    private string extraArguments = string.Empty;

    [ObservableProperty]
    private double currentProgress;

    [ObservableProperty]
    private string currentStatus = "Idle";

    [ObservableProperty]
    private string currentTitle = string.Empty;

    [ObservableProperty]
    private string currentSpeed = string.Empty;

    [ObservableProperty]
    private string currentEta = string.Empty;

    public ObservableCollection<LogEntry> LiveLogs { get; } = [];

    public bool IsAudioMode => SelectedMode == DownloadMode.AudioOnly;

    public bool IsVideoMode => SelectedMode == DownloadMode.Video;

    public string CurrentProgressText => $"{Math.Round(CurrentProgress, 1):0.#}%";

    public IAsyncRelayCommand DownloadNowCommand { get; }

    public IAsyncRelayCommand AddToQueueCommand { get; }

    public IAsyncRelayCommand CancelCurrentCommand { get; }

    public IAsyncRelayCommand BrowseOutputFolderCommand { get; }

    public IAsyncRelayCommand BrowseArchiveFileCommand { get; }

    public IAsyncRelayCommand BrowseCookiesFileCommand { get; }

    public async Task ApplySettingsAsync(AppSettings settings)
    {
        await _uiDispatcher.EnqueueAsync(() => ApplySettings(settings));
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync().ConfigureAwait(false);
        await ApplySettingsAsync(settings).ConfigureAwait(false);
    }

    private void ApplySettings(AppSettings settings)
    {
        SelectedMode = settings.DefaultDownloadMode;
        SelectedQuality = settings.DefaultQualityPreset;
        DownloadSubtitles = settings.DefaultDownloadSubtitles;
        DownloadAutoSubtitles = settings.DefaultDownloadAutoSubtitles;
        EmbedSubtitles = settings.DefaultEmbedSubtitles;
        EmbedMetadata = settings.DefaultEmbedMetadata;
        EmbedThumbnail = settings.DefaultEmbedThumbnail;
        Retries = settings.DefaultRetries;
        RateLimit = settings.DefaultRateLimit;
        ProxyUrl = settings.DefaultProxy;
        UserAgent = settings.DefaultUserAgent;
        Referer = settings.DefaultReferer;
        UseDownloadArchive = settings.DefaultUseDownloadArchive;
        DownloadArchiveFile = settings.DefaultDownloadArchivePath;
        UseCookiesFromBrowser = settings.DefaultUseCookiesFromBrowser;
        CookiesFromBrowser = settings.DefaultCookiesFromBrowser;
        UseCookiesFile = settings.DefaultUseCookiesFile;
        CookiesFilePath = settings.DefaultCookiesFilePath;
        OutputDirectory = settings.DefaultOutputDirectory;
        OutputTemplate = settings.DefaultOutputTemplate;
        ExtraArguments = settings.DefaultExtraArguments;
        AudioFormat = SelectedMode == DownloadMode.AudioOnly ? "mp3" : "best";
        AudioQuality = string.Empty;
        SelectedQualityOption = SelectedMode == DownloadMode.AudioOnly
            ? ToAudioQualityOption(AudioQuality)
            : ToVideoQualityOption(SelectedQuality);
    }

    private bool CanQueue() => !string.IsNullOrWhiteSpace(Url);

    private async Task DownloadNowAsync()
    {
        if (!ValidateRequest())
        {
            return;
        }

        var request = BuildRequest();
        var id = await _queueService.EnqueueAsync(request).ConfigureAwait(false);
        _trackedJobId = id;
        await _queueService.StartAsync().ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() =>
        {
            StatusMessage = "Download started.";
            CurrentStatus = "Queued";
            LiveLogs.Clear();
            NotifyCancelAvailability();
        }).ConfigureAwait(false);
    }

    private async Task AddToQueueAsync()
    {
        if (!ValidateRequest())
        {
            return;
        }

        var request = BuildRequest();
        await _queueService.EnqueueAsync(request).ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() => StatusMessage = "Added to queue.").ConfigureAwait(false);
    }

    private async Task CancelCurrentAsync()
    {
        if (!_trackedJobId.HasValue)
        {
            return;
        }

        await _queueService.CancelAsync(_trackedJobId.Value).ConfigureAwait(false);
    }

    private async Task BrowseOutputFolderAsync()
    {
        var folder = await _fileDialogService.PickFolderAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        await _uiDispatcher.EnqueueAsync(() => OutputDirectory = folder).ConfigureAwait(false);
    }

    private async Task BrowseArchiveFileAsync()
    {
        var file = await _fileDialogService.PickFileAsync(".txt", "*").ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(file))
        {
            return;
        }

        await _uiDispatcher.EnqueueAsync(() => DownloadArchiveFile = file).ConfigureAwait(false);
    }

    private async Task BrowseCookiesFileAsync()
    {
        var file = await _fileDialogService.PickFileAsync(".txt", ".cookies", "*").ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(file))
        {
            return;
        }

        await _uiDispatcher.EnqueueAsync(() => CookiesFilePath = file).ConfigureAwait(false);
    }

    private bool ValidateRequest()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "URL is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            StatusMessage = "Choose an output folder first.";
            return false;
        }

        if (UseDownloadArchive && string.IsNullOrWhiteSpace(DownloadArchiveFile))
        {
            StatusMessage = "Download archive is enabled but file path is empty.";
            return false;
        }

        if (UseCookiesFile && string.IsNullOrWhiteSpace(CookiesFilePath))
        {
            StatusMessage = "Cookies file is enabled but file path is empty.";
            return false;
        }

        if (UseCookiesFromBrowser && string.IsNullOrWhiteSpace(CookiesFromBrowser))
        {
            StatusMessage = "Browser cookies are enabled but browser value is empty.";
            return false;
        }

        if (IsCustomVideoQuality &&
            string.IsNullOrWhiteSpace(CustomFormat) &&
            string.IsNullOrWhiteSpace(FormatSort))
        {
            StatusMessage = "Custom quality needs a format selector, a format sort rule, or both.";
            return false;
        }

        StatusMessage = string.Empty;
        return true;
    }

    private DownloadRequest BuildRequest()
        => new()
        {
            Url = Url.Trim(),
            Mode = SelectedMode,
            QualityPreset = SelectedQuality,
            CustomFormat = IsCustomVideoQuality ? CustomFormat.Trim() : string.Empty,
            PlaylistMode = SelectedPlaylistMode,
            DownloadSubtitles = DownloadSubtitles,
            DownloadAutoSubtitles = DownloadAutoSubtitles,
            EmbedSubtitles = EmbedSubtitles,
            SubtitleLanguages = SubtitleLanguages,
            SubtitleFormat = SubtitleFormat,
            EmbedMetadata = EmbedMetadata,
            EmbedThumbnail = EmbedThumbnail,
            EmbedChapters = EmbedChapters,
            AudioFormat = SelectedMode == DownloadMode.AudioOnly ? AudioFormat : "best",
            AudioQuality = AudioQuality,
            OutputDirectory = OutputDirectory,
            OutputTemplate = OutputTemplate,
            Retries = Retries,
            RateLimit = RateLimit,
            ProxyUrl = ProxyUrl,
            UserAgent = UserAgent,
            Referer = Referer,
            UseDownloadArchive = UseDownloadArchive,
            DownloadArchiveFile = DownloadArchiveFile,
            UseCookiesFile = UseCookiesFile,
            CookiesFilePath = CookiesFilePath,
            UseCookiesFromBrowser = UseCookiesFromBrowser,
            CookiesFromBrowser = CookiesFromBrowser,
            FormatSort = IsCustomVideoQuality ? FormatSort.Trim() : string.Empty,
            SponsorBlockMark = SponsorBlockMark,
            SponsorBlockMarkCategories = SponsorBlockMarkCategories,
            SponsorBlockRemove = SponsorBlockRemove,
            SponsorBlockRemoveCategories = SponsorBlockRemoveCategories,
            ExtraArguments = BuildEffectiveExtraArguments(),
        };

    partial void OnUrlChanged(string value)
    {
        NotifyQueueAvailability();
    }

    partial void OnSelectedModeChanged(DownloadMode value)
    {
        OnPropertyChanged(nameof(IsAudioMode));
        OnPropertyChanged(nameof(IsVideoMode));
        OnPropertyChanged(nameof(FormatOptions));
        OnPropertyChanged(nameof(FormatLabel));
        OnPropertyChanged(nameof(QualityOptions));
        OnPropertyChanged(nameof(QualityLabel));
        var validFormats = value == DownloadMode.AudioOnly ? AudioOnlyFormats : VideoContainerFormats;
        if (!validFormats.Contains(AudioFormat, StringComparer.OrdinalIgnoreCase))
        {
            AudioFormat = value == DownloadMode.AudioOnly ? "mp3" : "best";
        }

        var desired = value == DownloadMode.AudioOnly
            ? ToAudioQualityOption(AudioQuality)
            : ToVideoQualityOption(SelectedQuality);
        if (!string.Equals(SelectedQualityOption, desired, StringComparison.Ordinal))
        {
            SelectedQualityOption = desired;
        }
    }

    partial void OnSelectedQualityChanged(QualityPreset value)
    {
        if (SelectedMode != DownloadMode.Video)
        {
            return;
        }

        var desired = ToVideoQualityOption(value);
        if (!string.Equals(SelectedQualityOption, desired, StringComparison.Ordinal))
        {
            SelectedQualityOption = desired;
        }
    }

    partial void OnAudioQualityChanged(string value)
    {
        if (SelectedMode != DownloadMode.AudioOnly)
        {
            return;
        }

        var desired = ToAudioQualityOption(value);
        if (!string.Equals(SelectedQualityOption, desired, StringComparison.Ordinal))
        {
            SelectedQualityOption = desired;
        }
    }

    partial void OnSelectedQualityOptionChanged(string value)
    {
        if (SelectedMode == DownloadMode.AudioOnly)
        {
            var parsedAudioQuality = ParseAudioQualityOption(value);
            if (!string.Equals(AudioQuality, parsedAudioQuality, StringComparison.OrdinalIgnoreCase))
            {
                AudioQuality = parsedAudioQuality;
            }

            return;
        }

        var parsedQuality = ParseVideoQualityOption(value);
        if (SelectedQuality != parsedQuality)
        {
            SelectedQuality = parsedQuality;
        }

    }

    partial void OnCurrentProgressChanged(double value)
    {
        OnPropertyChanged(nameof(CurrentProgressText));
    }

    private string BuildEffectiveExtraArguments()
    {
        var args = ExtraArguments?.Trim() ?? string.Empty;

        if (SelectedMode != DownloadMode.Video || string.Equals(AudioFormat, "best", StringComparison.OrdinalIgnoreCase))
        {
            return args;
        }

        if (args.Contains("--merge-output-format", StringComparison.OrdinalIgnoreCase))
        {
            return args;
        }

        var mergeArg = $"--merge-output-format {AudioFormat}";
        return string.IsNullOrWhiteSpace(args) ? mergeArg : $"{args} {mergeArg}";
    }

    private void NotifyQueueAvailability()
    {
        if (DownloadNowCommand is AsyncRelayCommand downloadNow)
        {
            downloadNow.NotifyCanExecuteChanged();
        }

        if (AddToQueueCommand is AsyncRelayCommand addToQueue)
        {
            addToQueue.NotifyCanExecuteChanged();
        }
    }

    private void NotifyCancelAvailability()
    {
        if (CancelCurrentCommand is AsyncRelayCommand cancel)
        {
            cancel.NotifyCanExecuteChanged();
        }
    }

    private void HandleJobAdded(object? sender, DownloadJob job)
    {
        if (!_trackedJobId.HasValue)
        {
            _trackedJobId = job.Id;
            _ = _uiDispatcher.EnqueueAsync(NotifyCancelAvailability);
        }
    }

    private void HandleJobUpdated(object? sender, DownloadJob job)
    {
        if (_trackedJobId != job.Id)
        {
            if (!_trackedJobId.HasValue && job.Status == QueueItemStatus.Running)
            {
                _trackedJobId = job.Id;
                _ = _uiDispatcher.EnqueueAsync(NotifyCancelAvailability);
            }
            else
            {
                return;
            }
        }

        _ = _uiDispatcher.EnqueueAsync(() =>
        {
            CurrentProgress = Math.Clamp(job.Progress.Percent ?? 0d, 0d, 100d);
            CurrentSpeed = job.Progress.SpeedText;
            CurrentEta = job.Progress.EtaText;
            CurrentTitle = job.CurrentTitle;
            CurrentStatus = job.Status switch
            {
                QueueItemStatus.Pending => "Queued",
                QueueItemStatus.Running => "Downloading",
                QueueItemStatus.Completed => "Completed",
                QueueItemStatus.Failed => "Failed",
                QueueItemStatus.Canceled => "Canceled",
                _ => "Idle",
            };

            LiveLogs.Clear();
            foreach (var log in job.Logs.TakeLast(120))
            {
                LiveLogs.Add(log);
            }

            StatusMessage = job.Status switch
            {
                QueueItemStatus.Completed => "Download completed.",
                QueueItemStatus.Failed => string.IsNullOrWhiteSpace(job.LastError) ? "Download failed." : job.LastError,
                QueueItemStatus.Canceled => "Download canceled.",
                _ => StatusMessage,
            };

            if (job.Status is QueueItemStatus.Completed or QueueItemStatus.Failed or QueueItemStatus.Canceled)
            {
                _trackedJobId = null;
                NotifyCancelAvailability();
            }
        });
    }

    private static string ToVideoQualityOption(QualityPreset preset)
        => preset switch
        {
            QualityPreset.UpTo1080p => "Up To 1080p",
            QualityPreset.UpTo720p => "Up To 720p",
            QualityPreset.Custom => "Custom",
            _ => "Best",
        };

    private static QualityPreset ParseVideoQualityOption(string option)
    {
        var normalized = (option ?? string.Empty).Trim();
        return normalized switch
        {
            "Up To 1080p" => QualityPreset.UpTo1080p,
            "Up To 720p" => QualityPreset.UpTo720p,
            "Custom" => QualityPreset.Custom,
            _ => QualityPreset.Best,
        };
    }

    private static string ToAudioQualityOption(string quality)
    {
        var normalized = (quality ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);
        return normalized switch
        {
            "128K" => "128 kbps",
            "192K" => "192 kbps",
            "256K" => "256 kbps",
            "320K" => "320 kbps",
            _ => "Best",
        };
    }

    private static string ParseAudioQualityOption(string option)
    {
        var normalized = (option ?? string.Empty).Trim();
        return normalized switch
        {
            "128 kbps" => "128K",
            "192 kbps" => "192K",
            "256 kbps" => "256K",
            "320 kbps" => "320K",
            _ => string.Empty,
        };
    }
}
