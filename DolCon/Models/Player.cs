namespace DolCon.Models;

using Newtonsoft.Json;

public class Player
{
    public Player()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Npc { get; set; }
    public long coin { get; set; }
    public List<Item> Inventory { get; set; } = new();

    [JsonIgnore]
    public long copper => coin % 10;
    [JsonIgnore]
    public long silver => coin / 10 % 100;

    [JsonIgnore]
    public long gold => coin / 1000;
}
