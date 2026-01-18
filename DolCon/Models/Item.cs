using DolCon.Enums;

namespace DolCon.Models;

public record Item
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Rarity Rarity { get; set; }
    public List<Tag> Tags { get; set; } = null!;
    public int Price { get; set; }
    public Equipment Equipment { get; set; }
    public bool Equipped { get; set; }
}