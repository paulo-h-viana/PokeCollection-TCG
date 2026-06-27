using PokeCollection.Data;
using Microsoft.EntityFrameworkCore;
using PokeCollection.Data.Services;
using Velopack;

// Inicializa o Velopack para lidar com instalação/atualização antes de subir a UI
VelopackApp.Build().Run();

Application.SetHighDpiMode(HighDpiMode.SystemAware);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var splash = new SplashScreen();
splash.Show();

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
builder.Services.AddScoped<UserCollectionService>();
builder.Services.AddSingleton<WindowService>();
builder.Services.AddSingleton<UpdateService>();
builder.Services.AddSingleton(new BackupService(dbPath, appDataFolder));

builder.WebHost.UseUrls("http://localhost:5123");

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Bancos criados com EnsureCreated() não têm __EFMigrationsHistory.
    // Detectamos esse caso e marcamos a migration inicial como já aplicada
    // para que Migrate() não tente recriar tabelas que já existem.
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();

    long hasMigrationsTable;
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
        hasMigrationsTable = (long)(await cmd.ExecuteScalarAsync())!;
    }

    if (hasMigrationsTable == 0)
    {
        long hasSetsTable;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Sets'";
            hasSetsTable = (long)(await cmd.ExecuteScalarAsync())!;
        }

        if (hasSetsTable > 0)
        {
            // DB legado: cria a tabela de histórico e registra a migration inicial como aplicada
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE __EFMigrationsHistory (MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY, ProductVersion TEXT NOT NULL)";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = "INSERT INTO __EFMigrationsHistory VALUES ('20260603220600_InitialCreate', '8.0.0')";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    await conn.CloseAsync();
    await db.Database.MigrateAsync();

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
var backupService = app.Services.GetRequiredService<BackupService>();

var uiThread = new Thread(() =>
{
    Application.Run(new MainForm("http://localhost:5123", windowService, backupService, splash));
});
uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();
uiThread.Join();

await app.StopAsync();
