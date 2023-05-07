namespace DolCon.Models.BaseTypes;

public class Biomes
{
    public List<int> i { get; set; }
    public List<string> name { get; set; }
    public List<string> color { get; set; }
    public List<BiomesMatrix> biomesMartix { get; set; }
    public List<int> habitability { get; set; }
    public List<int> iconsDensity { get; set; }
    public List<List<string>> icons { get; set; }
    public List<int> cost { get; set; }
}
