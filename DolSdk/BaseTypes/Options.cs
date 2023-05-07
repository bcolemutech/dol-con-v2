namespace DolSdk.BaseTypes;

public class Options
{
    public bool pinNotes { get; set; }
    public bool showMFCGMap { get; set; }
    public List<int> winds { get; set; }
    public string stateLabelsMode { get; set; }
    public int year { get; set; }
    public string era { get; set; }
    public string eraShort { get; set; }
    public List<Military> military { get; set; }
}