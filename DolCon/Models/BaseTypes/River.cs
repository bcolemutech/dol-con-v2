namespace DolCon.Models.BaseTypes;

public class River
{
    public int i { get; set; }
    public int source { get; set; }
    public int mouth { get; set; }
    public int discharge { get; set; }
    public double length { get; set; }
    public double width { get; set; }
    public double widthFactor { get; set; }
    public int sourceWidth { get; set; }
    public int parent { get; set; }
    public List<int> cells { get; set; }
    public int basin { get; set; }
    public string name { get; set; }
    public string type { get; set; }
}
