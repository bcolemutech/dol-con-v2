namespace DolCon.Services;

using System.Text.Json;
using DolSdk.BaseTypes;
using Spectre.Console;

public interface IMapService
{
    IEnumerable<FileInfo> GetMaps();
    Task<Map> LoadMap(FileInfo mapFile);
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

    public async Task<Map> LoadMap(FileInfo mapFile)
    {
        Map? map = null;
        await AnsiConsole.Status().StartAsync("Loading map...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));
            var stream = File.OpenRead(mapFile.FullName);
            map = await JsonSerializer.DeserializeAsync<Map>(stream);
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
