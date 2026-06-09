namespace PokeCollection.Data.Models;

public class UserCollection
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public List<UserCollectionCard> Cards { get; set; } = new();
}
