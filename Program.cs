using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}
app.Run();
