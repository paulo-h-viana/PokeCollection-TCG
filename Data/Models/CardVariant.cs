namespace PokeCollection.Data.Models;

public class CardVariant
{
    public int Id { get; set; }

    public int PokemonCardId { get; set; }
    public PokemonCard? Card { get; set; }

    public string Type { get; set; } = "";

    public bool Owned { get; set; }
}
