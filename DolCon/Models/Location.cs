namespace DolCon.Models;

using Enums;

public class Location
{
    string Name { get; set; }
    LocationType Type { get; set; }
    Rarity Rarity { get; set; }
    int Explored { get; set; }
    DateTime LastExplored { get; set; }
}
