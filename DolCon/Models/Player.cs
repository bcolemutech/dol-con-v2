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
    public long copper => coin % 10; // 1 copper = 1 coin
    [JsonIgnore]
    public long silver => coin / 10 % 100; // 1 silver = 10 copper = 10 coin

    [JsonIgnore]
    public long gold => coin / 100; // 1 gold = 10 silver = 100 copper = 100 coin 
}
