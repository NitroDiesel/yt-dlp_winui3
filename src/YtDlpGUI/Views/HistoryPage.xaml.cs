using YtDlpGUI.ViewModels;

namespace YtDlpGUI.Views
{
    public sealed partial class HistoryPage : Page
    {
        public HistoryPage()
        {
            InitializeComponent();
            DataContext = App.GetService<HistoryViewModel>();
        }
    }
}
