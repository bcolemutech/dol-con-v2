namespace DolCon.Services;

using System.Text.Json;
using Enums;
using Models;
using Models.BaseTypes;
using Spectre.Console;

public interface IMapService
{
    IEnumerable<FileInfo> GetMaps();
    void LoadMap(FileInfo mapFile);
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
    private readonly IImageService _imageService;
    private static List<LocationType> _burgTypes = new();
    private List<LocationType> _cellTypes = new();

    public MapService(IPlayerService playerService, IImageService imageService)
    {
        _playerService = playerService;
        _imageService = imageService;
        _mapsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon",
            "Maps");
        if (Directory.Exists(_mapsPath)) return;

        AnsiConsole.WriteLine("Creating maps directory...");
        Directory.CreateDirectory(_mapsPath);
    }

    public IEnumerable<FileInfo> GetMaps()
    {
        var maps = new DirectoryInfo(_mapsPath).GetFiles("*.json");
        if (maps.Any())
        {
            return maps;
        }

        AnsiConsole.MarkupLine("[red bold]No maps found![/]");
        AnsiConsole.WriteLine(
            "Create a map using Azgaar's Fantasy Map Generator (https://azgaar.github.io/Fantasy-Map-Generator/).");
        AnsiConsole.WriteLine(
            "Once you have created a map, export it as a JSON file and place it in the maps directory.");

        return new List<FileInfo>();
    }

    public void LoadMap(FileInfo mapFile)
    {
        Map map = new();
        AnsiConsole.Status().Start("Loading map...", ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));
            ctx.Refresh();
            map = DeserializeMap(mapFile).Result ?? throw new DolMapException("Failed to load map");
            AnsiConsole.MarkupLine("Loaded map [yellow]{0}[/]", mapFile.Name);
            ProvisionMap(ctx, map);
        });
        SaveGameService.CurrentMap = map;
    }

    private void ProvisionMap(StatusContext ctx, Map map)
    {
        ctx.Status("Identifying City of Light...");
        ctx.Refresh();

        var topPop = map.Collections.burgs.Max(x => x.population);
        var cityOfLight = map.Collections.burgs.First(x => Math.Abs(x.population - topPop) < 0.01);
        cityOfLight.isCityOfLight = true;

        var dolLocations = LocationTypes.Types.Where(x => x.NeedsCityOfLight).ToList();
        cityOfLight.locations.AddRange(dolLocations.Select(x => new Location
            { Type = x, Name = x.Type, Rarity = x.Rarity }));
        AnsiConsole.MarkupLine("City of Light established as [yellow]{0}[/]", cityOfLight.name);

        ctx.Status("Provisioning cells...");
        ctx.Refresh();

        _cellTypes = LocationTypes.Types.Where(x => !x.IsBurgLocation).ToList();

        foreach (var cell in map.Collections.cells)
        {
            ctx.Status($"Setting up cell: {cell.i}...");
            ctx.Refresh();
            cell.locations.AddRange(ProvisionCellLocations(cell));
        }

        ctx.Status("Provisioning burgs...");
        ctx.Refresh();

        var minPop = map.Collections.burgs.Min(x => x.population);

        _burgTypes = LocationTypes.Types.Where(x => x is
            { NeedsCityOfLight: false, IsBurgLocation: true, NeedsPort: false, NeedsTemple: false }).ToList();

        foreach (var burg in map.Collections.burgs)
        {
            ctx.Status($"Setting up burg: {burg.name}...");
            ctx.Refresh();
            AdjustBurgSize(burg, minPop);
            burg.locations.AddRange(ProvisionBurgLocations(burg));
        }

        ctx.Status("Setting player position...");
        ctx.Refresh();

        var player = _playerService.SetPlayer("Player 1", false);
        SaveGameService.Party.Players.Add(player);
        SaveGameService.CurrentPlayerId = player.Id;
        SaveGameService.Party.Cell = cityOfLight.cell;
        SaveGameService.Party.Burg = cityOfLight.i;
        _imageService.ProcessSvg();

        AnsiConsole.MarkupLine("Player position set to [yellow]{0}[/]", cityOfLight.name);
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
