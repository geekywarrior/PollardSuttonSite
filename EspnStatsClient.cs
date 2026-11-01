using System.Text.Json;

namespace PollardSuttonSite;

public class EspnStatsClient
{
    public const string HttpClientName = "espn";

    private readonly IHttpClientFactory _httpClientFactory;

    public EspnStatsClient(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<PlayerStats> GetPlayerAsync(PlayerOptions player, int season, CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(HttpClientName);
        var url = $"https://site.web.api.espn.com/apis/common/v3/sports/football/nfl/athletes/{player.EspnId}/gamelog?season={season}";
        await using var stream = await http.GetStreamAsync(url, ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var columns = new Dictionary<string, int>();
        var index = 0;
        foreach (var name in root.GetProperty("names").EnumerateArray())
            columns.TryAdd(name.GetString() ?? "", index++);
            // TryAdd keeps the first occurrence; index still advances for duplicates.

        var games = new List<GameLine>();
        if (root.TryGetProperty("seasonTypes", out var seasonTypes) && root.TryGetProperty("events", out var events))
        {
            foreach (var seasonType in seasonTypes.EnumerateArray())
            {
                var typeName = seasonType.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
                if (typeName == null || !typeName.Contains("Regular Season", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var category in seasonType.GetProperty("categories").EnumerateArray())
                foreach (var ev in category.GetProperty("events").EnumerateArray())
                {
                    var eventId = ev.GetProperty("eventId").GetString()!;
                    if (!events.TryGetProperty(eventId, out var info)) continue;

                    var stats = ev.GetProperty("stats").EnumerateArray().Select(s => s.GetString() ?? "").ToArray();
                    int Stat(string name) => columns.TryGetValue(name, out var i) && i < stats.Length ? ParseInt(stats[i]) : 0;

                    var rushAtt = Stat("rushingAttempts");
                    var rushYds = Stat("rushingYards");
                    var rushTd = Stat("rushingTouchdowns");
                    var rec = Stat("receptions");
                    var tgt = Stat("receivingTargets");
                    var recYds = Stat("receivingYards");
                    var recTd = Stat("receivingTouchdowns");
                    var fumLost = Stat("fumblesLost");

                    games.Add(new GameLine(
                        Week: info.GetProperty("week").GetInt32(),
                        Date: info.TryGetProperty("gameDate", out var gd) && gd.TryGetDateTime(out var date) ? date : default,
                        Opponent: info.GetProperty("opponent").GetProperty("abbreviation").GetString() ?? "?",
                        Away: info.TryGetProperty("atVs", out var atVs) && atVs.GetString() == "@",
                        Result: info.TryGetProperty("gameResult", out var gr) ? gr.GetString() : null,
                        Score: info.TryGetProperty("score", out var sc) ? sc.GetString() : null,
                        RushAtt: rushAtt, RushYds: rushYds, RushTd: rushTd,
                        Rec: rec, Tgt: tgt, RecYds: recYds, RecTd: recTd,
                        FumblesLost: fumLost,
                        Points: HalfPprPoints(rushYds + recYds, rushTd + recTd, rec, fumLost)));
                }
            }
        }

        return new PlayerStats(player.Key, player.Name, player.Position, player.Team,
            games.OrderBy(g => g.Week).ToList());
    }

    /// <summary>0.5 per reception, 0.1 per yard, 6 per TD, -2 per fumble lost. Integer tenths avoid float drift.</summary>
    private static double HalfPprPoints(int yards, int touchdowns, int receptions, int fumblesLost) =>
        (receptions * 5 + yards + touchdowns * 60 - fumblesLost * 20) / 10.0;

    private static int ParseInt(string s) =>
        int.TryParse(s.Replace(",", ""), out var n) ? n : 0; // ESPN uses "-" for no stat
}
