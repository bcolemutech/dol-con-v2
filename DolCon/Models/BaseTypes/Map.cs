namespace DolCon.Models.BaseTypes;

public class Map
{
    public Info info { get; set; }
    public Settings settings { get; set; }
    public Coords coords { get; set; }
    public Cell cells { get; set; }
    public Biomes biomes { get; set; }
    public List<NameBasis> nameBases { get; set; }
    public Party Party { get; set; }
    public Guid CurrentPlayerId { get; set; }
}
