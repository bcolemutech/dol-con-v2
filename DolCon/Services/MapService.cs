namespace DolCon.Services;

using System.Text.Json;
using Enums;
using Models;
using Models.BaseTypes;
using Spectre.Console;

public interface IMapService
{
    IEnumerable<FileInfo> GetMaps();
    Task LoadMap(FileInfo mapFile);
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
    private static List<LocationType> _burgTypes = new();

    public MapService(IPlayerService playerService)
    {
        _playerService = playerService;
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

    public async Task LoadMap(FileInfo mapFile)
    {
        Map map = new();
        await AnsiConsole.Status().StartAsync("Loading map...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));
            ctx.Refresh();
            map = await DeserializeMap(mapFile) ?? throw new DolMapException("Failed to load map");
            AnsiConsole.MarkupLine("Loaded map [yellow]{0}[/]", mapFile.Name);
            ProvisionMap(ctx, map);
        });
        SaveGameService.CurrentMap = map;
    }

    private void ProvisionMap(StatusContext ctx, Map map)
    {
        ctx.Status("Adjusting burgs' population...");
        ctx.Refresh();

        var maxPop = map.Collections.burgs.Max(x => x.population);
        var minPop = map.Collections.burgs.Min(x => x.population);
        var positionCeiling = maxPop - minPop;

        var adjustedMax = maxPop * 3;
        var adjustedMin = minPop / 3;
        var adjustedPositionCeiling = adjustedMax - adjustedMin;
        
        _burgTypes = LocationTypes.Types.Where(x => x is { NeedsCityOfLight: false, IsBurgLocation: true, NeedsPort: false, NeedsTemple: false }).ToList();

        foreach (var burg in map.Collections.burgs)
        {
            ctx.Status($"Adjusting {burg.name}...");
            ctx.Refresh();
            ProvisionBurg(burg, minPop, positionCeiling, adjustedPositionCeiling, adjustedMin);
        }

        ctx.Status("Identifying City of Light...");
        ctx.Refresh();

        var topPop = map.Collections.burgs.Max(x => x.population);
        var cityOfLight = map.Collections.burgs.First(x => Math.Abs(x.population - topPop) < 0.01);
        cityOfLight.isCityOfLight = true;
        cityOfLight.population *= 3;
        AnsiConsole.MarkupLine("City of Light established as [yellow]{0}[/]", cityOfLight.name);

        ctx.Status("Setting player position...");
        ctx.Refresh();

        var player = _playerService.SetPlayer("Player 1", false);
        SaveGameService.Party.Players.Add(player);
        SaveGameService.CurrentPlayerId = player.Id;
        SaveGameService.Party.Cell = cityOfLight.cell;
        SaveGameService.Party.Burg = cityOfLight.i;
        AnsiConsole.MarkupLine("Player position set to [yellow]{0}[/]", cityOfLight.name);
    }

    private static void ProvisionBurg(Burg burg, double minPop, double positionCeiling, double adjustedPositionCeiling,
        double adjustedMin)
    {
        var adjustedPosition = (burg.population - minPop) / positionCeiling * adjustedPositionCeiling;
        burg.population = adjustedPosition + adjustedMin;
        burg.size = burg.population switch
        {
            > 0 and < 100 => BurgSize.Hamlet,
            >= 100 and < 1000 => BurgSize.Village,
            >= 1000 and < 5000 => BurgSize.Town,
            >= 5000 and < 25000 => BurgSize.City,
            >= 25000 and < 100000 => BurgSize.Metropolis,
            >= 100000 => BurgSize.Megalopolis,
            _ => burg.size
        };
        burg.locations = ProvisionBurgLocations(burg);
    }

    private static List<Location> ProvisionBurgLocations(Burg burg)
    {
        var locations = new List<Location>();

        var sequence = burg.size switch
        {
            BurgSize.Hamlet => new[] { 1, 0, 0, 0, 0 },
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
                case BurgSize.Hamlet:
                    var pier = LocationTypes.Types.First(x => x.Type == "pier");
                    locations.Add(new Location { Type = pier, Name = "Pier", Rarity = pier.Rarity });
                    break;
                case BurgSize.Village:
                    var dock = LocationTypes.Types.First(x => x.Type == "dock");
                    locations.Add(new Location { Type = dock, Name = $"{burg.name} Docks", Rarity = dock.Rarity });
                    break;
                case BurgSize.Town:
                    var harbor = LocationTypes.Types.First(x => x.Type == "harbor");
                    locations.Add(new Location { Type = harbor, Name = $"{burg.name} Harbor", Rarity = harbor.Rarity });
                    break;
                case BurgSize.City:
                case BurgSize.Metropolis:
                case BurgSize.Megalopolis:
                    var port = LocationTypes.Types.First(x => x.Type == "port");
                    locations.Add(new Location { Type = port, Name = $"{burg.name} Port", Rarity = port.Rarity });
                    break;
            }
        }

        if (burg is { temple: 1, isCityOfLight: false })
        {
            switch (burg.size)
            {
                case BurgSize.Hamlet:
                case BurgSize.Village:
                    var shrine = LocationTypes.Types.First(x => x.Type == "shrine");
                    locations.Add(new Location { Type = shrine, Name = $"{burg.name} Shrine", Rarity = shrine.Rarity });
                    break;
                case BurgSize.Town:
                case BurgSize.City:
                    var temple = LocationTypes.Types.First(x => x.Type == "temple");
                    locations.Add(new Location { Type = temple, Name = $"{burg.name} Temple", Rarity = temple.Rarity });
                    break;
                case BurgSize.Metropolis:
                case BurgSize.Megalopolis:
                    var basilica = LocationTypes.Types.First(x => x.Type == "basilica");
                    locations.Add(new Location { Type = basilica, Name = $"{burg.name} Basilica", Rarity = basilica.Rarity });
                    break;
            }
        }

        if (burg.isCityOfLight)
        {
            var dolLocations = LocationTypes.Types.Where(x => x.NeedsCityOfLight).ToList();
            locations.AddRange(dolLocations.Select(x => new Location { Type = x, Name = x.Type, Rarity = x.Rarity }));
        }
        
        var cnt = _burgTypes.Count - 1;
        for (var i = 0; i < 5; i++)
        {
            var j = 0;
            while ( j < sequence[i])
            {
                var rarity = (Rarity)i;
                var rnd = new Random().Next(0, cnt);
                var locationType = _burgTypes[rnd];
                if (rarity < locationType.Rarity || rarity > locationType.MaxRarity)
                {
                    continue;
                }
                if (locationType.AllowMultiple == false && locations.Any(x => x.Type.Type == locationType.Type))
                {
                    continue;
                }
                if ((locationType.NeedsCitadel && burg.citadel != 1) ||
                    (locationType.NeedsPlaza && burg.plaza != 1) ||
                    (locationType.NeedsShanty && burg.shanty != 1) ||
                    (locationType.NeedsWalls && burg.walls != 1))
                {
                    continue;
                }

                var location = new Location
                {
                    Type = locationType,
                    Name = $"{burg.name} {locationType.Type}",
                    Rarity = rarity
                };
                locations.Add(location);
                j++;
            }
        }

        return locations;
    }

    private static async Task<Map?> DeserializeMap(FileSystemInfo mapFile)
    {
        var stream = File.OpenRead(mapFile.FullName);
        return await JsonSerializer.DeserializeAsync<Map>(stream);
    }
}
