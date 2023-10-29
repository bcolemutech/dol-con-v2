using DolCon.Enums;

namespace DolCon.Models;

public record Item
{
    public string Name { get; set; }
    public string description { get; set; }
    public Rarity rarity { get; set; }
    public List<Tag> tags { get; set; }
    public int price { get; set; }
}
