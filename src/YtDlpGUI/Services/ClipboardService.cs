using Windows.ApplicationModel.DataTransfer;

namespace YtDlpGUI.Services;

public sealed class ClipboardService : IClipboardService
{
    public async Task<string?> GetTextAsync()
    {
        try
        {
            var content = Clipboard.GetContent();
            if (content is null || !content.Contains(StandardDataFormats.Text))
            {
                return null;
            }

            return await content.GetTextAsync();
        }
        catch
        {
            // Clipboard can be temporarily unavailable if locked by another process.
            return null;
        }
    }

    public Task SetTextAsync(string text)
    {
        var package = new DataPackage();
        package.SetText(text ?? string.Empty);
        Clipboard.SetContent(package);
        return Task.CompletedTask;
    }
}
