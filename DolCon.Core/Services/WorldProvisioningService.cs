namespace DolCon.Core.Services;

using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.BaseTypes;

public interface IWorldProvisioningService
{
    /// <summary>
    /// Provisions a freshly-deserialized Azgaar map in place: flags the City of Light, computes
    /// per-cell challenge ratings, adjusts burg sizes, and places cell/burg locations. Deterministic
    /// for a given <paramref name="seed"/> — the same input + seed always produces the same result.
    /// </summary>
    void Provision(Map map, int seed, IMapProvisioningCallback callback);
}

/// <summary>
/// The single home for world provisioning. Lifted out of <see cref="MapService"/> so both the game
/// (which still loads raw Azgaar until Phase 2) and WorldForge (the baker) share one implementation.
/// All randomness flows through one seeded <see cref="Random"/> so a bake is reproducible.
/// </summary>
public class WorldProvisioningService : IWorldProvisioningService
{
    public void Provision(Map map, int seed, IMapProvisioningCallback callback)
    {
        var rng = new Random(seed);

        callback.OnStatus("Identifying City of Light...");

        var topPop = map.Collections.burgs.Max(x => x.population);
        var cityOfLight = map.Collections.burgs.First(x => Math.Abs(x.population - topPop) < 0.01);
        cityOfLight.isCityOfLight = true;

        foreach (var type in LocationTypes.Types.Where(x => x.NeedsCityOfLight))
        {
            cityOfLight.locations.Add(new Location
                { Id = NewDeterministicGuid(rng), Type = type, Name = type.Type, Rarity = type.Rarity });
        }

        callback.OnEvent($"City of Light established as {cityOfLight.name}");

        var colCell = map.Collections.cells.First(x => x.i == cityOfLight.cell);
        var colX = colCell.p[0];
        var colY = colCell.p[1];

        callback.OnStatus("Provisioning cells...");

        var cellTypes = LocationTypes.Types.Where(x => !x.IsBurgLocation).ToList();
        var crDistance = map.Collections.cells.Max(x => x.p.Max()) / 2;

        foreach (var cell in map.Collections.cells)
        {
            cell.locations.AddRange(ProvisionCellLocations(cell, cellTypes, rng));
            cell.ChallengeRating = CalculateChallengeRating(cell, colX, colY, crDistance);
        }

        callback.OnStatus("Provisioning burgs...");

        var minPop = map.Collections.burgs.Min(x => x.population);
        var burgTypes = LocationTypes.Types.Where(x => x is
            { NeedsCityOfLight: false, IsBurgLocation: true, NeedsPort: false, NeedsTemple: false }).ToList();

        foreach (var burg in map.Collections.burgs)
        {
            AdjustBurgSize(burg, minPop);
            burg.locations.AddRange(ProvisionBurgLocations(burg, burgTypes, rng));
        }

        callback.OnEvent("World provisioning complete");
    }

    /// <summary>
    /// Calculates the challenge rating for a cell based on its distance from the City of Light.
    /// Cells closer to the City of Light have lower CRs; the rating scales 0–20 by distance ratio
    /// and is rounded to the nearest 1/8th (0.125).
    /// </summary>
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

    private static IEnumerable<Location> ProvisionCellLocations(Cell cell, List<LocationType> cellTypes, Random rng)
    {
        var locations = new List<Location>();

        var wilderness = cellTypes.Where(x => x.isWild).ToList();
        var nonWilderness = cellTypes.Where(x => !x.isWild).ToList();

        var (nonWildCells, wildCells) = cell switch
        {
            { CellSize: CellSize.small, PopDensity: PopDensity.wild } => (1, 4),
            { CellSize: CellSize.small, PopDensity: PopDensity.rural } => (2, 3),
            { CellSize: CellSize.small, PopDensity: PopDensity.urban } => (3, 2),
            { CellSize: CellSize.large, PopDensity: PopDensity.wild } => (2, 6),
            { CellSize: CellSize.large, PopDensity: PopDensity.rural } => (3, 5),
            { CellSize: CellSize.large, PopDensity: PopDensity.urban } => (5, 3),
            _ => throw new ArgumentOutOfRangeException(nameof(cell))
        };

        var i = 0;
        while (i < nonWildCells)
        {
            var location = nonWilderness[rng.Next(nonWilderness.Count)];
            if (!location.AllowMultiple && locations.Any(x => x.Type == location))
            {
                continue;
            }

            locations.Add(new Location
                { Id = NewDeterministicGuid(rng), Type = location, Name = location.Type, Rarity = location.Rarity });
            i++;
        }

        i = 0;
        while (i < wildCells)
        {
            var location = wilderness[rng.Next(wilderness.Count)];
            if (!location.AllowMultiple && locations.Any(x => x.Type == location))
            {
                continue;
            }

            locations.Add(new Location
                { Id = NewDeterministicGuid(rng), Type = location, Name = location.Type, Rarity = location.Rarity });
            i++;
        }

        return locations;
    }

