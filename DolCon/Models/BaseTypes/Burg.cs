namespace DolCon.Models.BaseTypes;

public class Burg
{
    public bool isCityOfLight { get; set; }
    public int cell { get; set; }
    public double? x { get; set; }
    public double? y { get; set; }
    public int? state { get; set; }
    public int? i { get; set; }
    public int? culture { get; set; }
    public string name { get; set; }
    public int? feature { get; set; }
    public int? capital { get; set; }
    public int? port { get; set; }
    public double population { get; set; } = 0;
    public string type { get; set; }
    public Coa coa { get; set; }
    public int? citadel { get; set; }
    public int? plaza { get; set; }
    public int? walls { get; set; }
    public int? shanty { get; set; }
    public int? temple { get; set; }
    public List<Location> locations { get; set; } = new List<Location>();
}
