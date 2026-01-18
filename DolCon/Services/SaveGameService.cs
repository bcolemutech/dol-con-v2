namespace DolCon.Services;

using System.Text.Json;
using Models;
using Models.BaseTypes;
using Spectre.Console;

public interface ISaveGameService
{
    Task<string> SaveGame(string saveName = "AutoSave");
    IEnumerable<FileInfo> GetSaves();
    Task LoadGame(FileInfo saveFile);
}

public class SaveGameService : ISaveGameService
{
    public static Location? CurrentLocation =>
        Party switch
        {
            { Location: not null, Burg: not null } => CurrentMap.Collections.burgs[Party.Burg.Value]
                .locations.First(x => x.Id == Party.Location.Value),
            { Location: not null } => CurrentMap.Collections.cells[Party.Cell]
                .locations.First(x => x.Id == Party.Location.Value),
            _ => null
        };
    public static Map CurrentMap { get; set; } = new();
    public static Party Party { get; set; } = new();
    public static Guid CurrentPlayerId { get; set; } = Guid.NewGuid();
    
    public static Cell CurrentCell => CurrentMap.Collections.cells[Party.Cell];
    
    public static Burg? CurrentBurg => Party.Burg.HasValue ? CurrentMap.Collections.burgs[Party.Burg.Value] : null;
    
    public static Province CurrentProvince => CurrentMap.Collections.provinces[CurrentCell.province];
    
    public static State CurrentState => CurrentMap.Collections.states[CurrentCell.state];
    
    public static string CurrentBiome => CurrentMap.biomes.name[CurrentCell.biome];
    
    private readonly string _savesPath;

    public SaveGameService()
    {
        _savesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon",
            "Saves");
        AnsiConsole.WriteLine("Saves path: [yellow]{0}[/]", _savesPath);
        if (Directory.Exists(_savesPath)) return;
        AnsiConsole.WriteLine("Creating saves directory...");
        Directory.CreateDirectory(_savesPath);
    }

    public async Task<string> SaveGame(string saveName = "AutoSave")
    {
        var saveGamePath = Path.Combine(_savesPath, $"{CurrentMap.info?.mapName ?? "unknown"}.{saveName}.json");
        await AnsiConsole.Status().StartAsync("Saving game...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));
            AnsiConsole.MarkupLine("Saving game to [yellow]{0}[/]", saveGamePath);
            CurrentMap.Party = Party;
            CurrentMap.CurrentPlayerId = CurrentPlayerId;
            await File.WriteAllTextAsync(saveGamePath, JsonSerializer.Serialize(CurrentMap));
        });
        return saveGamePath;
    }

    public IEnumerable<FileInfo> GetSaves()
    {
        return new DirectoryInfo(_savesPath).GetFiles("*.json");
    }

    public async Task LoadGame(FileInfo saveFile)
    {
        Map? map = null;
        await AnsiConsole.Status().StartAsync("Loading game...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));
            AnsiConsole.MarkupLine("Loading game from [yellow]{0}[/]", saveFile.FullName);
            var fileStream = File.OpenRead(saveFile.FullName);
            map = await JsonSerializer.DeserializeAsync<Map>(fileStream);
            fileStream.Close();
            AnsiConsole.MarkupLine("Loaded game from [yellow]{0}[/]", saveFile.FullName);
        });
        
        CurrentMap = map ?? throw new DolSaveGameException("Failed to load game");
        Party = CurrentMap.Party;
        CurrentPlayerId = CurrentMap.CurrentPlayerId;
    }

    public static Cell GetCell(int cellId)
    {
        return CurrentMap.Collections.cells[cellId];
    }

    public static Burg? GetBurg(int cellBurg)
    {
        return CurrentMap.Collections.burgs.Find(x => x.i == cellBurg);
    }

    public static string GetBiome(int cellBiome)
    {
        return CurrentMap.biomes.name[cellBiome];
    }

    public static Province GetProvince(int cellProvince)
    {
        return CurrentMap.Collections.provinces[cellProvince];
    }

    public static State GetState(int cellState)
    {
        return CurrentMap.Collections.states[cellState];
    }
    
}
