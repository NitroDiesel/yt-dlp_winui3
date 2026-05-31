using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGUI.Core.Enums;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;
using YtDlpGUI.Helpers;
using YtDlpGUI.Services;

namespace YtDlpGUI.ViewModels;

public sealed partial class AddDownloadDialogViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<string> AudioOnlyFormats = ["mp3", "m4a", "aac", "opus", "flac"];
    private static readonly IReadOnlyList<string> VideoContainerFormats = ["best", "mp4", "mkv", "webm", "mov", "flv"];
    private static readonly IReadOnlyList<string> VideoQualityOptions = ["Best", "Up To 1080p", "Up To 720p", "Custom"];
    private static readonly IReadOnlyList<string> AudioQualityOptions = ["Best", "128 kbps", "192 kbps", "256 kbps", "320 kbps"];

    private readonly IQueueService _queueService;
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IUiDispatcher _uiDispatcher;

    private AppSettings _settings = new();

    public AddDownloadDialogViewModel(
        IQueueService queueService,
        ISettingsService settingsService,
        IFileDialogService fileDialogService,
        IClipboardService clipboardService,
        IUiDispatcher uiDispatcher)
    {
        _queueService = queueService;
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        _clipboardService = clipboardService;
        _uiDispatcher = uiDispatcher;

        BrowseOutputFolderCommand = new AsyncRelayCommand(BrowseOutputFolderAsync);
        BrowseCookiesFileCommand = new AsyncRelayCommand(BrowseCookiesFileAsync);
    }

    public IReadOnlyList<DownloadMode> DownloadModes { get; } = Enum.GetValues<DownloadMode>();

    public IReadOnlyList<QualityPreset> QualityPresets { get; } = Enum.GetValues<QualityPreset>();

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
    private string urlsText = string.Empty;

    [ObservableProperty]
    private DownloadMode selectedMode;

    [ObservableProperty]
    private QualityPreset selectedQuality;

    [ObservableProperty]
    private string selectedQualityOption = "Best";

    [ObservableProperty]
    private string audioFormat = "best";

    [ObservableProperty]
    private string audioQuality = string.Empty;

    [ObservableProperty]
    private string customFormat = string.Empty;

    [ObservableProperty]
    private string formatSort = string.Empty;

    [ObservableProperty]
    private string outputDirectory = string.Empty;

    [ObservableProperty]
    private string outputTemplate = "%(title)s [%(id)s].%(ext)s";

    [ObservableProperty]
    private bool showAdvanced;

    [ObservableProperty]
    private int retries = 10;

    [ObservableProperty]
    private string proxyUrl = string.Empty;

    [ObservableProperty]
    private string userAgent = string.Empty;

    [ObservableProperty]
    private string referer = string.Empty;

    [ObservableProperty]
    private bool useCookiesFile;

    [ObservableProperty]
    private string cookiesFilePath = string.Empty;

    [ObservableProperty]
    private bool useCookiesFromBrowser;

    [ObservableProperty]
    private string cookiesFromBrowser = string.Empty;

    [ObservableProperty]
    private bool downloadSubtitles;

    [ObservableProperty]
    private bool embedSubtitles;

    [ObservableProperty]
    private bool embedMetadata = true;

    [ObservableProperty]
    private bool embedThumbnail;

    [ObservableProperty]
    private bool startImmediately = true;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    public int AddedCount { get; private set; }

    public IAsyncRelayCommand BrowseOutputFolderCommand { get; }

    public IAsyncRelayCommand BrowseCookiesFileCommand { get; }

    public async Task InitializeForOpenAsync(string? seedUrl = null, CancellationToken cancellationToken = default)
    {
        _settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);

        SelectedMode = _settings.DefaultDownloadMode;
        SelectedQuality = _settings.DefaultQualityPreset;
        DownloadSubtitles = _settings.DefaultDownloadSubtitles;
        EmbedSubtitles = _settings.DefaultEmbedSubtitles;
        EmbedMetadata = _settings.DefaultEmbedMetadata;
        EmbedThumbnail = _settings.DefaultEmbedThumbnail;
        Retries = _settings.DefaultRetries;
        ProxyUrl = _settings.DefaultProxy;
        UserAgent = _settings.DefaultUserAgent;
        Referer = _settings.DefaultReferer;
        UseCookiesFile = _settings.DefaultUseCookiesFile;
        CookiesFilePath = _settings.DefaultCookiesFilePath;
        UseCookiesFromBrowser = _settings.DefaultUseCookiesFromBrowser;
        CookiesFromBrowser = _settings.DefaultCookiesFromBrowser;
        OutputDirectory = _settings.DefaultOutputDirectory;
        OutputTemplate = _settings.DefaultOutputTemplate;
        AudioFormat = SelectedMode == DownloadMode.AudioOnly ? "mp3" : "best";
        AudioQuality = string.Empty;
        CustomFormat = string.Empty;
        FormatSort = string.Empty;
        SelectedQualityOption = SelectedMode == DownloadMode.AudioOnly
            ? ToAudioQualityOption(AudioQuality)
            : ToVideoQualityOption(SelectedQuality);
        StartImmediately = _settings.StartQueueAfterAddDialogSubmit;
        ShowAdvanced = false;
        ValidationMessage = string.Empty;
        AddedCount = 0;

        var candidate = seedUrl?.Trim();
        if (string.IsNullOrWhiteSpace(candidate) && _settings.AutoFillAddDialogFromClipboard)
        {
            candidate = (await _clipboardService.GetTextAsync().ConfigureAwait(false))?.Trim();
        }

        UrlsText = UrlInputParser.Parse(candidate).Count > 0 ? candidate ?? string.Empty : string.Empty;
    }

    public async Task<bool> SubmitAsync(CancellationToken cancellationToken = default)
    {
        ValidationMessage = string.Empty;

        var urls = UrlInputParser.Parse(UrlsText);
        if (urls.Count == 0)
        {
            ValidationMessage = "Paste at least one valid URL.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            ValidationMessage = "Choose an output folder.";
            return false;
        }

        if (UseCookiesFile && string.IsNullOrWhiteSpace(CookiesFilePath))
        {
            ValidationMessage = "Cookies file is enabled but path is empty.";
            return false;
        }

        if (UseCookiesFromBrowser && string.IsNullOrWhiteSpace(CookiesFromBrowser))
        {
            ValidationMessage = "Browser cookies are enabled but browser value is empty.";
            return false;
        }

        if (IsCustomVideoQuality &&
            string.IsNullOrWhiteSpace(CustomFormat) &&
            string.IsNullOrWhiteSpace(FormatSort))
        {
            ValidationMessage = "Custom quality needs a format selector, a format sort rule, or both.";
            return false;
        }

        var invalid = urls.Where(static url => !Uri.TryCreate(url, UriKind.Absolute, out _)).ToList();
        if (invalid.Count > 0)
        {
            ValidationMessage = $"Invalid URL: {invalid[0]}";
            return false;
        }

        var baseRequest = DownloadRequest.CreateDefault(_settings) with
        {
            Mode = SelectedMode,
            QualityPreset = SelectedQuality,
            CustomFormat = IsCustomVideoQuality ? CustomFormat.Trim() : string.Empty,
            AudioFormat = SelectedMode == DownloadMode.AudioOnly ? AudioFormat : "best",
            AudioQuality = SelectedMode == DownloadMode.AudioOnly ? AudioQuality : string.Empty,
            DownloadSubtitles = DownloadSubtitles,
            EmbedSubtitles = EmbedSubtitles,
            EmbedMetadata = EmbedMetadata,
            EmbedThumbnail = EmbedThumbnail,
            Retries = Math.Max(0, Retries),
            ProxyUrl = ProxyUrl.Trim(),
            UserAgent = UserAgent.Trim(),
            Referer = Referer.Trim(),
            UseCookiesFile = UseCookiesFile,
            CookiesFilePath = CookiesFilePath.Trim(),
            UseCookiesFromBrowser = UseCookiesFromBrowser,
            CookiesFromBrowser = CookiesFromBrowser.Trim(),
            OutputDirectory = OutputDirectory.Trim(),
            OutputTemplate = OutputTemplate.Trim(),
            FormatSort = IsCustomVideoQuality ? FormatSort.Trim() : string.Empty,
            ExtraArguments = BuildEffectiveExtraArguments(_settings.DefaultExtraArguments),
        };

        AddedCount = 0;
        foreach (var url in urls)
        {
            var request = baseRequest with { Url = url };
            await _queueService.EnqueueAsync(request, cancellationToken).ConfigureAwait(false);
            AddedCount++;
        }

        if (StartImmediately && AddedCount > 0)
        {
            await _queueService.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        return true;
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

    partial void OnSelectedModeChanged(DownloadMode value)
    {
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

    private string BuildEffectiveExtraArguments(string baseArgs)
    {
        var args = baseArgs?.Trim() ?? string.Empty;

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

    private async Task BrowseCookiesFileAsync()
    {
        var file = await _fileDialogService.PickFileAsync(".txt", ".cookies", "*").ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(file))
        {
            return;
        }

        await _uiDispatcher.EnqueueAsync(() => CookiesFilePath = file).ConfigureAwait(false);
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
