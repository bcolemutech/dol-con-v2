namespace DolSdk.BaseTypes;

public class Settings
{
    public string distanceUnit { get; set; }
    public string distanceScale { get; set; }
    public string areaUnit { get; set; }
    public string heightUnit { get; set; }
    public string heightExponent { get; set; }
    public string temperatureScale { get; set; }
    public string barSize { get; set; }
    public string barLabel { get; set; }
    public string barBackOpacity { get; set; }
    public string barBackColor { get; set; }
    public string barPosX { get; set; }
    public string barPosY { get; set; }
    public int populationRate { get; set; }
    public int urbanization { get; set; }
    public string mapSize { get; set; }
    public string latitudeO { get; set; }
    public string temperatureEquator { get; set; }
    public string temperaturePole { get; set; }
    public string prec { get; set; }
    public Options options { get; set; }
    public string mapName { get; set; }
    public bool hideLabels { get; set; }
    public string stylePreset { get; set; }
    public bool rescaleLabels { get; set; }
    public int urbanDensity { get; set; }
}