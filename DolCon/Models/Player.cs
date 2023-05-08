namespace DolCon.Models;

public class Player
{
    public Player()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public int Cell { get; set; }
    public int? Burg { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Npc { get; set; }
}
