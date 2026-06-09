using Microsoft.EntityFrameworkCore;
using PokeCollection.Data.Models;

namespace PokeCollection.Data.Services;

public class UserCollectionService
{
    private readonly AppDbContext _db;
    private readonly PokemonApiService _api;

    public UserCollectionService(AppDbContext db, PokemonApiService api)
    {
        _db = db;
        _api = api;
    }

    public async Task<List<UserCollection>> GetCollectionsAsync() =>
        await _db.UserCollections.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<Dictionary<int, (int total, int owned)>> GetStatsAsync()
    {
        var grouped = await _db.UserCollectionCards.AsNoTracking()
            .GroupBy(c => c.UserCollectionId)
            .Select(g => new { Id = g.Key, Total = g.Count(), Owned = g.Count(x => x.Owned) })
            .ToListAsync();

        return grouped.ToDictionary(x => x.Id, x => (x.Total, x.Owned));
    }

    public async Task<UserCollection?> GetAsync(int id) =>
        await _db.UserCollections.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<(bool ok, string message, UserCollection? collection)> CreateAsync(string name)
    {
        var trimmed = name?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(trimmed))
            return (false, "Informe um nome para a coleção.", null);

        try
        {
            var collection = new UserCollection { Name = trimmed, CreatedAt = DateTime.UtcNow };
            _db.UserCollections.Add(collection);
            await _db.SaveChangesAsync();
            return (true, "OK", collection);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao criar coleção: {ex.Message}", null);
        }
    }

    public async Task<(bool ok, string message)> DeleteAsync(int id)
    {
        try
        {
            var collection = await _db.UserCollections.FirstOrDefaultAsync(c => c.Id == id);
            if (collection is null) return (false, "Coleção não encontrada.");

            _db.UserCollections.Remove(collection);
            await _db.SaveChangesAsync();
            return (true, "OK");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao excluir coleção: {ex.Message}");
        }
    }

    public async Task<List<UserCollectionCard>> GetCardsAsync(int collectionId)
    {
        var cards = await _db.UserCollectionCards.AsNoTracking()
            .Where(c => c.UserCollectionId == collectionId)
            .ToListAsync();

        return cards
            .OrderBy(c => int.TryParse(c.Number, out var n) ? n : int.MaxValue)
            .ThenBy(c => c.Number)
            .ThenBy(c => c.Name)
            .ToList();
    }

    public async Task<Dictionary<string, string>> GetSetNamesAsync()
    {
        var sets = await _db.Sets.AsNoTracking()
            .Select(s => new { s.ExternalId, s.Name })
            .ToListAsync();

        return sets.ToDictionary(s => s.ExternalId, s => s.Name);
    }

    public async Task<Dictionary<string, int>> GetSetTotalsAsync()
    {
        var sets = await _db.Sets.AsNoTracking()
            .Select(s => new { s.ExternalId, s.TotalCards })
            .ToListAsync();

        return sets.ToDictionary(s => s.ExternalId, s => s.TotalCards);
    }

    public async Task<Dictionary<string, string>> GetSymbolsForAsync(IEnumerable<string> cardExternalIds)
    {
        var setIds = cardExternalIds.Select(SetExternalIdOf).Distinct().ToList();

        var symbols = await _db.Sets.AsNoTracking()
            .Where(s => setIds.Contains(s.ExternalId) && s.Symbol != "")
            .Select(s => new { s.ExternalId, s.Symbol })
            .ToListAsync();

        return symbols.ToDictionary(s => s.ExternalId, s => s.Symbol);
    }

    public async Task<(bool ok, string message)> AddCardAsync(int collectionId, PokemonApiService.CardSearchItemDto item)
    {
        if (string.IsNullOrWhiteSpace(item.id))
            return (false, "Carta inválida.");

        try
        {
            var exists = await _db.UserCollectionCards
                .AnyAsync(c => c.UserCollectionId == collectionId && c.ExternalId == item.id);
            if (exists) return (true, "Carta já está na coleção.");

            var image = PokemonApiService.ExtractImageFromSearchItem(item);
            if (string.IsNullOrWhiteSpace(image))
            {
                var total = await GetOfficialTotalAsync(item.id);
                var (fbOk, _, fbUrl) = await _api.GetFallbackImageAsync(item.name, item.localId, total);
                if (fbOk) image = fbUrl;
            }

            _db.UserCollectionCards.Add(new UserCollectionCard
            {
                UserCollectionId = collectionId,
                ExternalId       = item.id,
                Name             = item.name ?? "",
                Number           = item.localId ?? "",
                ImageSmallUrl    = image,
                Owned            = false
            });
            await _db.SaveChangesAsync();
            return (true, "OK");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao adicionar carta: {ex.Message}");
        }
    }

    public async Task<(bool ok, string message)> RemoveCardAsync(int cardId)
    {
        try
        {
            var card = await _db.UserCollectionCards.FirstOrDefaultAsync(c => c.Id == cardId);
            if (card is null) return (false, "Carta não encontrada.");

            _db.UserCollectionCards.Remove(card);
            await _db.SaveChangesAsync();
            return (true, "OK");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao remover carta: {ex.Message}");
        }
    }

    public async Task<(bool ok, string message, DateTime? acquiredAt)> SetOwnedAsync(int cardId, bool owned)
    {
        try
        {
            var card = await _db.UserCollectionCards.FirstOrDefaultAsync(c => c.Id == cardId);
            if (card is null) return (false, "Carta não encontrada.", null);

            card.Owned = owned;
            card.AcquiredAt = owned ? DateTime.UtcNow : null;
            await _db.SaveChangesAsync();
            return (true, "OK", card.AcquiredAt);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao atualizar carta: {ex.Message}", null);
        }
    }

    public async Task<(bool ok, string message, int updated)> BackfillImagesAsync(int collectionId)
    {
        try
        {
            var missing = await _db.UserCollectionCards
                .Where(c => c.UserCollectionId == collectionId && (c.ImageSmallUrl == null || c.ImageSmallUrl == ""))
                .ToListAsync();

            if (missing.Count == 0) return (true, "OK", 0);

            int updated = 0;
            foreach (var card in missing)
            {
                var total = await GetOfficialTotalAsync(card.ExternalId);
                var (fbOk, _, fbUrl) = await _api.GetFallbackImageAsync(card.Name, card.Number, total);
                if (fbOk && !string.IsNullOrWhiteSpace(fbUrl))
                {
                    card.ImageSmallUrl = fbUrl;
                    updated++;
                }
            }

            if (updated > 0) await _db.SaveChangesAsync();
            return (true, "OK", updated);
        }
        catch (Exception ex)
        {
            return (false, $"Erro no backfill: {ex.Message}", 0);
        }
    }

    private async Task<int> GetOfficialTotalAsync(string cardExternalId)
    {
        var setId = SetExternalIdOf(cardExternalId);
        return await _db.Sets.AsNoTracking()
            .Where(s => s.ExternalId == setId)
            .Select(s => s.TotalCards)
            .FirstOrDefaultAsync();
    }

    public static string SetExternalIdOf(string cardExternalId)
    {
        var i = cardExternalId.LastIndexOf('-');
        return i > 0 ? cardExternalId.Substring(0, i) : cardExternalId;
    }
}
