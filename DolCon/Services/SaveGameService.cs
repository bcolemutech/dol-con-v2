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
    public static Map CurrentMap { get; set; } = new();
    public static Party Party { get; set; } = new();
    public static Guid CurrentPlayerId { get; set; } = Guid.NewGuid();
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
        var saveGamePath = Path.Combine(_savesPath, $"{CurrentMap.info.mapName}.{saveName}.json");
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
            map = await JsonSerializer.DeserializeAsync<Map>(File.OpenRead(saveFile.FullName));
            AnsiConsole.MarkupLine("Loaded game from [yellow]{0}[/]", saveFile.FullName);
        });
        
        CurrentMap = map ?? throw new DolSaveGameException("Failed to load game");
        Party = CurrentMap.Party;
        CurrentPlayerId = CurrentMap.CurrentPlayerId;
    }
}
