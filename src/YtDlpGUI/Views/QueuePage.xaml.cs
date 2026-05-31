using YtDlpGUI.ViewModels;
using YtDlpGUI.Services;

namespace YtDlpGUI.Views
{
    public sealed partial class QueuePage : Page
    {
        private readonly IAddDownloadDialogService _addDownloadDialogService;

        public QueuePage()
        {
            InitializeComponent();
            _addDownloadDialogService = App.GetService<IAddDownloadDialogService>();
            DataContext = App.GetService<QueueViewModel>();
        }

        private async void OnAddDownloadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await _addDownloadDialogService.ShowAsync(XamlRoot);
            }
            catch
            {
                // Keep queue page stable if the dialog fails to open.
            }
        }
    }
}
