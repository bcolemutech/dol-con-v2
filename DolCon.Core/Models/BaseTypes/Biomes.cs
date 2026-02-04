namespace DolCon.Core.Models.BaseTypes;

public class Biomes
{
    public List<int> i { get; set; } = null!;
    public List<string> name { get; set; } = null!;
    public List<string> color { get; set; } = null!;
    public List<BiomesMatrix> biomesMartix { get; set; } = null!;
    public List<int> habitability { get; set; } = null!;
    public List<int> iconsDensity { get; set; } = null!;
    public List<List<string>> icons { get; set; } = null!;
    public List<int> cost { get; set; } = null!;
}
