namespace DolCon.Models.BaseTypes;

public class Settings
{
    public string distanceUnit { get; set; } = null!;
    public string distanceScale { get; set; } = null!;
    public string areaUnit { get; set; } = null!;
    public string heightUnit { get; set; } = null!;
    public string heightExponent { get; set; } = null!;
    public string temperatureScale { get; set; } = null!;
    public string barSize { get; set; } = null!;
    public string barLabel { get; set; } = null!;
    public string barBackOpacity { get; set; } = null!;
    public string barBackColor { get; set; } = null!;
    public string barPosX { get; set; } = null!;
    public string barPosY { get; set; } = null!;
    public int populationRate { get; set; }
    public int urbanization { get; set; }
    public string mapSize { get; set; } = null!;
    public string latitudeO { get; set; } = null!;
    public string temperatureEquator { get; set; } = null!;
    public string temperaturePole { get; set; } = null!;
    public string prec { get; set; } = null!;
    public Options options { get; set; } = null!;
    public string mapName { get; set; } = null!;
    public bool hideLabels { get; set; }
    public string stylePreset { get; set; } = null!;
    public bool rescaleLabels { get; set; }
    public int urbanDensity { get; set; }
}
