using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;
using YtDlpGUI.Views;

namespace YtDlpGUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetWindowIcon();
            if (RootNavigationView.MenuItems.Count > 0)
            {
                RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
                NavigateTo("quick");
            }
        }

        private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is not NavigationViewItem item || item.Tag is not string tag)
            {
                return;
            }

            NavigateTo(tag);
        }

        private void NavigateTo(string tag)
        {
            var targetType = tag switch
            {
                "quick" => typeof(QuickDownloadPage),
                "advanced" => typeof(AdvancedOptionsPage),
                "queue" => typeof(QueuePage),
                "history" => typeof(HistoryPage),
                "settings" => typeof(SettingsPage),
                _ => typeof(QuickDownloadPage),
            };

            if (ContentFrame.CurrentSourcePageType != targetType)
            {
                ContentFrame.Navigate(targetType, null, new SuppressNavigationTransitionInfo());
            }
        }

        private void SetWindowIcon()
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ytdlpgui.ico");
                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(AppContext.BaseDirectory, "ytdlpgui.ico");
                }

                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
            catch
            {
                // Ignore icon failures and keep window startup stable.
            }
        }
    }
}
