using Microsoft.EntityFrameworkCore;
using PokeCollection.Data.Models;

namespace PokeCollection.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PokemonSet> Sets => Set<PokemonSet>();
    public DbSet<PokemonCard> Cards => Set<PokemonCard>();
    public DbSet<CardVariant> CardVariants => Set<CardVariant>();
    public DbSet<UserCollection> UserCollections => Set<UserCollection>();
    public DbSet<UserCollectionCard> UserCollectionCards => Set<UserCollectionCard>();

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

        modelBuilder.Entity<CardVariant>()
            .HasOne(v => v.Card)
            .WithMany(c => c.Variants)
            .HasForeignKey(v => v.PokemonCardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CardVariant>()
            .HasIndex(v => new { v.PokemonCardId, v.Type })
            .IsUnique();

        modelBuilder.Entity<UserCollectionCard>()
            .HasOne(c => c.Collection)
            .WithMany(uc => uc.Cards)
            .HasForeignKey(c => c.UserCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserCollectionCard>()
            .HasIndex(c => new { c.UserCollectionId, c.ExternalId })
            .IsUnique();
    }
}