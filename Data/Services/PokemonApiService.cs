using System.Text.Json;

namespace PokeCollection.Services;

public class PokemonApiService
{
    private readonly HttpClient _http;

    public PokemonApiService(HttpClient http) => _http = http;

    // ===== Helpers (ficam DENTRO da classe) =====
    
    public static string ExtractFinalImageUrlFromDetails(CardDetailsDto? d)
{
    if (d is null) return "";

    // 1) images.small/large como string
    if (d.images.ValueKind == JsonValueKind.Object)
    {
        if (d.images.TryGetProperty("small", out var small) && small.ValueKind == JsonValueKind.String)
            return NormalizeUrl(small.GetString());

        if (d.images.TryGetProperty("large", out var large) && large.ValueKind == JsonValueKind.String)
            return NormalizeUrl(large.GetString());
    }

    // 2) image como string
    if (d.image.ValueKind == JsonValueKind.String)
        return NormalizeUrl(d.image.GetString());

    // 3) image como objeto asset (fallback genérico)
    // Alguns retornos podem ter algo como { "path": ".../assets/..." } ou similar.
    if (d.image.ValueKind == JsonValueKind.Object)
    {
        // tenta "url"
        if (d.image.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
            return NormalizeUrl(url.GetString());

        // tenta "path"
        if (d.image.TryGetProperty("path", out var path) && path.ValueKind == JsonValueKind.String)
        {
            var p = path.GetString() ?? "";
            // tenta assumir png (se for um caminho de asset sem extensão)
            if (!string.IsNullOrWhiteSpace(p) && !p.EndsWith(".png") && !p.EndsWith(".webp") && !p.EndsWith(".jpg") && !p.EndsWith(".jpeg"))
                p += ".png";

            return NormalizeUrl(p);
        }
    }

    return "";
}

    public async Task<(bool ok, string message, CardDetailsDto? card)> GetCardDetailsAsync(string cardId)
{
    var url = $"https://api.tcgdex.net/v2/pt/cards/{cardId}";

    try
    {
        using var resp = await _http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            return (false, $"Erro card details\nURL: {url}\nHTTP {(int)resp.StatusCode} - {resp.ReasonPhrase}\n\nBODY:\n{body}", null);

        var card = JsonSerializer.Deserialize<CardDetailsDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return (true, "OK", card);
    }
    catch (Exception ex)
    {
        return (false, $"Falha ao buscar detalhes da carta {cardId}: {ex.Message}", null);
    }
}


    private static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        url = url.Trim();

        if (url.StartsWith("//")) return "https:" + url;
        if (url.StartsWith("http://")) return "https://" + url.Substring("http://".Length);

        return url;
    }

    // ===== API calls =====
    public async Task<(bool ok, string message, List<SetDto> sets)> GetSetsAsync()
    {
        var url = "https://api.tcgdex.net/v2/pt/sets";

        try
        {
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, $"Erro na API (TCGdex)\nURL: {url}\nHTTP {(int)resp.StatusCode} - {resp.ReasonPhrase}\n\nBODY:\n{body}", new());

            var sets = JsonSerializer.Deserialize<List<SetDto>>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();

            return (true, $"OK (TCGdex). Sets recebidos: {sets.Count}", sets);
        }
        catch (Exception ex)
        {
            return (false, $"Falha ao chamar TCGdex: {ex.Message}", new());
        }
    }

    public async Task<(bool ok, string message, List<CardListItemDto> cards)> GetCardsBySetAsync(string setId)
    {
        var url = $"https://api.tcgdex.net/v2/pt/sets/{setId}";

        try
        {
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, $"Erro na API (TCGdex)\nURL: {url}\nHTTP {(int)resp.StatusCode} - {resp.ReasonPhrase}\n\nBODY:\n{body}", new());

            var set = JsonSerializer.Deserialize<SetDetailsDto>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var cards = set?.cards ?? new List<CardListItemDto>();
            return (true, $"OK (TCGdex). Cartas recebidas: {cards.Count}", cards);
        }
        catch (Exception ex)
        {
            return (false, $"Falha ao buscar cartas do set {setId}: {ex.Message}", new());
        }
    }

    

    // ===== DTOs =====
    public class SetDto
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string serie { get; set; } = "";
    }

    public class CardDetailsDto
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";

    // A TCGdex pode retornar assets como objeto.
    // Vamos manter cru e extrair depois.
    public JsonElement image { get; set; }
    public JsonElement images { get; set; }
}

    public class SetDetailsDto
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public JsonElement serie { get; set; }
        public List<CardListItemDto> cards { get; set; } = new();
    }

    public class CardListItemDto
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string localId { get; set; } = "";
        public string? rarity { get; set; }

        public JsonElement image { get; set; }
        public JsonElement images { get; set; }
    }

    
}