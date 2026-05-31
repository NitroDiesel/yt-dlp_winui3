using System.Text.Json;
using YtDlpGUI.Core.Interfaces;
using YtDlpGUI.Core.Models;

namespace YtDlpGUI.Infrastructure.Services;

public sealed class JsonHistoryService : IHistoryService
{
    private const int MaxItems = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _historyFilePath;

    public JsonHistoryService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YtDlpGUI");
        Directory.CreateDirectory(directory);
        _historyFilePath = Path.Combine(directory, "history.json");
    }

    public async Task<IReadOnlyList<HistoryEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                return [];
            }

            await using var stream = File.OpenRead(_historyFilePath);
            var entries = await JsonSerializer.DeserializeAsync<List<HistoryEntry>>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            return entries ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AppendAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<HistoryEntry> entries;
            if (!File.Exists(_historyFilePath))
            {
                entries = [];
            }
            else
            {
                await using var readStream = File.OpenRead(_historyFilePath);
                entries = await JsonSerializer.DeserializeAsync<List<HistoryEntry>>(readStream, SerializerOptions, cancellationToken).ConfigureAwait(false) ?? [];
            }

            entries.Insert(0, entry);
            if (entries.Count > MaxItems)
            {
                entries = entries[..MaxItems];
            }

            await using var writeStream = File.Create(_historyFilePath);
            await JsonSerializer.SerializeAsync(writeStream, entries, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = File.Create(_historyFilePath);
            await JsonSerializer.SerializeAsync(stream, Array.Empty<HistoryEntry>(), SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
