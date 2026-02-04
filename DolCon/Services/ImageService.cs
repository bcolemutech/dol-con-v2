namespace DolCon.Services;

using System.Diagnostics;
using System.Xml.Linq;
using DolCon.Core.Services;

public interface IImageService
{
    void ProcessSvg();
    void OpenImage();
}

public class ImageService : IImageService
{
    private readonly string _mapsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon",
        "Maps");

    public void ProcessSvg()
    {
        var mapName = SaveGameService.CurrentMap.info?.mapName ?? "map";
        var imagePath = Directory.GetFiles(_mapsPath, $"{mapName}.svg").FirstOrDefault();

        if (File.Exists(imagePath))
        {
            var doc = XDocument.Load(imagePath);
            var svg = doc.Root;
            if (svg is null) return;
            var circles = svg.Elements();
            var current = circles.First(x => x.Attribute("id")?.Value == "current");
            current.Attribute("cx")?.SetValue(SaveGameService.CurrentCell.p[0]);
            current.Attribute("cy")?.SetValue(SaveGameService.CurrentCell.p[1]);
            doc.Save(imagePath);
        }
    }

    public void OpenImage()
    {
        var mapName = SaveGameService.CurrentMap.info?.mapName ?? "map";
        var imagePath = Directory.GetFiles(_mapsPath, $"{mapName}.svg").FirstOrDefault();

        if (File.Exists(imagePath))
        {
            using var fileOpener = new Process();
            fileOpener.StartInfo.FileName = "explorer";
            fileOpener.StartInfo.Arguments = imagePath;
            fileOpener.Start();
        }
    }
}
