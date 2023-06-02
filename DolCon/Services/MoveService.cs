namespace DolCon.Services;

using ChanceNET;
using Enums;

public interface IMoveService
{
    bool? ProcessExploration();
    bool MoveToCell(int cellId);
    bool MoveToLocation(Guid locationId);
    bool MoveToBurg(int burg);
    void Camp();
}

public class MoveService : IMoveService
{
    private readonly IImageService _imageService;

    public MoveService(IImageService imageService)
    {
        _imageService = imageService;
    }

    public bool? ProcessExploration()
    {
        var party = SaveGameService.Party;
        const int defaultExploration = 100;
        var currentLocation = SaveGameService.CurrentLocation;
        int locationExplorationSize;
        double explored;
        var currentBurg = SaveGameService.CurrentBurg;
        if (currentLocation is not null)
        {
            if (!party.TryMove(.005)) return false;

            locationExplorationSize = (int)currentLocation.Type.Size * 100;
            explored = currentLocation.ExploredPercent * locationExplorationSize;

            explored += defaultExploration;

            currentLocation.ExploredPercent = explored / locationExplorationSize;

            return true;
        }

        if (currentBurg is not null) return null;

        if (!party.TryMove(.05)) return false;

        var currentCell = SaveGameService.CurrentCell;

        locationExplorationSize = currentCell.CellSize == CellSize.small ? 300 : 500;

        explored = currentCell.ExploredPercent * locationExplorationSize;
        explored += defaultExploration;
        currentCell.ExploredPercent = explored / locationExplorationSize;

        if (Math.Abs(currentCell.ExploredPercent - 1) < .01)
        {
            currentCell.ExploredPercent = 1;
            currentCell.locations.ForEach(x => x.Discovered = true);
            return true;
        }

        var chance = new Chance();
        var dice = chance.Dice(20);
        if (dice > 5)
        {
            var random1 = new Random();
            var pick1 = random1.Next(0, currentCell.locations.Count(x => !x.Discovered));
            var location1 = currentCell.locations.Where(x => !x.Discovered).Skip(pick1)
                .Take(1).First();
            location1.Discovered = true;
        }

        if (dice < 18) return true;

        var random2 = new Random();
        var pick2 = random2.Next(0, currentCell.locations.Count(x => !x.Discovered));
        var location2 = currentCell.locations.Where(x => !x.Discovered).Skip(pick2)
            .Take(1).First();
        location2.Discovered = true;
        return true;
    }

    public bool MoveToCell(int cellId)
    {
        var party = SaveGameService.Party;

        if (!party.TryMove(.1)) return false;

        party.Cell = cellId;
        _imageService.ProcessSvg();

        return true;
    }

    public bool MoveToLocation(Guid locationId)
    {
        var party = SaveGameService.Party;

        if (!party.TryMove(.002)) return false;

        party.Location = locationId;

        return true;
    }

    public bool MoveToBurg(int burg)
    {
        var party = SaveGameService.Party;

        if (!party.TryMove(.01)) return false;

        party.Burg = burg;

        return true;
    }

    public void Camp()
    {
        var party = SaveGameService.Party;

        party.Stamina += .5;
        
        if (party.Stamina > 1)
        {
            party.Stamina = 1;
        }
    }
}
