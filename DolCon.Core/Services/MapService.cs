namespace DolCon.Core.Services;

using System.Text.Json;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.BaseTypes;

public interface IMapService
{
    IEnumerable<FileInfo> GetMaps();
    void LoadMap(FileInfo mapFile);
    void LoadMap(FileInfo mapFile, IMapProvisioningCallback callback);
}

public class MapService : IMapService
{
    public static Direction GetDirection(double ax, double ay, double bx, double by)
    {
        var angle = Math.Atan2(by - ay, bx - ax);
        angle += Math.PI;
        angle /= Math.PI / 8;
        var halfQuarter = Convert.ToInt32(angle);
        halfQuarter %= 16;
        return (Direction)halfQuarter;
    }

    private readonly string _mapsPath;
    private readonly IPlayerService _playerService;
    private readonly IPositionUpdateHandler _positionUpdateHandler;
    private static List<LocationType> _burgTypes = new();
    private List<LocationType> _cellTypes = new();

    public MapService(IPlayerService playerService, IPositionUpdateHandler positionUpdateHandler)
    {
        _playerService = playerService;
        _positionUpdateHandler = positionUpdateHandler;
        var mainPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon");
        _mapsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon",
            "Maps");
        if (Directory.Exists(_mapsPath)) return;

        Directory.CreateDirectory(mainPath);

