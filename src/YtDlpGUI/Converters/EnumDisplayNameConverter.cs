using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Data;

namespace YtDlpGUI.Converters;

public sealed class EnumDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var raw = value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        // Keep common quality tokens compact: UpTo1080p -> Up To 1080p
        var qualityMatch = Regex.Match(raw, "^UpTo(\\d+)p$", RegexOptions.IgnoreCase);
        if (qualityMatch.Success)
        {
            return $"Up To {qualityMatch.Groups[1].Value}p";
        }

        // UpTo1080p -> Up To 1080p, AudioOnly -> Audio Only.
        var withWordBreaks = Regex.Replace(raw, "(?<=[a-z])(?=[A-Z])", " ");
        withWordBreaks = Regex.Replace(withWordBreaks, "(?<=[A-Za-z])(?=[0-9])", " ");
        withWordBreaks = Regex.Replace(withWordBreaks, "(?<=[0-9])(?=[A-Za-z])", " ");
        withWordBreaks = Regex.Replace(withWordBreaks, "(?<=[0-9])\\s+p\\b", "p", RegexOptions.IgnoreCase);
        return withWordBreaks;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
