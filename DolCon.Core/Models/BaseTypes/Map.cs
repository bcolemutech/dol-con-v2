namespace DolCon.Core.Models.BaseTypes;

using System.Text.Json.Serialization;
using Newtonsoft.Json;

public class Map
{
    public Info? info { get; set; }
    public Settings settings { get; set; } = null!;
    public Coords coords { get; set; } = null!;

    [JsonPropertyName("cells")]
    public MapCollections Collections { get; set; } = null!;
    public List<MapVertex> vertices { get; set; } = new();
    public Biomes biomes { get; set; } = null!;
    public List<NameBasis> nameBases { get; set; } = new();
    public Party Party { get; set; } = null!;
    public Guid CurrentPlayerId { get; set; }
}

public class MapVertex
{
    public List<double> p { get; set; } = null!;
    public List<int> v { get; set; } = null!;
    public List<int> c { get; set; } = null!;
}

public class MapCollections
{
    public List<Cell> cells { get; set; } = null!;
    public List<Culture> cultures { get; set; } = null!;
    public List<Burg> burgs { get; set; } = null!;
    public List<State> states { get; set; } = null!;
    public List<Province> provinces { get; set; } = null!;
    public List<River> rivers { get; set; } = null!;
}
