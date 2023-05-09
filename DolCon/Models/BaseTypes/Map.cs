namespace DolCon.Models.BaseTypes;

using System.Text.Json.Serialization;

public class Map
{
    public Info info { get; set; }
    public Settings settings { get; set; }
    public Coords coords { get; set; }
    
    [JsonPropertyName("cells")]
    public MapCollections Collections { get; set; }
    public Biomes biomes { get; set; }
    public List<NameBasis> nameBases { get; set; }
    public Party Party { get; set; }
    public Guid CurrentPlayerId { get; set; }
}

public class MapCollections
{
    public List<Cell> cells { get; set; }
    public List<object> features { get; set; }
    public List<Culture> cultures { get; set; }
    public List<Burg> burgs { get; set; }
    public List<State> states { get; set; }
    public List<Province> provinces { get; set; }
    public List<Religion> religions { get; set; }
    public List<River> rivers { get; set; }
    public List<Marker> markers { get; set; }
}
