using DolCon.Core.Models.World;

namespace DolCon.Core.Models;

public class Event
{
    public Event(WorldLocation? currentLocation, WorldCell currentCell)
    {
        Location = currentLocation;
        Area = currentCell;
    }

    public WorldLocation? Location { get; set; }
    public WorldCell Area { get; set; }
}