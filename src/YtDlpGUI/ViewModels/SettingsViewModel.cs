using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGUI.Core.Enums;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;
using YtDlpGUI.Services;

namespace YtDlpGUI.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService;
    private readonly DownloadComposerViewModel _downloadComposerViewModel;
    private readonly IUiDispatcher _uiDispatcher;

    public SettingsViewModel(
        ISettingsService settingsService,
        IFileDialogService fileDialogService,
        DownloadComposerViewModel downloadComposerViewModel,
        IUiDispatcher uiDispatcher)
    {
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        _downloadComposerViewModel = downloadComposerViewModel;
        _uiDispatcher = uiDispatcher;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ReloadSettingsCommand = new AsyncRelayCommand(LoadSettingsAsync);
        BrowseDefaultOutputFolderCommand = new AsyncRelayCommand(BrowseDefaultOutputFolderAsync);
        BrowseYtDlpExecutableCommand = new AsyncRelayCommand(BrowseYtDlpExecutableAsync);
        BrowsePythonExecutableCommand = new AsyncRelayCommand(BrowsePythonExecutableAsync);
        BrowseFfmpegExecutableCommand = new AsyncRelayCommand(BrowseFfmpegExecutableAsync);
        BrowseArchiveFileCommand = new AsyncRelayCommand(BrowseArchiveFileAsync);
        BrowseCookiesFileCommand = new AsyncRelayCommand(BrowseCookiesFileAsync);

        _ = LoadSettingsAsync();
    }

    public IReadOnlyList<DownloadMode> DownloadModes { get; } = Enum.GetValues<DownloadMode>();

    public IReadOnlyList<QualityPreset> QualityPresets { get; } = Enum.GetValues<QualityPreset>();

    [ObservableProperty]
    private bool usePythonModule;

    [ObservableProperty]
    private string ytDlpExecutablePath = "yt-dlp";

    [ObservableProperty]
    private string pythonExecutablePath = "python";

    [ObservableProperty]
    private string ffmpegLocation = string.Empty;

    [ObservableProperty]
    private string defaultOutputDirectory = string.Empty;

    [ObservableProperty]
    private string defaultOutputTemplate = "%(title)s [%(id)s].%(ext)s";

    [ObservableProperty]
    private DownloadMode defaultDownloadMode;

    [ObservableProperty]
    private QualityPreset defaultQualityPreset;

    [ObservableProperty]
    private bool defaultDownloadSubtitles;

    [ObservableProperty]
    private bool defaultDownloadAutoSubtitles;

    [ObservableProperty]
    private bool defaultEmbedSubtitles;

    [ObservableProperty]
    private bool defaultEmbedMetadata = true;

    [ObservableProperty]
    private bool defaultEmbedThumbnail;

    [ObservableProperty]
    private int defaultRetries = 10;

    [ObservableProperty]
    private string defaultRateLimit = string.Empty;

    [ObservableProperty]
    private string defaultProxy = string.Empty;

    [ObservableProperty]
    private string defaultUserAgent = string.Empty;

    [ObservableProperty]
    private string defaultReferer = string.Empty;

    [ObservableProperty]
    private string defaultExtraArguments = string.Empty;

    [ObservableProperty]
    private bool defaultUseDownloadArchive;

    [ObservableProperty]
    private string defaultDownloadArchivePath = string.Empty;

    [ObservableProperty]
    private bool defaultUseCookiesFromBrowser;

    [ObservableProperty]
    private string defaultCookiesFromBrowser = string.Empty;

    [ObservableProperty]
    private bool defaultUseCookiesFile;

    [ObservableProperty]
    private string defaultCookiesFilePath = string.Empty;

    [ObservableProperty]
    private bool autoStartQueue = true;

    [ObservableProperty]
    private bool autoFillAddDialogFromClipboard = true;

    [ObservableProperty]
    private bool startQueueAfterAddDialogSubmit = true;

    [ObservableProperty]
    private bool verboseLogging;

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public IAsyncRelayCommand ReloadSettingsCommand { get; }

    public IAsyncRelayCommand BrowseDefaultOutputFolderCommand { get; }

    public IAsyncRelayCommand BrowseYtDlpExecutableCommand { get; }

    public IAsyncRelayCommand BrowsePythonExecutableCommand { get; }

    public IAsyncRelayCommand BrowseFfmpegExecutableCommand { get; }

    public IAsyncRelayCommand BrowseArchiveFileCommand { get; }

    public IAsyncRelayCommand BrowseCookiesFileCommand { get; }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync().ConfigureAwait(false);
        await _uiDispatcher.EnqueueAsync(() => ApplySettings(settings)).ConfigureAwait(false);
    }

    private async Task SaveSettingsAsync()
    {
        var settings = BuildSettings();
        await _settingsService.SaveAsync(settings).ConfigureAwait(false);
        await _downloadComposerViewModel.ApplySettingsAsync(settings).ConfigureAwait(false);

        await _uiDispatcher.EnqueueAsync(() => StatusMessage = "Settings saved.").ConfigureAwait(false);
    }

    private async Task BrowseDefaultOutputFolderAsync()
    {
        var folder = await _fileDialogService.PickFolderAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            await _uiDispatcher.EnqueueAsync(() => DefaultOutputDirectory = folder).ConfigureAwait(false);
        }
    }

    private async Task BrowseYtDlpExecutableAsync()
    {
        var file = await _fileDialogService.PickFileAsync(".exe", ".cmd", "*").ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(file))
        {
            await _uiDispatcher.EnqueueAsync(() => YtDlpExecutablePath = file).ConfigureAwait(false);
        }
    }

    private async Task BrowsePythonExecutableAsync()
    {
        var file = await _fileDialogService.PickFileAsync(".exe", "*").ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(file))
        {
            await _uiDispatcher.EnqueueAsync(() => PythonExecutablePath = file).ConfigureAwait(false);
        }
    }

    private async Task BrowseFfmpegExecutableAsync()
    {
        var file = await _fileDialogService.PickFileAsync(".exe", "*").ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(file))
        {
            await _uiDispatcher.EnqueueAsync(() => FfmpegLocation = file).ConfigureAwait(false);
        }
    }

    private async Task BrowseArchiveFileAsync()
    {
        var file = await _fileDialogService.PickFileAsync(".txt", "*").ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(file))
        {
            await _uiDispatcher.EnqueueAsync(() => DefaultDownloadArchivePath = file).ConfigureAwait(false);
        }
    }

    private async Task BrowseCookiesFileAsync()
    {
        var file = await _fileDialogService.PickFileAsync(".txt", ".cookies", "*").ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(file))
        {
            await _uiDispatcher.EnqueueAsync(() => DefaultCookiesFilePath = file).ConfigureAwait(false);
        }
    }

    private AppSettings BuildSettings() => new()
    {
        UsePythonModule = UsePythonModule,
        YtDlpExecutablePath = YtDlpExecutablePath,
        PythonExecutablePath = PythonExecutablePath,
        FfmpegLocation = FfmpegLocation,
        DefaultOutputDirectory = DefaultOutputDirectory,
        DefaultOutputTemplate = DefaultOutputTemplate,
        DefaultDownloadMode = DefaultDownloadMode,
        DefaultQualityPreset = DefaultQualityPreset,
        DefaultDownloadSubtitles = DefaultDownloadSubtitles,
        DefaultDownloadAutoSubtitles = DefaultDownloadAutoSubtitles,
        DefaultEmbedSubtitles = DefaultEmbedSubtitles,
        DefaultEmbedMetadata = DefaultEmbedMetadata,
        DefaultEmbedThumbnail = DefaultEmbedThumbnail,
        DefaultRetries = Math.Max(1, DefaultRetries),
        DefaultRateLimit = DefaultRateLimit,
        DefaultProxy = DefaultProxy,
        DefaultUserAgent = DefaultUserAgent,
        DefaultReferer = DefaultReferer,
        DefaultExtraArguments = DefaultExtraArguments,
        DefaultUseDownloadArchive = DefaultUseDownloadArchive,
        DefaultDownloadArchivePath = DefaultDownloadArchivePath,
        DefaultUseCookiesFromBrowser = DefaultUseCookiesFromBrowser,
        DefaultCookiesFromBrowser = DefaultCookiesFromBrowser,
        DefaultUseCookiesFile = DefaultUseCookiesFile,
        DefaultCookiesFilePath = DefaultCookiesFilePath,
        AutoStartQueue = AutoStartQueue,
        AutoFillAddDialogFromClipboard = AutoFillAddDialogFromClipboard,
        StartQueueAfterAddDialogSubmit = StartQueueAfterAddDialogSubmit,
        VerboseLogging = VerboseLogging,
    };

    private void ApplySettings(AppSettings settings)
    {
        UsePythonModule = settings.UsePythonModule;
        YtDlpExecutablePath = settings.YtDlpExecutablePath;
        PythonExecutablePath = settings.PythonExecutablePath;
        FfmpegLocation = settings.FfmpegLocation;
        DefaultOutputDirectory = settings.DefaultOutputDirectory;
        DefaultOutputTemplate = settings.DefaultOutputTemplate;
        DefaultDownloadMode = settings.DefaultDownloadMode;
        DefaultQualityPreset = settings.DefaultQualityPreset;
        DefaultDownloadSubtitles = settings.DefaultDownloadSubtitles;
        DefaultDownloadAutoSubtitles = settings.DefaultDownloadAutoSubtitles;
        DefaultEmbedSubtitles = settings.DefaultEmbedSubtitles;
        DefaultEmbedMetadata = settings.DefaultEmbedMetadata;
        DefaultEmbedThumbnail = settings.DefaultEmbedThumbnail;
        DefaultRetries = settings.DefaultRetries;
        DefaultRateLimit = settings.DefaultRateLimit;
        DefaultProxy = settings.DefaultProxy;
        DefaultUserAgent = settings.DefaultUserAgent;
        DefaultReferer = settings.DefaultReferer;
        DefaultExtraArguments = settings.DefaultExtraArguments;
        DefaultUseDownloadArchive = settings.DefaultUseDownloadArchive;
        DefaultDownloadArchivePath = settings.DefaultDownloadArchivePath;
        DefaultUseCookiesFromBrowser = settings.DefaultUseCookiesFromBrowser;
        DefaultCookiesFromBrowser = settings.DefaultCookiesFromBrowser;
        DefaultUseCookiesFile = settings.DefaultUseCookiesFile;
        DefaultCookiesFilePath = settings.DefaultCookiesFilePath;
        AutoStartQueue = settings.AutoStartQueue;
        AutoFillAddDialogFromClipboard = settings.AutoFillAddDialogFromClipboard;
        StartQueueAfterAddDialogSubmit = settings.StartQueueAfterAddDialogSubmit;
        VerboseLogging = settings.VerboseLogging;
    }
}
