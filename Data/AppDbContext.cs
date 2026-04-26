using Microsoft.EntityFrameworkCore;
using PokeCollection.Data.Models;

namespace PokeCollection.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PokemonSet> Sets => Set<PokemonSet>();
    public DbSet<PokemonCard> Cards => Set<PokemonCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PokemonSet>()
            .HasIndex(x => x.ExternalId)
            .IsUnique();

        modelBuilder.Entity<PokemonCard>()
            .HasIndex(x => x.ExternalId)
            .IsUnique();

        modelBuilder.Entity<PokemonCard>()
            .HasOne(c => c.Set)
            .WithMany(s => s.Cards)
            .HasForeignKey(c => c.PokemonSetId);
    }
}