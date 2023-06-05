namespace DolCon.Models;

public class Player
{
    public Player()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Npc { get; set; }
    public int coin { get; set; }
    public List<Item> Inventory { get; set; } = new();

    public int copper => coin;
    public int silver => coin / 10;
    public int gold => coin / 100;
    public int platinum => coin / 1000;
}
