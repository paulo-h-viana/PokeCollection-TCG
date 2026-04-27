namespace PokeCollection.Data.Models;

public class PokemonSet
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Series { get; set; } = "";
    public DateTime? ReleaseDate { get; set; }
    public int TotalCards {get; set;}
    public string Symbol {get; set;} = "";
    public string Logo {get ;set; } = "";

    public List<PokemonCard> Cards { get; set; } = new();
}