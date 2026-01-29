using DolCon.Core.Enums;

namespace DolCon.Core.Models;

public record Service
{
    public ServiceType Type { get; set; }
    public Rarity Rarity { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Price { get; set; }
}