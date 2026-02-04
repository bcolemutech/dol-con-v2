using DolCon.Core.Models.BaseTypes;

namespace DolCon.Core.Models;

public class Event
{
    public Event(Location? currentLocation, Cell currentCell)
    {
        Location = currentLocation;
        Area = currentCell;
    }

    public Location? Location { get; set; }
    public Cell Area { get; set; }
}