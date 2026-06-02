using System.Text.Json;
using System.Text.RegularExpressions;

namespace PokeCollection.Data.Services;

public record PromotionItem(string Title, decimal Price, string Link, string ImageUrl, string Source);

public class PromotionApiService
{
    private readonly HttpClient _http;

    public PromotionApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(bool ok, string message, List<PromotionItem> result)> GetPromotionsAsync()
    {
        var promotions = new List<PromotionItem>();
        string query = "pokemon tcg booster box";

        try
        {
            var mlUrl = $"https://api.mercadolibre.com/sites/MLB/search?q={Uri.EscapeDataString(query)}&limit=12";
            var mlResponse = await _http.GetAsync(mlUrl);
            
            if (mlResponse.IsSuccessStatusCode)
            {
                var mlContent = await mlResponse.Content.ReadAsStringAsync();
                using var mlJson = JsonDocument.Parse(mlContent);
                var results = mlJson.RootElement.GetProperty("results");
                
                foreach (var item in results.EnumerateArray())
                {
                    var title = item.GetProperty("title").GetString() ?? "";
                    var price = item.GetProperty("price").GetDecimal();
                    var link = item.GetProperty("permalink").GetString() ?? "";
                    var thumbnail = item.GetProperty("thumbnail").GetString() ?? "";
                    
                    thumbnail = thumbnail.Replace("http://", "https://");
                    promotions.Add(new PromotionItem(title, price, link, thumbnail, "Mercado Livre"));
                }
            }
        }
        catch { }

        try
        {
            var amzRequest = new HttpRequestMessage(HttpMethod.Get, $"https://www.amazon.com.br/s?k={Uri.EscapeDataString(query)}");
            amzRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            
            var amzResponse = await _http.SendAsync(amzRequest);
            if (amzResponse.IsSuccessStatusCode)
            {
                var amzHtml = await amzResponse.Content.ReadAsStringAsync();
                
                var titleRegex = new Regex(@"<span class=""a-size-base-plus a-color-base a-text-normal"">([^<]+)</span>");
                var priceRegex = new Regex(@"<span class=""a-price-whole"">([0-9.,]+)</span>");
                var linkRegex = new Regex(@"<a class=""a-link-normal s-no-outline"" href=""([^""]+)"">");
                var imgRegex = new Regex(@"<img class=""s-image"" src=""([^""]+)""");

                var titles = titleRegex.Matches(amzHtml);
                var prices = priceRegex.Matches(amzHtml);
                var links = linkRegex.Matches(amzHtml);
                var imgs = imgRegex.Matches(amzHtml);

                int count = Math.Min(6, Math.Min(titles.Count, prices.Count));
                
                for (int i = 0; i < count; i++)
                {
                    if (i < links.Count && i < imgs.Count)
                    {
                        var title = titles[i].Groups[1].Value;
                        var priceStr = prices[i].Groups[1].Value.Replace(".", "").Replace(",", ".");
                        
                        if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
                        {
                            var link = "https://www.amazon.com.br" + links[i].Groups[1].Value;
                            var img = imgs[i].Groups[1].Value;
                            promotions.Add(new PromotionItem(title, price, link, img, "Amazon"));
                        }
                    }
                }
            }
        }
        catch { }

        if (promotions.Count == 0)
        {
            return (false, "Nenhuma oferta encontrada no momento.", new());
        }

        return (true, $"Encontradas {promotions.Count} ofertas.", promotions.OrderBy(x => x.Price).ToList());
    }
}