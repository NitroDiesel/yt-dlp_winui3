using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Infrastructure.Services;
using YtDlpGUI.Services;
using YtDlpGUI.ViewModels;

namespace YtDlpGUI
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            InitializeComponent();

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<ISettingsService, JsonSettingsService>();
                    services.AddSingleton<IHistoryService, JsonHistoryService>();
                    services.AddSingleton<IProcessService, ProcessService>();
                    services.AddSingleton<IOutputParserService, OutputParserService>();
                    services.AddSingleton<IYtDlpCommandBuilder, YtDlpCommandBuilder>();
                    services.AddSingleton<IYtDlpService, YtDlpService>();
                    services.AddSingleton<IQueueService, QueueService>();

                    services.AddSingleton<IFileDialogService, FileDialogService>();
                    services.AddSingleton<ILauncherService, LauncherService>();
                    services.AddSingleton<IClipboardService, ClipboardService>();
                    services.AddSingleton<IAddDownloadDialogService, AddDownloadDialogService>();
                    services.AddSingleton<IUiDispatcher, UiDispatcher>();

                    services.AddSingleton<ShellViewModel>();
                    services.AddSingleton<DownloadComposerViewModel>();
                    services.AddSingleton<QueueViewModel>();
                    services.AddSingleton<HistoryViewModel>();
                    services.AddSingleton<SettingsViewModel>();
                    services.AddTransient<AddDownloadDialogViewModel>();

                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        public static Window? MainWindow { get; private set; }

        public static T GetService<T>() where T : notnull
            => ((App)Current)._host.Services.GetRequiredService<T>();

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await _host.StartAsync();

            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Activate();
        }
    }
}
