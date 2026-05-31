using System.Text.RegularExpressions;

namespace YtDlpGUI.Helpers;

public static partial class UrlInputParser
{
    [GeneratedRegex(@"(?<url>(https?|ftp)://[^\s""'<>]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    public static IReadOnlyList<string> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var matches = UrlRegex().Matches(input);
        if (matches.Count > 0)
        {
            return matches
                .Select(static x => x.Groups["url"].Value.Trim())
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return input
            .Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