        // Move pre-built maps if they exist
        var prebuiltPath = Environment.CurrentDirectory + "/PrebuiltMaps";
        if (Directory.Exists(prebuiltPath))
        {
            Directory.Move(prebuiltPath, _mapsPath);
        }
    }

    public MapService(IPlayerService playerService) : this(playerService, new NoOpPositionUpdateHandler())
    {
    }

    public IEnumerable<FileInfo> GetMaps()
    {
        var maps = new DirectoryInfo(_mapsPath).GetFiles("*.json");
        return maps;
    }

    public void LoadMap(FileInfo mapFile)
    {
        LoadMap(mapFile, new NoOpMapProvisioningCallback());
    }

    public void LoadMap(FileInfo mapFile, IMapProvisioningCallback callback)
    {
        callback.OnStatus("Loading map...");
        var map = DeserializeMap(mapFile).Result ?? throw new DolMapException("Failed to load map");
        callback.OnEvent($"Loaded map {mapFile.Name}");
        ProvisionMap(callback, map);
        SaveGameService.CurrentMap = map;
    }

    private void ProvisionMap(IMapProvisioningCallback callback, Map map)
    {
        callback.OnStatus("Identifying City of Light...");

        var topPop = map.Collections.burgs.Max(x => x.population);
        var cityOfLight = map.Collections.burgs.First(x => Math.Abs(x.population - topPop) < 0.01);
        cityOfLight.isCityOfLight = true;

        var dolLocations = LocationTypes.Types.Where(x => x.NeedsCityOfLight).ToList();
        cityOfLight.locations.AddRange(dolLocations.Select(x => new Location
            { Id = Guid.NewGuid(), Type = x, Name = x.Type, Rarity = x.Rarity }));
        callback.OnEvent($"City of Light established as {cityOfLight.name}");

        var colCell = map.Collections.cells.First(x => x.i == cityOfLight.cell);
        var colX = colCell.p[0];
        var colY = colCell.p[1];

        callback.OnStatus("Provisioning cells...");

        _cellTypes = LocationTypes.Types.Where(x => !x.IsBurgLocation).ToList();

        var crDistance = map.Collections.cells.Max(x => x.p.Max()) / 2;

        foreach (var cell in map.Collections.cells)
        {
            callback.OnStatus($"Setting up cell: {cell.i}...");
            cell.locations.AddRange(ProvisionCellLocations(cell));
            cell.ChallengeRating = CalculateChallengeRating(cell, colX, colY, crDistance);
        }

        callback.OnStatus("Provisioning burgs...");

        var minPop = map.Collections.burgs.Min(x => x.population);

        _burgTypes = LocationTypes.Types.Where(x => x is
            { NeedsCityOfLight: false, IsBurgLocation: true, NeedsPort: false, NeedsTemple: false }).ToList();

        foreach (var burg in map.Collections.burgs)
        {
            callback.OnStatus($"Setting up burg: {burg.name}...");
            AdjustBurgSize(burg, minPop);
            burg.locations.AddRange(ProvisionBurgLocations(burg));
        }

        callback.OnStatus("Setting player position...");

        SaveGameService.Party = new Party
        {
            Cell = cityOfLight.cell,
            Burg = cityOfLight.i,
            Stamina = 1
        };
        var player = _playerService.SetPlayer("Player 1", false);
        SaveGameService.CurrentPlayerId = player.Id;
        _positionUpdateHandler.OnPositionUpdated();

        callback.OnEvent($"Player position set to {cityOfLight.name}");
    }

    /// <summary>
    /// Calculates the challenge rating for a cell based on its distance from the City of Light.
    /// </summary>
    /// <param name="cell">The cell to calculate the challenge rating for.</param>
    /// <param name="colX">The X coordinate of the City of Light.</param>
    /// <param name="colY">The Y coordinate of the City of Light.</param>
    /// <param name="crDistance">The maximum distance used for CR scaling (typically map dimension).</param>
    /// <returns>
    /// A challenge rating value rounded to the nearest 1/8th (0.125).
    /// Cells closer to the City of Light have lower CRs, while distant cells have higher CRs.
    /// The rating scales from 0 to 20 based on the distance ratio.
    /// </returns>
    public static double CalculateChallengeRating(Cell cell, double colX, double colY, double crDistance)
    {
        var x = cell.p[0];
        var y = cell.p[1];
        var distance = Math.Sqrt(Math.Pow(x - colX, 2) + Math.Pow(y - colY, 2));
        var crRatio = distance / crDistance;
        var rawRating = crRatio * 20;
        var nearest8th = Math.Round(rawRating * 8, MidpointRounding.AwayFromZero) / 8;
        return nearest8th;
    }

    private IEnumerable<Location> ProvisionCellLocations(Cell cell)
    {
        var locations = new List<Location>();

        var wilderness = _cellTypes.Where(x => x.isWild).ToList();
        var nonWilderness = _cellTypes.Where(x => !x.isWild).ToList();

        int wildCells;
        int nonWildCells;

        switch (cell)
        {
            case { CellSize: CellSize.small, PopDensity: PopDensity.wild }:

                nonWildCells = 1;
                wildCells = 4;
                break;
            case { CellSize: CellSize.small, PopDensity: PopDensity.rural }:
                nonWildCells = 2;
                wildCells = 3;
                break;
            case { CellSize: CellSize.small, PopDensity: PopDensity.urban }:
                nonWildCells = 3;
                wildCells = 2;
                break;
            case { CellSize: CellSize.large, PopDensity: PopDensity.wild }:
                nonWildCells = 2;
                wildCells = 6;
                break;
            case { CellSize: CellSize.large, PopDensity: PopDensity.rural }:
                nonWildCells = 3;
                wildCells = 5;
                break;
            case { CellSize: CellSize.large, PopDensity: PopDensity.urban }:
                nonWildCells = 5;
                wildCells = 3;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var random = new Random();
        var i = 0;
        while (i < nonWildCells)
        {
            var index = random.Next(nonWilderness.Count);
            var location = nonWilderness[index];
            if (!location.AllowMultiple && locations.Any(x => x.Type == location))
            {
                continue;
            }

            locations.Add(new Location { Id = Guid.NewGuid(), Type = location, Name = location.Type, Rarity = location.Rarity });
            i++;
        }

        i = 0;
        while (i < wildCells)
        {
            var index = random.Next(wilderness.Count);
            var location = wilderness[index];
            if (!location.AllowMultiple && locations.Any(x => x.Type == location))
            {
                continue;
            }

            locations.Add(new Location { Id = Guid.NewGuid(), Type = location, Name = location.Type, Rarity = location.Rarity });
            i++;
        }

        return locations;
    }

    public static void AdjustBurgSize(Burg burg, double minPop)
    {
        double maxPop = 75;
        double idealMin = .1;
        double idealMax = 350;

        if (burg.population < maxPop)
        {
            burg.population = (((burg.population - minPop) / (maxPop - minPop)) * (idealMax - idealMin)) + idealMin;

            burg.population = burg.isCityOfLight ? burg.population * 3 : burg.population;
        }
        else
        {
            burg.population += idealMax;
        }

        burg.size = burg.population switch
        {
            > 0 and < 2 => BurgSize.Village,
            >= 2 and < 50 => BurgSize.Town,
            >= 50 and < 150 => BurgSize.City,
            >= 150 and < 300 => BurgSize.Metropolis,
            >= 300 => BurgSize.Megalopolis,
            _ => burg.size
        };
    }

    private static List<Location> ProvisionBurgLocations(Burg burg)
    {
        var locations = new List<Location>();

        var sequence = burg.size switch
        {
            BurgSize.Village => new[] { 2, 1, 0, 0, 0 },
            BurgSize.Town => new[] { 3, 2, 1, 0, 0 },
            BurgSize.City => new[] { 5, 3, 2, 1, 0 },
            BurgSize.Metropolis => new[] { 8, 5, 3, 2, 1 },
            BurgSize.Megalopolis => new[] { 13, 8, 5, 3, 2 },
            _ => new[] { 0, 0, 0, 0, 0 }
        };

        if (burg.port == 1)
        {
            switch (burg.size)
            {
                case BurgSize.Village:
                    var pier = LocationTypes.Types.First(x => x.Type == "pier");
                    locations.Add(new Location { Id = Guid.NewGuid(), Type = pier, Name = "Pier", Rarity = pier.Rarity });
                    break;
                case BurgSize.Town:
                    var dock = LocationTypes.Types.First(x => x.Type == "dock");
                    locations.Add(new Location { Id = Guid.NewGuid(), Type = dock, Name = $"{burg.name} Docks", Rarity = dock.Rarity });
                    break;
                case BurgSize.City:
                    var harbor = LocationTypes.Types.First(x => x.Type == "harbor");
                    locations.Add(new Location { Id = Guid.NewGuid(), Type = harbor, Name = $"{burg.name} Harbor", Rarity = harbor.Rarity });
                    break;
                case BurgSize.Metropolis:
                case BurgSize.Megalopolis:
                    var port = LocationTypes.Types.First(x => x.Type == "port");
                    locations.Add(new Location { Id = Guid.NewGuid(), Type = port, Name = $"{burg.name} Port", Rarity = port.Rarity });
                    break;
            }
        }

        if (burg is { temple: 1, isCityOfLight: false })
        {
            switch (burg.size)
            {
                case BurgSize.Village:
                case BurgSize.Town:
                    var shrine = LocationTypes.Types.First(x => x.Type == "shrine");
                    locations.Add(new Location { Id = Guid.NewGuid(), Type = shrine, Name = $"{burg.name} Shrine", Rarity = shrine.Rarity });
                    break;
                case BurgSize.City:
                case BurgSize.Metropolis:
                    var temple = LocationTypes.Types.First(x => x.Type == "temple");
                    locations.Add(new Location { Id = Guid.NewGuid(), Type = temple, Name = $"{burg.name} Temple", Rarity = temple.Rarity });
                    break;
                case BurgSize.Megalopolis:
                    var basilica = LocationTypes.Types.First(x => x.Type == "basilica");
                    locations.Add(new Location
                        { Id = Guid.NewGuid(), Type = basilica, Name = $"{burg.name} Basilica", Rarity = basilica.Rarity });
                    break;
            }
        }

        if (burg is { citadel: 1, isCityOfLight: false })
        {
            switch (burg.size)
            {
                case BurgSize.Village:
                case BurgSize.Town:
                    var manor = LocationTypes.Types.First(x => x.Type == "manor");
                    locations.Add(new Location { Id = Guid.NewGuid(), Type = manor, Name = $"{burg.name} Manor", Rarity = manor.Rarity });
                    break;
                case BurgSize.City:
                    var castle = LocationTypes.Types.First(x => x.Type == "castle");
                    locations.Add(new Location { Id = Guid.NewGuid(), Type = castle, Name = $"{burg.name} Castle", Rarity = castle.Rarity });
                    break;
                case BurgSize.Metropolis:
                    var fortress = LocationTypes.Types.First(x => x.Type == "fortress");
                    locations.Add(new Location
                        { Id = Guid.NewGuid(), Type = fortress, Name = $"{burg.name} Fortress", Rarity = fortress.Rarity });
                    break;
                case BurgSize.Megalopolis:
                    var citadel = LocationTypes.Types.First(x => x.Type == "citadel");
                    locations.Add(new Location
                        { Id = Guid.NewGuid(), Type = citadel, Name = $"{burg.name} Citadel", Rarity = citadel.Rarity });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var cnt = _burgTypes.Count - 1;
        for (var i = 0; i < 4; i++)
        {
            var j = 0;
            while (j < sequence[i])
            {
                var rarity = (Rarity)i;
                var rnd = new Random().Next(0, cnt);
                var locationType = _burgTypes[rnd];
                if (rarity < locationType.Rarity || rarity > locationType.MaxRarity)
                {
                    j++;
                    continue;
                }

                if (locationType.AllowMultiple == false && locations.Any(x => x.Type.Type == locationType.Type))
                {
                    j++;
                    continue;
                }

                if ((locationType.NeedsCitadel && burg.citadel != 1) ||
                    (locationType.NeedsPlaza && burg.plaza != 1) ||
                    (locationType.NeedsShanty && burg.shanty != 1) ||
                    (locationType.NeedsWalls && burg.walls != 1) ||
                    (locationType.NeedsTemple && burg.temple != 1))
                {
                    j++;
                    continue;
                }

                var location = new Location
                {
                    Id = Guid.NewGuid(),
                    Type = locationType,
                    Name = $"{burg.name} {locationType.Type}",
                    Rarity = rarity
                };
                locations.Add(location);
                j++;
            }
        }

        if (locations.Count == 0)
        {
            var tavern = LocationTypes.Types.First(x => x.Type == "tavern");
            locations.Add(new Location { Id = Guid.NewGuid(), Type = tavern, Name = $"{burg.name} Tavern", Rarity = tavern.Rarity });
        }

        return locations;
    }

    private static async Task<Map?> DeserializeMap(FileSystemInfo mapFile)
    {
        var stream = File.OpenRead(mapFile.FullName);
        return await JsonSerializer.DeserializeAsync<Map>(stream);
    }
}
