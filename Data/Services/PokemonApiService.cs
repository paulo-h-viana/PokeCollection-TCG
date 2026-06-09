using System.Text.Json;

namespace PokeCollection.Data.Services;

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

    public static readonly string[] KnownVariantTypes =
        { "normal", "reverse", "holo", "firstEdition", "wPromo" };

    public static List<string> ExtractVariantTypesFromDetails(CardDetailsDto? d)
    {
        var result = new List<string>();
        if (d is null || d.variants.ValueKind != JsonValueKind.Object) return result;

        foreach (var type in KnownVariantTypes)
        {
            if (d.variants.TryGetProperty(type, out var value)
                && value.ValueKind == JsonValueKind.True)
            {
                result.Add(type);
            }
        }

        return result;
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

    public async Task<(bool ok, string message, List<CardListItemDto> cards, int officialTotal)> GetCardsBySetAsync(string setId)
    {
        var url = $"https://api.tcgdex.net/v2/pt/sets/{setId}";

        try
        {
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, $"Erro na API (TCGdex)\nURL: {url}\nHTTP {(int)resp.StatusCode} - {resp.ReasonPhrase}\n\nBODY:\n{body}", new(), 0);

            var setDto = JsonSerializer.Deserialize<SetDetailsDto>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var cards = setDto?.cards ?? new List<CardListItemDto>();
            var officialTotal = setDto?.cardCount.official ?? 0;
            return (true, $"OK (TCGdex). Cartas recebidas: {cards.Count}", cards, officialTotal);
        }
        catch (Exception ex)
        {
            return (false, $"Falha ao buscar cartas do set {setId}: {ex.Message}", new(), 0);
        }
    }



    public async Task<(bool ok, string message, List<CardSearchItemDto> cards)> SearchCardsAsync(string query)
    {
        var trimmed = query?.Trim() ?? "";

        string url;
        if (PokeCollection.Data.CardSearch.IsNumberQuery(trimmed, out var numberPart))
            url = $"https://api.tcgdex.net/v2/pt/cards?localId={Uri.EscapeDataString(numberPart)}&pagination:page=1&pagination:itemsPerPage=60";
        else if (trimmed.Length >= 2)
            url = $"https://api.tcgdex.net/v2/pt/cards?name=like:{Uri.EscapeDataString(trimmed)}&pagination:page=1&pagination:itemsPerPage=60";
        else
            return (true, "Digite ao menos 2 caracteres ou um número.", new());

        try
        {
            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, $"Erro na busca (TCGdex)\nURL: {url}\nHTTP {(int)resp.StatusCode} - {resp.ReasonPhrase}", new());

            var cards = JsonSerializer.Deserialize<List<CardSearchItemDto>>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();

            return (true, $"OK. Resultados: {cards.Count}", cards);
        }
        catch (Exception ex)
        {
            return (false, $"Falha ao buscar cartas: {ex.Message}", new());
        }
    }

    public static string ExtractImageFromSearchItem(CardSearchItemDto item)
    {
        if (item.image.ValueKind == JsonValueKind.String)
            return NormalizeUrl(item.image.GetString());
        return "";
    }

    public async Task<(bool ok, string message, string imageUrl)> GetFallbackImageAsync(string name, string number, int officialTotal)
    {
        var cleanName = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanName) || !int.TryParse((number ?? "").Trim(), out var num))
            return (false, "Dados insuficientes para fallback.", "");

        var query = $"name:\"{cleanName}\" number:{num}";
        var url = $"https://api.pokemontcg.io/v2/cards?q={Uri.EscapeDataString(query)}&pageSize=20&select=images,set";

        try
        {
            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return (false, $"Fallback HTTP {(int)resp.StatusCode}", "");

            var body = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PtcgSearchResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var data = result?.data;
            if (data is null || data.Count == 0)
                return (true, "Sem resultado no fallback.", "");

            PtcgCardDto? chosen = null;
            if (officialTotal > 0)
            {
                foreach (var c in data)
                {
                    if (c.set.printedTotal == officialTotal)
                    {
                        chosen = c;
                        break;
                    }
                }
            }

            if (chosen is null && data.Count == 1)
                chosen = data[0];

            if (chosen is null)
                return (true, "Sem correspondência única no fallback.", "");

            var img = !string.IsNullOrWhiteSpace(chosen.images.small) ? chosen.images.small : chosen.images.large;
            return (true, "OK", NormalizeUrl(img));
        }
        catch (Exception ex)
        {
            return (false, $"Falha no fallback: {ex.Message}", "");
        }
    }

    // ===== DTOs =====
    public class CardCountDto
    {
        public int total { get; set; }
        public int official { get; set; }
    }

    public class SetDto
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string serie { get; set; } = "";
        public string symbol { get; set; } = "";
        public string logo { get; set; } = "";
        public CardCountDto cardCount { get; set; } = new();
    }

    public class CardDetailsDto
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public JsonElement image { get; set; }
        public JsonElement images { get; set; }
        public JsonElement variants { get; set; }
    }

    public class SetDetailsDto
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public JsonElement serie { get; set; }
        public CardCountDto cardCount { get; set; } = new();
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

    public class CardSearchItemDto
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string localId { get; set; } = "";
        public JsonElement image { get; set; }
    }

    public class PtcgSearchResponse
    {
        public List<PtcgCardDto> data { get; set; } = new();
    }

    public class PtcgCardDto
    {
        public PtcgImagesDto images { get; set; } = new();
        public PtcgSetDto set { get; set; } = new();
    }

    public class PtcgImagesDto
    {
        public string small { get; set; } = "";
        public string large { get; set; } = "";
    }

    public class PtcgSetDto
    {
        public int printedTotal { get; set; }
        public int total { get; set; }
    }

}