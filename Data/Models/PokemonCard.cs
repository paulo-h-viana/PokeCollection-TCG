namespace PokeCollection.Data.Models;

public class PokemonCard
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = "";

    public int PokemonSetId { get; set; }
    public PokemonSet? Set { get; set; }

    public string Name { get; set; } = "";
public string Number { get; set; } = "";
public string Rarity { get; set; } = "";
public string ImageSmallUrl { get; set; } = "";

    public bool Owned { get; set; }
}