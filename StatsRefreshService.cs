using Microsoft.Extensions.Options;

namespace PollardSuttonSite;

/// <summary>Pulls fresh stats from ESPN at startup and then every <see cref="StatsOptions.RefreshHours"/>.</summary>
public class StatsRefreshService : BackgroundService
{
    private readonly EspnStatsClient _espn;
    private readonly StatsStore _store;
    private readonly StatsOptions _options;
    private readonly ILogger<StatsRefreshService> _logger;

    public StatsRefreshService(
        EspnStatsClient espn,
        StatsStore store,
        IOptions<StatsOptions> options,
        ILogger<StatsRefreshService> logger)
    {
        _espn = espn;
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.RefreshHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RefreshAsync(stoppingToken);
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            var season = _options.ResolveSeason();
            var players = new List<PlayerStats>();
            foreach (var player in _options.Players)
                players.Add(await _espn.GetPlayerAsync(player, season, ct));

            await _store.SaveAsync(new StatsSnapshot(DateTime.UtcNow, season, players), ct);
            _logger.LogInformation("Stats refreshed for season {Season}", season);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // Keep serving the last good snapshot; try again next tick.
            _logger.LogError(ex, "Stats refresh failed");
        }
    }
}
