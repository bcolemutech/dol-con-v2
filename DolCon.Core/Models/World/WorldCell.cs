namespace DolCon.Core.Models.World;

using System.Text.Json.Serialization;
using DolCon.Core.Enums;

/// <summary>
/// A Voronoi cell of the world. Holds the Azgaar geometry the game renders/navigates plus the
/// baked challenge rating and generated locations that used to be re-rolled at runtime.
/// </summary>
public class WorldCell
{
    public int Id { get; set; }

    /// <summary>Indices into <see cref="DolWorld.Vertices"/> forming this cell's polygon (Azgaar <c>v</c>).</summary>
    public List<int> VertexIndices { get; set; } = new();

    /// <summary>Neighboring cell ids (Azgaar <c>c</c>).</summary>
    public List<int> Neighbors { get; set; } = new();

    /// <summary>Cell center [x, y] (Azgaar <c>p</c>).</summary>
    public double[] Center { get; set; } = System.Array.Empty<double>();

    /// <summary>Cell area; drives the derived <c>CellSize</c>.</summary>
    public int Area { get; set; }

    /// <summary>Population; drives the derived <c>PopDensity</c>.</summary>
    public decimal Pop { get; set; }

    /// <summary>Biome index into <see cref="DolWorld.Biomes"/>.</summary>
    public int Biome { get; set; }

    public int State { get; set; }
    public int Province { get; set; }

    /// <summary>Burg id located in this cell, or 0 if none.</summary>
    public int Burg { get; set; }

    /// <summary>Baked encounter difficulty (was computed from distance to the City of Light).</summary>
    public double ChallengeRating { get; set; }

    /// <summary>Baked, generated locations for this cell.</summary>
    public List<WorldLocation> Locations { get; set; } = new();

    /// <summary>
    /// Per-playthrough exploration progress (0–1). Runtime/save state, not canonical world data —
    /// omitted from a freshly baked world.dol (it's 0) and only written into save files once explored.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double ExploredPercent { get; set; }

    /// <summary>Reserved enrichment for this cell. Optional; omitted when null.</summary>
    public Enrichment? Enrichment { get; set; }

    /// <summary>Biome enum derived from the <see cref="Biome"/> index (movement cost, map colours).</summary>
    [JsonIgnore]
    public Biome BiomeType => (Biome)Biome;

    /// <summary>Size bucket derived from <see cref="Area"/> (drives exploration/movement maths).</summary>
    [JsonIgnore]
    public CellSize CellSize => Area < 100 ? CellSize.small : CellSize.large;

    /// <summary>Population density derived from <see cref="Pop"/>.</summary>
    [JsonIgnore]
    public PopDensity PopDensity => (Pop * 1000) switch
    {
        < (int)PopDensity.rural => PopDensity.wild,
        < (int)PopDensity.urban => PopDensity.rural,
        _ => PopDensity.urban
    };
}
