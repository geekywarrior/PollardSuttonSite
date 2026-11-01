// Builds the static site: fetches stats from ESPN and bakes them into index.html.
// Usage: dotnet run --project Generator [outputDir]   (default: ./site)

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PollardSuttonSite;

const string Placeholder = "<script id=\"stats-data\" type=\"application/json\"></script>";

var root = FindProjectRoot();
var outDir = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine(root, "site"));

var config = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(root, "appsettings.json"))
    .Build();
var options = config.GetSection("Stats").Get<StatsOptions>() ?? new StatsOptions();

var services = new ServiceCollection();
services.AddHttpClient(EspnStatsClient.HttpClientName, c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("PollardSuttonSite/1.0");
});
services.AddSingleton<EspnStatsClient>();
using var provider = services.BuildServiceProvider();
var espn = provider.GetRequiredService<EspnStatsClient>();

var season = options.ResolveSeason();
var players = new List<PlayerStats>();
foreach (var player in options.Players)
    players.Add(await espn.GetPlayerAsync(player, season, CancellationToken.None));

var snapshot = new StatsSnapshot(DateTime.UtcNow, season, players);
// The default encoder escapes "<", so the JSON can't close the script tag early.
var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web));

var wwwroot = Path.Combine(root, "wwwroot");
var html = await File.ReadAllTextAsync(Path.Combine(wwwroot, "index.html"));
if (!html.Contains(Placeholder))
    throw new InvalidOperationException("index.html is missing the stats-data placeholder.");
html = html.Replace(Placeholder, $"<script id=\"stats-data\" type=\"application/json\">{json}</script>");

Directory.CreateDirectory(outDir);
await File.WriteAllTextAsync(Path.Combine(outDir, "index.html"), html);
await File.WriteAllTextAsync(Path.Combine(outDir, ".nojekyll"), "");
CopyDirectory(Path.Combine(wwwroot, "theme", "dist"), Path.Combine(outDir, "theme", "dist"));

Console.WriteLine($"Wrote {outDir} ({season} season, {players.Sum(p => p.Games.Count)} games)");

static string FindProjectRoot()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PollardSuttonSite.csproj")))
                return dir.FullName;
    throw new DirectoryNotFoundException("Could not find PollardSuttonSite.csproj above the current directory.");
}

static void CopyDirectory(string source, string target)
{
    Directory.CreateDirectory(target);
    foreach (var file in Directory.GetFiles(source))
        File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
    foreach (var dir in Directory.GetDirectories(source))
        CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
}
