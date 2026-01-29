namespace DolCon.Core.Models.BaseTypes;

public class State
{
    public int i { get; set; }
    public string name { get; set; } = null!;
    public double urban { get; set; }
    public double rural { get; set; }
    public int burgs { get; set; }
    public int area { get; set; }
    public int cells { get; set; }
    public List<int> neighbors { get; set; } = null!;
    public List<object> diplomacy { get; set; } = null!;
    public List<int> provinces { get; set; } = null!;
    public string color { get; set; } = null!;
    public double? expansionism { get; set; }
    public int? capital { get; set; }
    public string type { get; set; } = null!;
    public int? center { get; set; }
    public int? culture { get; set; }
    public Coa coa { get; set; } = null!;
    public List<Campaign> campaigns { get; set; } = null!;
    public string form { get; set; } = null!;
    public string formName { get; set; } = null!;
    public string? fullName { get; set; }
    public List<double> pole { get; set; } = null!;
    public double? alert { get; set; }
    public List<Military> military { get; set; } = null!;
}
