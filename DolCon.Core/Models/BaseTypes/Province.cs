namespace DolCon.Core.Models.BaseTypes;

public class Province
{
    public int i { get; set; }
    public int state { get; set; }
    public int center { get; set; }
    public int burg { get; set; }
    public string name { get; set; } = null!;
    public string formName { get; set; } = null!;
    public string fullName { get; set; } = null!;
    public string color { get; set; } = null!;
    public Coa coa { get; set; } = null!;
}
