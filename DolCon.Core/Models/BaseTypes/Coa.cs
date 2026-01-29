namespace DolCon.Core.Models.BaseTypes;

public class Coa
{
    public string t1 { get; set; } = null!;
    public List<Charge> charges { get; set; } = null!;
    public string shield { get; set; } = null!;
    public Division division { get; set; } = null!;
    public List<Ordinary> ordinaries { get; set; } = null!;
}
