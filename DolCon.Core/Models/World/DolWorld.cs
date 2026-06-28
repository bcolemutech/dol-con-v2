namespace DolCon.Core.Models.World;

/// <summary>
/// Canonical, baked "Dominion of Light" world (<c>world.dol</c>). Produced once by WorldForge from
/// an Azgaar export and loaded by the game instead of re-provisioning at runtime. Contains the
/// Azgaar subset the game actually reads, the provisioning that used to be re-rolled each play
/// (City of Light, challenge ratings, locations, burg sizes), and reserved enrichment containers.
/// Per-playthrough progress (exploration/discovery) is NOT stored here — that lives in the save game.
/// See <c>docs/WORLD_DOL_FORMAT.md</c>.
/// </summary>
public class DolWorld
{
    /// <summary>Format version on the root so baked worlds can evolve without breaking. Starts at 1.</summary>
    public int SchemaVersion { get; set; } = 1;

    public WorldInfo Info { get; set; } = new();
    public WorldCoords Coords { get; set; } = new();

    /// <summary>Shared vertex pool; cells reference these by index for polygon rendering.</summary>
    public List<WorldVertex> Vertices { get; set; } = new();

    public List<WorldCell> Cells { get; set; } = new();
    public List<WorldBurg> Burgs { get; set; } = new();
    public List<WorldState> States { get; set; } = new();
    public List<WorldProvince> Provinces { get; set; } = new();
    public List<WorldRiver> Rivers { get; set; } = new();

    /// <summary>Biome-name lookup table; cell <c>Biome</c> indexes into this list.</summary>
    public List<string> Biomes { get; set; } = new();

    /// <summary>World-level reserved enrichment (global history, NPCs). Optional; omitted when null.</summary>
    public Enrichment? Enrichment { get; set; }
}

/// <summary>Provenance/metadata for a baked world.</summary>
public class WorldInfo
{
    public string Name { get; set; } = null!;
    public string? SourceSeed { get; set; }
    public string? SourceAzgaarVersion { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? GeneratorVersion { get; set; }

    /// <summary>
    /// Seed used by the deterministic provisioner for this bake. Stored so a world is reproducible:
    /// re-baking the same Azgaar export with this seed yields identical content.
    /// </summary>
    public int ProvisioningSeed { get; set; }
}

/// <summary>Geographic bounds carried over from the Azgaar export (reserved for future rendering).</summary>
public class WorldCoords
{
    public double LatT { get; set; }
    public double LatN { get; set; }
    public double LatS { get; set; }
    public double LonT { get; set; }
    public double LonW { get; set; }
    public double LonE { get; set; }
}

/// <summary>A map vertex. Only the coordinate pair is needed for polygon rendering.</summary>
public class WorldVertex
{
    /// <summary>Coordinate pair [x, y].</summary>
    public double[] P { get; set; } = System.Array.Empty<double>();
}
