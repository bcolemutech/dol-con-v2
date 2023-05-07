namespace DolCon.Services;

using System.Text.Json;
using DolSdk.BaseTypes;
using Spectre.Console;

public interface IMapService
{
    IEnumerable<FileInfo> GetMaps();
    Map LoadMap(FileInfo mapFile);
}

public class MapService : IMapService
{
    private readonly string _mapsPath;

    public MapService()
    {
        _mapsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon", "Maps");
        if (Directory.Exists(_mapsPath)) return;
        
        AnsiConsole.WriteLine("Creating maps directory...");
        Directory.CreateDirectory(_mapsPath);
    }

    public IEnumerable<FileInfo> GetMaps()
    {
        var maps = new DirectoryInfo(_mapsPath).GetFiles("*.json");
        if (maps.Any())
        {
            return maps;
        }

        AnsiConsole.MarkupLine("[red bold]No maps found![/]");
        AnsiConsole.WriteLine("Create a map using Azgaars Fantasy Map Generator (https://azgaar.github.io/Fantasy-Map-Generator/).");
        AnsiConsole.WriteLine("Once you have created a map, export it as a JSON file and place it in the maps directory.");
        
        return new List<FileInfo>();
    }

    public Map LoadMap(FileInfo mapFile)
    {
        Map? map = null;
        AnsiConsole.Status().Start("Loading map...", ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));

            map = JsonSerializer.Deserialize<Map>(File.ReadAllText(mapFile.FullName));
        });
        return map ?? throw new DolMapException("Failed to load map");
    }
}

public class DolMapException : Exception
{
    public DolMapException(string message) : base(message)
    {
    }
}
