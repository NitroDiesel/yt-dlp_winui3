using YtDlpGUI.ViewModels;

namespace YtDlpGUI.Views
{
    public sealed partial class AddDownloadDialog : ContentDialog
    {
        private readonly AddDownloadDialogViewModel _viewModel;

        public AddDownloadDialog(AddDownloadDialogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void OnDialogLoaded(object sender, RoutedEventArgs e)
        {
            SizeChanged += OnDialogSizeChanged;
            CenterInRoot();
        }

        private void OnDialogSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterInRoot();
        }

        private void CenterInRoot()
        {
            if (XamlRoot is null || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            var rootSize = XamlRoot.Size;
            if (rootSize.Width <= 0 || rootSize.Height <= 0)
            {
                return;
            }

            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(
                Math.Max(0, (rootSize.Width - ActualWidth) / 2),
                Math.Max(0, (rootSize.Height - ActualHeight) / 2),
                0,
                0);
        }

        private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                var ok = await _viewModel.SubmitAsync().ConfigureAwait(true);
                args.Cancel = !ok;
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
