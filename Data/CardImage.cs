namespace PokeCollection.Data;

public static class CardImage
{
    public const string Placeholder = "/images/tcg-card-back.jpg";

    public static string BuildUrl(string? baseUrl, string quality = "low", string ext = "webp")
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return "";
        var url = baseUrl.Trim();
        if (url.EndsWith(".png") || url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".webp"))
            return url;
        return $"{url.TrimEnd('/')}/{quality}.{ext}";
    }
}
