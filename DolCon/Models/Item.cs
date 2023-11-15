using DolCon.Enums;

namespace DolCon.Models;

public record Item
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Rarity Rarity { get; set; }
    public List<Tag> Tags { get; set; }
    public int Price { get; set; }
    public Equipment Equipment { get; set; }
    public bool Equipped { get; set; } 
}