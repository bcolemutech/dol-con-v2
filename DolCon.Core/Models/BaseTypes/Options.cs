namespace DolCon.Core.Models.BaseTypes;

public class Options
{
    public bool pinNotes { get; set; }
    public bool showMFCGMap { get; set; }
    public List<int> winds { get; set; } = null!;
    public string stateLabelsMode { get; set; } = null!;
    public int year { get; set; }
    public string era { get; set; } = null!;
    public string eraShort { get; set; } = null!;
    public List<Military> military { get; set; } = null!;
}
