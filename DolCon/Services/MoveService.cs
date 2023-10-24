namespace DolCon.Services;

using ChanceNET;
using Enums;

public interface IMoveService
{
    MoveStatus ProcessExploration();
    MoveStatus MoveToCell(int cellId);
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

    public MoveStatus ProcessExploration()
    {
        var party = SaveGameService.Party;
        const int defaultExploration = 100;
        var currentLocation = SaveGameService.CurrentLocation;
        int locationExplorationSize;
        double explored;
        var currentBurg = SaveGameService.CurrentBurg;
        if (currentLocation is not null)
        {
            if (!party.TryMove(.005)) return MoveStatus.Failure;

            locationExplorationSize = (int)currentLocation.Type.Size * 100;
            explored = currentLocation.ExploredPercent * locationExplorationSize;

            explored += defaultExploration;

            currentLocation.ExploredPercent = explored / locationExplorationSize;

            return MoveStatus.Success;
        }

        if (currentBurg is not null) return MoveStatus.None;

        if (!party.TryMove(.05)) return MoveStatus.Failure;

        var currentCell = SaveGameService.CurrentCell;

        locationExplorationSize = currentCell.CellSize == CellSize.small ? 300 : 500;

        explored = currentCell.ExploredPercent * locationExplorationSize;
        explored += defaultExploration;
        currentCell.ExploredPercent = explored / locationExplorationSize;

        if (Math.Abs(currentCell.ExploredPercent - 1) < .01)
        {
            currentCell.ExploredPercent = 1;
            currentCell.locations.ForEach(x => x.Discovered = true);
            return MoveStatus.Success;
        }

        var chance = new Chance();
        var dice = chance.Dice(20);
        if (dice > 0)
        {
            var random1 = new Random();
            var pick1 = random1.Next(0, currentCell.locations.Count(x => !x.Discovered));
            var location1 = currentCell.locations.Where(x => !x.Discovered).Skip(pick1)
                .Take(1).First();
            location1.Discovered = true;
        }

        if (dice < 12) return MoveStatus.Success;

        var random2 = new Random();
        var pick2 = random2.Next(0, currentCell.locations.Count(x => !x.Discovered));
        var location2 = currentCell.locations.Where(x => !x.Discovered).Skip(pick2)
            .Take(1).First();
        location2.Discovered = true;
        return MoveStatus.Success;
    }

    public MoveStatus MoveToCell(int cellId)
    {
        var party = SaveGameService.Party;
        var cell = SaveGameService.CurrentMap.Collections.cells[cellId];

        double baseMoveCost;

        switch (cell.Biome)
        {
            case Biome.Marine:
                return MoveStatus.Blocked;
            case Biome.HotDesert:
                baseMoveCost = .3;
                break;
            case Biome.ColdDesert:
                baseMoveCost = .25;
                break;
            case Biome.Savanna:
                baseMoveCost = .1;
                break;
            case Biome.Grassland:
                baseMoveCost = .05;
                break;
            case Biome.TropicalSeasonalForest:
                baseMoveCost = .2;
                break;
            case Biome.TemperateDeciduousForest:
                baseMoveCost = .2;
                break;
            case Biome.TropicalRainForest:
                baseMoveCost = .3;
                break;
            case Biome.TemperateRainForest:
                baseMoveCost = .25;
                break;
            case Biome.Taiga:
                baseMoveCost = .2;
                break;
            case Biome.Tundra:
                baseMoveCost = .15;
                break;
            case Biome.Glacier:
                baseMoveCost = .5;
                break;
            case Biome.Wetland:
                baseMoveCost = .4;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var divisor = cell.CellSize == CellSize.large ? 2 : 4;
        
        var moveCost = (1 - cell.ExploredPercent) / divisor;
        
        moveCost = moveCost < baseMoveCost ? baseMoveCost : moveCost;

        if (!party.TryMove(moveCost)) return MoveStatus.Failure;

        party.Cell = cellId;
        _imageService.ProcessSvg();

        return MoveStatus.Success;
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
