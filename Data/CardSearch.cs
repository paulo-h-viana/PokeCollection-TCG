namespace PokeCollection.Data;

public static class CardSearch
{
    public static bool IsNumberQuery(string term, out string numberPart)
    {
        term = (term ?? "").Trim();
        numberPart = term.Contains('/') ? term.Split('/')[0].Trim() : term;
        return numberPart.Length > 0 && numberPart.All(char.IsDigit);
    }

    public static bool TryGetTotal(string term, out int total)
    {
        total = 0;
        term = (term ?? "").Trim();
        var idx = term.IndexOf('/');
        if (idx < 0) return false;
        return int.TryParse(term.Substring(idx + 1).Trim(), out total) && total > 0;
    }

    public static bool Matches(string name, string number, string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return true;

        term = term.Trim();

        if (name.Contains(term, StringComparison.OrdinalIgnoreCase))
            return true;

        var numberTerm = term.Contains('/') ? term.Split('/')[0].Trim() : term;
        if (string.IsNullOrWhiteSpace(numberTerm)) return false;

        if (number.Contains(numberTerm, StringComparison.OrdinalIgnoreCase))
            return true;

        return int.TryParse(numberTerm, out var queryNumber)
            && int.TryParse(number, out var cardNumber)
            && queryNumber == cardNumber;
    }
}
