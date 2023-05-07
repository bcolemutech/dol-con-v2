namespace DolCon.Services;

using System.Text.Json;
using Models.BaseTypes;
using Spectre.Console;
using Settings = Models.Settings;

public interface ISaveGameService
{
    Task SaveNewGame(Map map);
    IEnumerable<FileInfo> GetSaves();
    Task<Map> LoadGame(FileInfo saveFile);
}

public class SaveGameService : ISaveGameService
{
    private readonly Settings? _settings;
    private readonly string _savesPath;

    public SaveGameService()
    {
        var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon",
            "settings.json");
        AnsiConsole.WriteLine("Settings path: [yellow]{0}[/]", settingsPath);
        if (File.Exists(settingsPath))
        {
            AnsiConsole.WriteLine("Loading settings...");
            _settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath));
        }
        else
        {
            AnsiConsole.WriteLine("Settings not found, creating new settings...");
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath) ?? throw new InvalidOperationException());
            _settings = new Settings();
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(_settings));
        }

        _savesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon",
            "Saves");
        AnsiConsole.WriteLine("Saves path: [yellow]{0}[/]", _savesPath);
        if (Directory.Exists(_savesPath)) return;
        AnsiConsole.WriteLine("Creating saves directory...");
        Directory.CreateDirectory(_savesPath);
    }

    public async Task SaveNewGame(Map map)
    {
        await AnsiConsole.Status().StartAsync("Saving game...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("yellow"));
            var saveGamePath = Path.Combine(_savesPath, $"{map.info.mapName}.AutoSave.json");
            AnsiConsole.MarkupLine("Saving game to [yellow]{0}[/]", saveGamePath);
            await File.WriteAllTextAsync(saveGamePath, JsonSerializer.Serialize(map));
        });
    }

    public IEnumerable<FileInfo> GetSaves()
    {
        return new DirectoryInfo(_savesPath).GetFiles("*.json");
    }

    public async Task<Map> LoadGame(FileInfo saveFile)
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
        
        return map ?? throw new DolSaveGameException("Failed to load game");
    }
}

public class DolSaveGameException : Exception
{
    public DolSaveGameException(string message) : base(message)
    {
    }
}
