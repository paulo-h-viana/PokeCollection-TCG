using PokeCollection.Data;
using Microsoft.EntityFrameworkCore;
using PokeCollection.Data.Services;
using Velopack;

// Inicializa o Velopack para lidar com instalação/atualização antes de subir a UI
VelopackApp.Build().Run();

var builder = WebApplication.CreateBuilder(args);

// Aponta o banco de dados para a pasta persistente do usuário (sobrevive a atualizações)
var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PokeCollection");
Directory.CreateDirectory(appDataFolder);
var dbPath = Path.Combine(appDataFolder, "poke_collection.db");

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddHttpClient<PokemonApiService>();
builder.Services.AddSingleton<WindowService>();
builder.Services.AddSingleton<UpdateService>();

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

    try { db.Database.ExecuteSqlRaw("ALTER TABLE Cards ADD COLUMN AcquiredAt TEXT NULL"); }
    catch { }

    var api = scope.ServiceProvider.GetRequiredService<PokemonApiService>();
    var (ok, _, apiSets) = await api.GetSetsAsync();

    if (ok)
    {
        var existingIds = db.Sets.Select(s => s.ExternalId).ToHashSet();
        foreach (var s in apiSets)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(s.id, @"^[a-zA-Z0-9\-\.]+$"))
                continue;

            if (existingIds.Contains(s.id)) continue;

            db.Sets.Add(new PokeCollection.Data.Models.PokemonSet
            {
                ExternalId = s.id,
                Name = s.name,
                Series = s.serie,
                TotalCards = s.cardCount.official,
                Symbol = s.symbol,
                Logo = s.logo
            });
        }
        await db.SaveChangesAsync();
    }
}

await app.StartAsync();

var windowService = app.Services.GetRequiredService<WindowService>();

var uiThread = new Thread(() =>
{
    Application.SetHighDpiMode(HighDpiMode.SystemAware);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new MainForm("http://localhost:5123", windowService));
});
uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();
uiThread.Join();

await app.StopAsync();
