using YtDlpGUI.ViewModels;

namespace YtDlpGUI.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = App.GetService<SettingsViewModel>();
        }
    }
}
