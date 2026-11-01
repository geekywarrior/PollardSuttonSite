using PollardSuttonSite;
using PollardSuttonSite.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<StatsOptions>(builder.Configuration.GetSection("Stats"));
builder.Services.AddHttpClient(EspnStatsClient.HttpClientName, c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("PollardSuttonSite/1.0");
});
builder.Services.AddSingleton<EspnStatsClient>();
builder.Services.AddSingleton<StatsStore>();
builder.Services.AddHostedService<StatsRefreshService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// The stats page is the only page.
app.MapGet("/", (IWebHostEnvironment env) =>
    Results.File(Path.Combine(env.WebRootPath, "index.html"), "text/html"));

app.MapGet("/api/stats", (StatsStore store) =>
    store.Current is { } snapshot ? Results.Ok(snapshot) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