    private static List<Location> ProvisionBurgLocations(Burg burg, List<LocationType> burgTypes, Random rng)
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
                    locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = pier, Name = "Pier", Rarity = pier.Rarity });
                    break;
                case BurgSize.Town:
                    var dock = LocationTypes.Types.First(x => x.Type == "dock");
                    locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = dock, Name = $"{burg.name} Docks", Rarity = dock.Rarity });
                    break;
                case BurgSize.City:
                    var harbor = LocationTypes.Types.First(x => x.Type == "harbor");
                    locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = harbor, Name = $"{burg.name} Harbor", Rarity = harbor.Rarity });
                    break;
                case BurgSize.Metropolis:
                case BurgSize.Megalopolis:
                    var port = LocationTypes.Types.First(x => x.Type == "port");
                    locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = port, Name = $"{burg.name} Port", Rarity = port.Rarity });
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
                    locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = shrine, Name = $"{burg.name} Shrine", Rarity = shrine.Rarity });
                    break;
                case BurgSize.City:
                case BurgSize.Metropolis:
                    var temple = LocationTypes.Types.First(x => x.Type == "temple");
                    locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = temple, Name = $"{burg.name} Temple", Rarity = temple.Rarity });
                    break;
                case BurgSize.Megalopolis:
                    var basilica = LocationTypes.Types.First(x => x.Type == "basilica");
                    locations.Add(new Location
                        { Id = NewDeterministicGuid(rng), Type = basilica, Name = $"{burg.name} Basilica", Rarity = basilica.Rarity });
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
                    locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = manor, Name = $"{burg.name} Manor", Rarity = manor.Rarity });
                    break;
                case BurgSize.City:
                    var castle = LocationTypes.Types.First(x => x.Type == "castle");
                    locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = castle, Name = $"{burg.name} Castle", Rarity = castle.Rarity });
                    break;
                case BurgSize.Metropolis:
                    var fortress = LocationTypes.Types.First(x => x.Type == "fortress");
                    locations.Add(new Location
                        { Id = NewDeterministicGuid(rng), Type = fortress, Name = $"{burg.name} Fortress", Rarity = fortress.Rarity });
                    break;
                case BurgSize.Megalopolis:
                    var citadel = LocationTypes.Types.First(x => x.Type == "citadel");
                    locations.Add(new Location
                        { Id = NewDeterministicGuid(rng), Type = citadel, Name = $"{burg.name} Citadel", Rarity = citadel.Rarity });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(burg));
            }
        }

        var cnt = burgTypes.Count - 1;
        for (var i = 0; i < 4; i++)
        {
            var j = 0;
            while (j < sequence[i])
            {
                var rarity = (Rarity)i;
                var locationType = burgTypes[rng.Next(0, cnt)];
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

                locations.Add(new Location
                {
                    Id = NewDeterministicGuid(rng),
                    Type = locationType,
                    Name = $"{burg.name} {locationType.Type}",
                    Rarity = rarity
                });
                j++;
            }
        }

        if (locations.Count == 0)
        {
            var tavern = LocationTypes.Types.First(x => x.Type == "tavern");
            locations.Add(new Location { Id = NewDeterministicGuid(rng), Type = tavern, Name = $"{burg.name} Tavern", Rarity = tavern.Rarity });
        }

        return locations;
    }

    /// <summary>Draws 16 bytes from the seeded RNG to build a reproducible GUID (replaces Guid.NewGuid()).</summary>
    private static Guid NewDeterministicGuid(Random rng)
    {
        var bytes = new byte[16];
        rng.NextBytes(bytes);
        return new Guid(bytes);
    }
}
