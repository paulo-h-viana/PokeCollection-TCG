using PokeCollection.Data;
using Microsoft.EntityFrameworkCore;
using PokeCollection.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=poke_collection.db"));
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddHttpClient<PokemonApiService>();

builder.WebHost.UseUrls("http://localhost:5123");

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var api = scope.ServiceProvider.GetRequiredService<PokemonApiService>();
    var(ok, _, apiSets) = await api.GetSetsAsync();

    if (ok)
    {
        var existingIds = db.Sets.Select(s => s.ExternalId).ToHashSet();
        foreach(var s in apiSets)
        {
            if(existingIds.Contains(s.id)) continue;
            db.Sets.Add(new PokeCollection.Data.Models.PokemonSet
            {
                ExternalId = s.id,
                Name = s.name,
                Series = s.serie,
                TotalCards = s.cardCount.total,
                Symbol = s.symbol,
                Logo = s.logo
            });
        }
        await db.SaveChangesAsync();
    }
}

await app.StartAsync();

var uiThread = new Thread(() =>
{
    Application.SetHighDpiMode(HighDpiMode.SystemAware);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new MainForm("http://localhost:5123"));
});
uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();
uiThread.Join();

await app.StopAsync();
