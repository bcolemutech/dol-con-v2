namespace DolCon.Core.Services;

using DolCon.Core.Enums;

public interface IMoveService
{
    MoveStatus MoveToCell(int cellId);
    bool MoveToLocation(Guid locationId);
    bool MoveToBurg(int burg);
    bool Sleep(Rarity? quality = null);
}

public class MoveService : IMoveService
{
    private readonly IPositionUpdateHandler _positionUpdateHandler;

    public MoveService(IPositionUpdateHandler positionUpdateHandler)
    {
        _positionUpdateHandler = positionUpdateHandler;
    }

    public MoveService() : this(new NoOpPositionUpdateHandler())
    {
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
        _positionUpdateHandler.OnPositionUpdated();

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

    public bool Sleep(Rarity? quality = null)
    {
        var party = SaveGameService.Party;

        var sleepLimit = .5;

        if (quality.HasValue)
        {
            sleepLimit = quality.Value switch
            {
                Rarity.Common => .75,
                Rarity.Uncommon => 1,
                Rarity.Rare => 1.25,
                Rarity.Epic => 1.50,
                Rarity.Legendary => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
            };
        }

        if (party.Stamina >= sleepLimit) return false;

        party.Stamina = sleepLimit;
        return true;
    }
}
