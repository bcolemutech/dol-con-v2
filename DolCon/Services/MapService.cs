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
        AnsiConsole.WriteLine("Create a map using Azgaar's Fantasy Map Generator (https://azgaar.github.io/Fantasy-Map-Generator/).");
        AnsiConsole.WriteLine("Once you have created a map, export it as a JSON file and place it in the maps directory.");
        
        return new List<FileInfo>();
    }

    public async Task<Map> LoadMap(FileInfo mapFile)
    {
        Map map = new();
        await AnsiConsole.Status().StartAsync("Loading map...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));
            ctx.Refresh();
            map = await DeserializeMap(mapFile) ?? throw new DolMapException("Failed to load map");
            AnsiConsole.MarkupLine("Loaded map [yellow]{0}[/]", mapFile.Name);
            ctx.Status("Identifying City of Light...");
            ctx.Refresh();

            var topPop = map.cells.burgs.Max(x => x.population);
            var cityOfLight = map.cells.burgs.First(x => Math.Abs(x.population - topPop) < 0.01);
            cityOfLight.isCityOfLight = true;
        });
        return map;
    }

    private static async Task<Map?> DeserializeMap(FileSystemInfo mapFile)
    {
        var stream = File.OpenRead(mapFile.FullName);
        return await JsonSerializer.DeserializeAsync<Map>(stream);
    }
}

public class DolMapException : Exception
{
    public DolMapException(string message) : base(message)
    {
    }
}
