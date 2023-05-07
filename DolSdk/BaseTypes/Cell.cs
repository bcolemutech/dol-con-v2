namespace DolSdk.BaseTypes;

public class Cell
{
    public int i { get; set; }
    public List<int> v { get; set; }
    public List<int> c { get; set; }
    public List<double> p { get; set; }
    public int g { get; set; }
    public int h { get; set; }
    public int area { get; set; }
    public int f { get; set; }
    public int t { get; set; }
    public int haven { get; set; }
    public int harbor { get; set; }
    public int fl { get; set; }
    public int r { get; set; }
    public int conf { get; set; }
    public int biome { get; set; }
    public int s { get; set; }
    public decimal pop { get; set; }
    public int culture { get; set; }
    public int burg { get; set; }
    public int road { get; set; }
    public int crossroad { get; set; }
    public int state { get; set; }
    public int religion { get; set; }
    public int province { get; set; }
    public List<Cell> cells { get; set; }
    public List<object> features { get; set; }
    public List<Culture> cultures { get; set; }
    public List<Burg> burgs { get; set; }
    public List<State> states { get; set; }
    public List<object> provinces { get; set; }
    public List<Religion> religions { get; set; }
    public List<River> rivers { get; set; }
    public List<Marker> markers { get; set; }
}
