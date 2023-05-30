namespace DolCon.Models;

using Enums;

public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public LocationType Type { get; set; }
    public Rarity Rarity { get; set; }
    public DateTime LastExplored { get; set; }
    public bool Discovered { get; set; }
    public double ExploredPercent { get; set; }
}
