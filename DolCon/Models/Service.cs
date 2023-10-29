using DolCon.Enums;

namespace DolCon.Models;

public abstract record Service
{
    public ServiceType Type { get; set; }
    public Rarity Rarity { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Price { get; set; }
}