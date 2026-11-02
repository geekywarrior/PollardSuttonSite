namespace PollardSuttonSite;

public class StatsOptions
{
    /// <summary>Hours between ESPN refreshes.</summary>
    public double RefreshHours { get; set; } = 12;

    /// <summary>Season to pull; null means the current NFL season.</summary>
    public int? Season { get; set; }

    /// <summary>Where the last good snapshot is cached so restarts and ESPN outages don't blank the site.</summary>
    public string CachePath { get; set; } = "data/stats.json";

    public List<PlayerOptions> Players { get; set; } = new();

    /// <summary>The NFL season is named for the year it starts in, and runs into February.</summary>
    public int ResolveSeason()
    {
        if (Season is { } season) return season;
        var now = DateTime.UtcNow;
        return now.Month >= 3 ? now.Year : now.Year - 1;
    }
}

public class PlayerOptions
{
    public string Key { get; set; } = "";
    public string EspnId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public string Team { get; set; } = "";
}
