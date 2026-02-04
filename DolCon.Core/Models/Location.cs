namespace DolCon.Core.Models;

using Enums;

public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public LocationType Type { get; set; } = null!;
    public Rarity Rarity { get; set; }
    public DateTime LastExplored { get; set; }
    public bool Discovered { get; set; }
    public double ExploredPercent { get; set; }
}
