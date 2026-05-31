using Microsoft.Extensions.DependencyInjection;
using YtDlpGUI.ViewModels;
using YtDlpGUI.Views;

namespace YtDlpGUI.Services;

public sealed class AddDownloadDialogService(IServiceProvider serviceProvider) : IAddDownloadDialogService
{
    public async Task<bool> ShowAsync(XamlRoot xamlRoot, string? seedUrl = null, CancellationToken cancellationToken = default)
    {
        var viewModel = ActivatorUtilities.CreateInstance<AddDownloadDialogViewModel>(serviceProvider);
        await viewModel.InitializeForOpenAsync(seedUrl, cancellationToken).ConfigureAwait(true);

        var hostRoot = (App.MainWindow?.Content as FrameworkElement)?.XamlRoot ?? xamlRoot;
        var dialog = new AddDownloadDialog(viewModel)
        {
            XamlRoot = hostRoot,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        var result = await dialog.ShowAsync(ContentDialogPlacement.InPlace);
        return result == ContentDialogResult.Primary && viewModel.AddedCount > 0;
    }
}
