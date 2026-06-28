namespace DolCon.Core.Services;

using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.World;

public interface IMapService
{
    IEnumerable<FileInfo> GetWorlds();
    void LoadWorld(FileInfo worldFile, string playerName, PlayerAbilities abilities);
    void LoadWorld(FileInfo worldFile, IMapProvisioningCallback callback, string playerName, PlayerAbilities abilities);
}

public class MapService : IMapService
{
    public const string WorldFileExtension = ".world.dol";

    public static Direction GetDirection(double ax, double ay, double bx, double by)
    {
        var angle = Math.Atan2(by - ay, bx - ax);
        angle += Math.PI;
        angle /= Math.PI / 8;
        var halfQuarter = Convert.ToInt32(angle);
        halfQuarter %= 16;
        return (Direction)halfQuarter;
    }

    private readonly string _worldsPath;
    private readonly IPlayerService _playerService;
    private readonly IPositionUpdateHandler _positionUpdateHandler;

    public MapService(IPlayerService playerService, IPositionUpdateHandler positionUpdateHandler)
    {
        _playerService = playerService;
        _positionUpdateHandler = positionUpdateHandler;
        var mainPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon");
        _worldsPath = Path.Combine(mainPath, "Worlds");
        if (Directory.Exists(_worldsPath)) return;

        Directory.CreateDirectory(_worldsPath);

        // Install the baked worlds that ship with the game on first run.
        var shippedWorlds = Path.Combine(Environment.CurrentDirectory, "Worlds");
        if (!Directory.Exists(shippedWorlds)) return;
        foreach (var world in Directory.GetFiles(shippedWorlds, $"*{WorldFileExtension}"))
        {
            File.Copy(world, Path.Combine(_worldsPath, Path.GetFileName(world)), overwrite: true);
        }
    }

    public MapService(IPlayerService playerService) : this(playerService, new NoOpPositionUpdateHandler())
    {
    }

    public IEnumerable<FileInfo> GetWorlds()
    {
        return new DirectoryInfo(_worldsPath).GetFiles($"*{WorldFileExtension}");
    }

    public void LoadWorld(FileInfo worldFile, string playerName, PlayerAbilities abilities)
    {
        LoadWorld(worldFile, new NoOpMapProvisioningCallback(), playerName, abilities);
    }

    public void LoadWorld(FileInfo worldFile, IMapProvisioningCallback callback, string playerName,
        PlayerAbilities abilities)
    {
        callback.OnStatus("Loading world...");
        var world = DolWorldSerializer.Deserialize(File.ReadAllText(worldFile.FullName))
                    ?? throw new DolMapException("Failed to load world");
        callback.OnEvent($"Loaded world {world.Info.Name}");

        SaveGameService.CurrentWorld = world;

        // The world is pre-baked (City of Light, challenge ratings, locations all provisioned by
        // WorldForge). Only per-playthrough player placement happens here.
        PlacePlayer(callback, world, playerName, abilities);
    }

    private void PlacePlayer(IMapProvisioningCallback callback, DolWorld world, string playerName,
        PlayerAbilities abilities)
    {
        callback.OnStatus("Setting player position...");

        var cityOfLight = world.Burgs.First(b => b.IsCityOfLight);
        SaveGameService.Party = new Party
        {
            Cell = cityOfLight.Cell,
            Burg = cityOfLight.Id,
            Stamina = 1
        };
        var player = _playerService.SetPlayer(playerName, false, abilities);
        SaveGameService.CurrentPlayerId = player.Id;
        _positionUpdateHandler.OnPositionUpdated();

        callback.OnEvent($"Player position set to {cityOfLight.Name}");
    }
}
