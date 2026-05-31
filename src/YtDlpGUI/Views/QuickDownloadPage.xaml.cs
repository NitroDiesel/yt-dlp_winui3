using YtDlpGUI.ViewModels;
using YtDlpGUI.Services;

namespace YtDlpGUI.Views
{
    public sealed partial class QuickDownloadPage : Page
    {
        private readonly IAddDownloadDialogService _addDownloadDialogService;

        public QuickDownloadPage()
        {
            InitializeComponent();
            _addDownloadDialogService = App.GetService<IAddDownloadDialogService>();
            DataContext = App.GetService<DownloadComposerViewModel>();
        }

        private async void OnAddDownloadClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DownloadComposerViewModel composer)
            {
                return;
            }

            try
            {
                var added = await _addDownloadDialogService.ShowAsync(XamlRoot, composer.Url);
                if (added)
                {
                    composer.StatusMessage = "Added new download(s) to queue.";
                }
            }
            catch (Exception ex)
            {
                composer.StatusMessage = $"Unable to open Add Download dialog: {ex.Message}";
            }
        }
    }
}
