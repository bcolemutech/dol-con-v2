namespace DolCon.Core.Models.BaseTypes;

public class Info
{
    public string version { get; set; } = null!;
    public string description { get; set; } = null!;
    public DateTime exportedAt { get; set; }
    public string mapName { get; set; } = null!;
    public string seed { get; set; } = null!;
    public long mapId { get; set; }
}
