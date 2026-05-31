using CommunityToolkit.Mvvm.ComponentModel;
using YtDlpGUI.Models;
using YtDlpGUI.Views;

namespace YtDlpGUI.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private string selectedTag = "quick";

    public string AppTitle => "yt-dlp Desktop";

    public IReadOnlyList<NavigationDestination> Destinations { get; } =
    [
        new("quick", "Quick Download", "\uE8A7", typeof(QuickDownloadPage)),
        new("advanced", "Advanced Options", "\uE713", typeof(AdvancedOptionsPage)),
        new("queue", "Queue", "\uE7C3", typeof(QueuePage)),
        new("history", "History", "\uE81C", typeof(HistoryPage)),
        new("settings", "Settings", "\uE713", typeof(SettingsPage)),
    ];
}
