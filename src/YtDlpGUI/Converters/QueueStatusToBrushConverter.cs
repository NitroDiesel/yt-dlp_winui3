using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using YtDlpGUI.Core.Enums;

namespace YtDlpGUI.Converters;

public sealed class QueueStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is QueueItemStatus status
            ? status switch
            {
                QueueItemStatus.Running => new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)),
                QueueItemStatus.Completed => new SolidColorBrush(Color.FromArgb(255, 16, 124, 16)),
                QueueItemStatus.Failed => new SolidColorBrush(Color.FromArgb(255, 196, 43, 28)),
                QueueItemStatus.Canceled => new SolidColorBrush(Color.FromArgb(255, 118, 118, 118)),
                _ => new SolidColorBrush(Color.FromArgb(255, 96, 96, 96)),
            }
            : new SolidColorBrush(Color.FromArgb(255, 96, 96, 96));

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
