namespace DolCon.Models.BaseTypes;

public class State
{
    public int i { get; set; }
    public string name { get; set; }
    public double urban { get; set; }
    public double rural { get; set; }
    public int burgs { get; set; }
    public int area { get; set; }
    public int cells { get; set; }
    public List<int> neighbors { get; set; }
    public List<object> diplomacy { get; set; }
    public List<int> provinces { get; set; }
    public string color { get; set; }
    public double? expansionism { get; set; }
    public int? capital { get; set; }
    public string type { get; set; }
    public int? center { get; set; }
    public int? culture { get; set; }
    public Coa coa { get; set; }
    public List<Campaign> campaigns { get; set; }
    public string form { get; set; }
    public string formName { get; set; }
    public string? fullName { get; set; }
    public List<double> pole { get; set; }
    public double? alert { get; set; }
    public List<Military> military { get; set; }
}
