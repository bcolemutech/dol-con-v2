namespace DolCon.Core.Services;

using System.Reflection;
using DolCon.Core.Models;
using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Models.World;

public interface IWorldBaker
{
    /// <summary>
    /// Provisions a deserialized Azgaar map (deterministically, given <paramref name="seed"/>) and
    /// maps it into a canonical <see cref="DolWorld"/>, dropping the Azgaar bulk the game never reads.
    /// </summary>
    DolWorld Bake(Map map, int seed, IMapProvisioningCallback callback);
}

/// <summary>
/// Turns a raw Azgaar export into a baked <see cref="DolWorld"/>. This is the reusable bake step
/// WorldForge drives; it lives in Core so it can be unit-tested and shares the provisioning logic.
/// </summary>
public class WorldBaker : IWorldBaker
{
    private readonly IWorldProvisioningService _provisioning;

    public WorldBaker(IWorldProvisioningService provisioning)
    {
        _provisioning = provisioning;
    }

    public WorldBaker() : this(new WorldProvisioningService())
    {
    }

    public DolWorld Bake(Map map, int seed, IMapProvisioningCallback callback)
    {
        _provisioning.Provision(map, seed, callback);

        callback.OnStatus("Mapping to world.dol...");
        var world = ToDolWorld(map, seed);

        var cityOfLight = map.Collections.burgs.FirstOrDefault(b => b.isCityOfLight)?.name ?? "(none)";
        var locationCount = world.Cells.Sum(c => c.Locations.Count) + world.Burgs.Sum(b => b.Locations.Count);
        callback.OnEvent(
            $"Baked '{world.Info.Name}': {world.Cells.Count} cells, {world.Burgs.Count} burgs, " +
            $"City of Light = {cityOfLight}, {locationCount} locations placed");

        return world;
    }

    private static DolWorld ToDolWorld(Map map, int seed)
    {
        return new DolWorld
        {
            SchemaVersion = 1,
            Info = new WorldInfo
            {
                Name = map.info?.mapName ?? "Unnamed World",
                SourceSeed = map.info?.seed,
                SourceAzgaarVersion = map.info?.version,
                GeneratedAt = DateTime.UtcNow,
                GeneratorVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                ProvisioningSeed = seed
            },
            Coords = ToCoords(map.coords),
            Vertices = map.vertices.Select(v => new WorldVertex { P = v.p.ToArray() }).ToList(),
            Biomes = map.biomes?.name?.ToList() ?? new List<string>(),
            Cells = map.Collections.cells.Select(ToCell).ToList(),
            Burgs = map.Collections.burgs.Select(ToBurg).ToList(),
            States = map.Collections.states.Select(ToState).ToList(),
            Provinces = map.Collections.provinces.Select(ToProvince).ToList(),
            Rivers = map.Collections.rivers?.Select(ToRiver).ToList() ?? new List<WorldRiver>()
        };
    }

    private static WorldCoords ToCoords(Coords c) => c is null
        ? new WorldCoords()
        : new WorldCoords { LatT = c.latT, LatN = c.latN, LatS = c.latS, LonT = c.lonT, LonW = c.lonW, LonE = c.lonE };

    private static WorldCell ToCell(Cell cell) => new()
    {
        Id = cell.i,
        VertexIndices = cell.v ?? new List<int>(),
        Neighbors = cell.c ?? new List<int>(),
        Center = cell.p?.ToArray() ?? Array.Empty<double>(),
        Area = cell.area,
        Pop = cell.pop,
        Biome = cell.biome,
        State = cell.state,
        Province = cell.province,
        Burg = cell.burg,
        ChallengeRating = cell.ChallengeRating,
        Locations = cell.locations.Select(ToLocation).ToList()
    };

    private static WorldBurg ToBurg(Burg burg) => new()
    {
        Id = burg.i ?? 0,
        Name = burg.name,
        Cell = burg.cell,
        Population = burg.population,
        Size = burg.size,
        IsCityOfLight = burg.isCityOfLight,
        X = burg.x,
        Y = burg.y,
        HasPort = burg.port == 1,
        HasCitadel = burg.citadel == 1,
        HasPlaza = burg.plaza == 1,
        HasWalls = burg.walls == 1,
        HasShanty = burg.shanty == 1,
        HasTemple = burg.temple == 1,
        Locations = burg.locations.Select(ToLocation).ToList()
    };

    private static WorldState ToState(State state) => new()
    {
        Id = state.i,
        Name = state.name,
        FullName = state.fullName
    };

    private static WorldProvince ToProvince(Province province) => new()
    {
        Id = province.i,
        Name = province.name,
        FullName = province.fullName
    };

    private static WorldRiver ToRiver(River river) => new()
    {
        Id = river.i,
        Name = river.name,
        Cells = river.cells ?? new List<int>(),
        Width = river.width,
        Length = river.length,
        Source = river.source,
        Mouth = river.mouth
    };

    private static WorldLocation ToLocation(Location location) => new()
    {
        Id = location.Id,
        Name = location.Name,
        TypeKey = location.Type.Type,
        Rarity = location.Rarity
    };
}
