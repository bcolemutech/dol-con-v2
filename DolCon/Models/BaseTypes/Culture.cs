namespace DolCon.Models.BaseTypes;

public class Culture
{
    public string name { get; set; } = null!;
    public int i { get; set; }
    public int @base { get; set; }
    public List<int?> origins { get; set; } = null!;
    public string shield { get; set; } = null!;
    public int? center { get; set; }
    public string color { get; set; } = null!;
    public string type { get; set; } = null!;
    public double? expansionism { get; set; }
    public string code { get; set; } = null!;
}
