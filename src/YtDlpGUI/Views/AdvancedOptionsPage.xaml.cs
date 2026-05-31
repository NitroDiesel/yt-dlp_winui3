using YtDlpGUI.ViewModels;

namespace YtDlpGUI.Views
{
    public sealed partial class AdvancedOptionsPage : Page
    {
        public AdvancedOptionsPage()
        {
            InitializeComponent();
            DataContext = App.GetService<DownloadComposerViewModel>();
        }
    }
}
