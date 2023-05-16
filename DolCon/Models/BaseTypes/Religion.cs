namespace DolCon.Models.BaseTypes;

public class Religion
{
    public string name { get; set; }
    public int i { get; set; }
    public List<int> origins { get; set; }
    public string type { get; set; }
    public string form { get; set; }
    public int? culture { get; set; }
    public int? center { get; set; }
    public string deity { get; set; }
    public string expansion { get; set; }
    public double? expansionism { get; set; }
    public string color { get; set; }
    public string code { get; set; }
}
