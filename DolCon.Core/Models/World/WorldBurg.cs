namespace DolCon.Core.Models.World;

using Enums;

/// <summary>
/// A settlement. Population and <see cref="Size"/> are the baked (adjusted) values, and
/// <see cref="IsCityOfLight"/> plus the generated locations are baked rather than re-rolled.
/// The feature booleans are converted from Azgaar's nullable 0/1 flags.
/// </summary>
public class WorldBurg
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>Id of the cell this burg sits in.</summary>
    public int Cell { get; set; }

    /// <summary>Adjusted population (baked).</summary>
    public double Population { get; set; }

    /// <summary>Size category derived from the adjusted population (baked).</summary>
    public BurgSize Size { get; set; }

    /// <summary>Whether this is the world's single City of Light (baked).</summary>
    public bool IsCityOfLight { get; set; }

    public double? X { get; set; }
    public double? Y { get; set; }

    public bool HasPort { get; set; }
    public bool HasCitadel { get; set; }
    public bool HasPlaza { get; set; }
    public bool HasWalls { get; set; }
    public bool HasShanty { get; set; }
    public bool HasTemple { get; set; }

    /// <summary>Baked, generated locations for this burg.</summary>
    public List<WorldLocation> Locations { get; set; } = new();

    /// <summary>Reserved enrichment for this burg. Optional; omitted when null.</summary>
    public Enrichment? Enrichment { get; set; }
}
