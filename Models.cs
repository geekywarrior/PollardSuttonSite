namespace PollardSuttonSite;

public record GameLine(
    int Week,
    DateTime Date,
    string Opponent,
    bool Away,
    string? Result,
    string? Score,
    int RushAtt,
    int RushYds,
    int RushTd,
    int Rec,
    int Tgt,
    int RecYds,
    int RecTd,
    int FumblesLost,
    double Points);

public record PlayerStats(
    string Key,
    string Name,
    string Position,
    string Team,
    IReadOnlyList<GameLine> Games)
{
    public double TotalPoints => Math.Round(Games.Sum(g => g.Points), 1);
    public double PointsPerGame => Games.Count == 0 ? 0 : Math.Round(Games.Sum(g => g.Points) / Games.Count, 1);
    public int RushAtt => Games.Sum(g => g.RushAtt);
    public int RushYds => Games.Sum(g => g.RushYds);
    public int RushTd => Games.Sum(g => g.RushTd);
    public int Rec => Games.Sum(g => g.Rec);
    public int Tgt => Games.Sum(g => g.Tgt);
    public int RecYds => Games.Sum(g => g.RecYds);
    public int RecTd => Games.Sum(g => g.RecTd);
}

public record StatsSnapshot(DateTime UpdatedUtc, int Season, IReadOnlyList<PlayerStats> Players);
