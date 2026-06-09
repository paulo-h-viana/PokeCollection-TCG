namespace PokeCollection.Data.Models;

public class UserCollectionCard
{
    public int Id { get; set; }

    public int UserCollectionId { get; set; }
    public UserCollection? Collection { get; set; }

    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Number { get; set; } = "";
    public string ImageSmallUrl { get; set; } = "";

    public bool Owned { get; set; }
    public DateTime? AcquiredAt { get; set; }
}
