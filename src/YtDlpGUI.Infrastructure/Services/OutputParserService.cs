using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Infrastructure.Services;

public sealed partial class OutputParserService : IOutputParserService
{
    private const string ProgressPrefix = "__YTDLP_PROGRESS__";
    private const string PostProcessPrefix = "__YTDLP_POSTPROCESS__";
    private const string InfoBeforePrefix = "__YTDLP_INFO_BEFORE__";
    private const string InfoAfterPrefix = "__YTDLP_INFO_AFTER__";

    public OutputParseResult Parse(string line, bool isStandardError)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new OutputParseResult(OutputLineType.Log, line ?? string.Empty, Message: string.Empty);
        }

        if (line.StartsWith(ProgressPrefix, StringComparison.Ordinal))
        {
            return ParseProgressLine(line, ProgressPrefix);
        }

        if (line.StartsWith(PostProcessPrefix, StringComparison.Ordinal))
        {
            return ParseProgressLine(line, PostProcessPrefix);
        }

        if (line.StartsWith(InfoBeforePrefix, StringComparison.Ordinal))
        {
            return ParseInfoBefore(line);
        }

        if (line.StartsWith(InfoAfterPrefix, StringComparison.Ordinal))
        {
            return ParseInfoAfter(line);
        }

        if (line.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[warning]", StringComparison.OrdinalIgnoreCase))
        {
            return new OutputParseResult(OutputLineType.Warning, line, Message: line);
        }

        if (line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[download] Got error:", StringComparison.OrdinalIgnoreCase))
        {
            return new OutputParseResult(OutputLineType.Error, line, Message: line);
        }

        var destinationMatch = DestinationRegex().Match(line);
        if (destinationMatch.Success)
        {
            var path = destinationMatch.Groups["path"].Value;
            return new OutputParseResult(OutputLineType.Destination, line, OutputPath: path, Message: line);
        }

        var progressMatch = HumanProgressRegex().Match(line);
        if (progressMatch.Success)
        {
            var percent = ParseNullableDouble(progressMatch.Groups["percent"].Value);
            var total = ParseSizeText(progressMatch.Groups["total"].Value);
            var speed = ParseSpeedText(progressMatch.Groups["speed"].Value);
            var etaText = progressMatch.Groups["eta"].Value;
            var progress = new DownloadProgressSnapshot
            {
                Status = "downloading",
                Percent = percent,
                TotalBytes = total,
                SpeedBytesPerSecond = speed,
                SpeedText = progressMatch.Groups["speed"].Value,
                Eta = ParseEta(etaText),
                EtaText = etaText,
            };
            return new OutputParseResult(OutputLineType.Progress, line, Progress: progress, Message: line);
        }

        return new OutputParseResult(OutputLineType.Log, line, Message: line);
    }

    private static OutputParseResult ParseProgressLine(string line, string prefix)
    {
        var json = line[prefix.Length..];
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var downloadedBytes = GetNullableInt64(root, "downloaded_bytes");
            var totalBytes = GetNullableInt64(root, "total_bytes");
            var totalBytesEstimate = GetNullableInt64(root, "total_bytes_estimate");
            var percent = GetNullableDouble(root, "_percent")
                ?? ParseNullableDouble(GetString(root, "_percent_str"));
            if (percent is null && downloadedBytes.HasValue)
            {
                var total = totalBytes ?? totalBytesEstimate;
                if (total > 0)
                {
                    percent = downloadedBytes.Value * 100d / total.Value;
                }
            }

            var etaSeconds = GetNullableInt64(root, "eta");
            var speed = GetNullableDouble(root, "speed");
            var progress = new DownloadProgressSnapshot
            {
                Status = GetString(root, "status") ?? string.Empty,
                Percent = percent,
                DownloadedBytes = downloadedBytes,
                TotalBytes = totalBytes,
                TotalBytesEstimate = totalBytesEstimate,
                SpeedBytesPerSecond = speed,
                SpeedText = GetString(root, "_speed_str") ?? FormatSpeed(speed),
                Eta = etaSeconds.HasValue ? TimeSpan.FromSeconds(etaSeconds.Value) : null,
                EtaText = GetString(root, "_eta_str") ?? FormatEta(etaSeconds),
                CurrentFile = GetString(root, "filename") ?? string.Empty,
                CurrentTitle = GetNestedString(root, "info_dict", "title") ?? string.Empty,
            };

            return new OutputParseResult(OutputLineType.Progress, line, Progress: progress, Message: line);
        }
        catch (JsonException)
        {
            return new OutputParseResult(OutputLineType.Log, line, Message: line);
        }
    }

    private static OutputParseResult ParseInfoBefore(string line)
    {
        var json = line[InfoBeforePrefix.Length..];
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new OutputParseResult(
                OutputLineType.InfoBefore,
                line,
                Message: line,
                ItemId: GetString(root, "id"),
                Title: GetString(root, "title"),
                Url: GetString(root, "webpage_url"));
        }
        catch (JsonException)
        {
            return new OutputParseResult(OutputLineType.Log, line, Message: line);
        }
    }

    private static OutputParseResult ParseInfoAfter(string line)
    {
        var json = line[InfoAfterPrefix.Length..];
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new OutputParseResult(
                OutputLineType.InfoAfter,
                line,
                Message: line,
                ItemId: GetString(root, "id"),
                Title: GetString(root, "title"),
                OutputPath: GetString(root, "filepath") ?? GetString(root, "filename"));
        }
        catch (JsonException)
        {
            return new OutputParseResult(OutputLineType.Log, line, Message: line);
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static string? GetNestedString(JsonElement element, string objectPropertyName, string nestedPropertyName)
    {
        if (!element.TryGetProperty(objectPropertyName, out var parent) || parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(parent, nestedPropertyName);
    }

    private static long? GetNullableInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt64(out var value) => value,
            JsonValueKind.String when long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static double? GetNullableDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out var value) => value,
            JsonValueKind.String => ParseNullableDouble(property.GetString()),
            _ => null,
        };
    }

    private static double? ParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("%", string.Empty, StringComparison.Ordinal)
            .Trim();

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static TimeSpan? ParseEta(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("NA", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string FormatSpeed(double? speed)
    {
        if (!speed.HasValue)
        {
            return string.Empty;
        }

        return $"{speed.Value / 1024d / 1024d:0.##} MiB/s";
    }

    private static string FormatEta(long? etaSeconds)
    {
        if (!etaSeconds.HasValue)
        {
            return string.Empty;
        }

        return TimeSpan.FromSeconds(etaSeconds.Value).ToString();
    }

    private static long? ParseSizeText(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return null;
        }

        var match = SizeRegex().Match(size);
        if (!match.Success)
        {
            return null;
        }

        if (!double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var unit = match.Groups["unit"].Value.ToUpperInvariant();
        var multiplier = unit switch
        {
            "B" => 1d,
            "KIB" or "KB" => 1024d,
            "MIB" or "MB" => 1024d * 1024d,
            "GIB" or "GB" => 1024d * 1024d * 1024d,
            "TIB" or "TB" => 1024d * 1024d * 1024d * 1024d,
            _ => 1d,
        };

        return (long)(value * multiplier);
    }

    private static double? ParseSpeedText(string speed)
    {
        if (string.IsNullOrWhiteSpace(speed))
        {
            return null;
        }

        var normalized = speed.Replace("/s", string.Empty, StringComparison.OrdinalIgnoreCase);
        var size = ParseSizeText(normalized);
        return size;
    }

    [GeneratedRegex("^\\[download\\]\\s+Destination:\\s+(?<path>.+)$", RegexOptions.Compiled)]
    private static partial Regex DestinationRegex();

    [GeneratedRegex("^\\[download\\]\\s+(?<percent>\\d{1,3}(?:\\.\\d+)?)%\\s+of\\s+(?:~)?(?<total>\\S+)\\s+at\\s+(?<speed>\\S+)\\s+ETA\\s+(?<eta>[\\d:]+)", RegexOptions.Compiled)]
    private static partial Regex HumanProgressRegex();

    [GeneratedRegex("^(?<value>[0-9]+(?:\\.[0-9]+)?)(?<unit>[KMGTP]?i?B)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SizeRegex();
}
