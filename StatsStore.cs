using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PollardSuttonSite;

/// <summary>Holds the latest snapshot in memory and mirrors it to disk.</summary>
public class StatsStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _path;
    private readonly ILogger<StatsStore> _logger;
    private volatile StatsSnapshot? _current;

    public StatsStore(IOptions<StatsOptions> options, IHostEnvironment env, ILogger<StatsStore> logger)
    {
        _logger = logger;
        _path = Path.IsPathRooted(options.Value.CachePath)
            ? options.Value.CachePath
            : Path.Combine(env.ContentRootPath, options.Value.CachePath);
        Load();
    }

    public StatsSnapshot? Current => _current;

    public async Task SaveAsync(StatsSnapshot snapshot, CancellationToken ct)
    {
        _current = snapshot;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(snapshot, Json), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write stats cache to {Path}", _path);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            _current = JsonSerializer.Deserialize<StatsSnapshot>(File.ReadAllText(_path), Json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read stats cache from {Path}", _path);
        }
    }
}
