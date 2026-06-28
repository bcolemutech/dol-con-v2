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
    void LoadMap(FileInfo mapFile, string playerName, PlayerAbilities abilities);
    void LoadMap(FileInfo mapFile, IMapProvisioningCallback callback, string playerName, PlayerAbilities abilities);
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
    private readonly IWorldProvisioningService _worldProvisioningService;

    public MapService(IPlayerService playerService, IPositionUpdateHandler positionUpdateHandler,
        IWorldProvisioningService worldProvisioningService)
    {
        _playerService = playerService;
        _positionUpdateHandler = positionUpdateHandler;
        _worldProvisioningService = worldProvisioningService;
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

    public MapService(IPlayerService playerService)
        : this(playerService, new NoOpPositionUpdateHandler(), new WorldProvisioningService())
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

    public void LoadMap(FileInfo mapFile, string playerName, PlayerAbilities abilities)
    {
        LoadMap(mapFile, new NoOpMapProvisioningCallback(), playerName, abilities);
    }

    public void LoadMap(FileInfo mapFile, IMapProvisioningCallback callback, string playerName, PlayerAbilities abilities)
    {
        callback.OnStatus("Loading map...");
        var map = DeserializeMap(mapFile).Result ?? throw new DolMapException("Failed to load map");
        callback.OnEvent($"Loaded map {mapFile.Name}");
        ProvisionMap(callback, map, playerName, abilities);
        SaveGameService.CurrentMap = map;
    }

    private void ProvisionMap(IMapProvisioningCallback callback, Map map,
        string? playerName = null, PlayerAbilities? abilities = null)
    {
        // World provisioning now lives in WorldProvisioningService (shared with WorldForge). The game
        // still provisions at load time until Phase 2 wires it to consume world.dol. A fresh seed each
        // load preserves today's "flair differs every game" behaviour.
        _worldProvisioningService.Provision(map, Environment.TickCount, callback);

        callback.OnStatus("Setting player position...");

        var cityOfLight = map.Collections.burgs.First(x => x.isCityOfLight);
        SaveGameService.Party = new Party
        {
            Cell = cityOfLight.cell,
            Burg = cityOfLight.i,
            Stamina = 1
        };
        var player = abilities != null
            ? _playerService.SetPlayer(playerName ?? "Player 1", false, abilities)
            : _playerService.SetPlayer(playerName ?? "Player 1", false);
        SaveGameService.CurrentPlayerId = player.Id;
        _positionUpdateHandler.OnPositionUpdated();

        callback.OnEvent($"Player position set to {cityOfLight.name}");
    }

    private static async Task<Map?> DeserializeMap(FileSystemInfo mapFile)
    {
        var stream = File.OpenRead(mapFile.FullName);
        return await JsonSerializer.DeserializeAsync<Map>(stream);
    }
}
