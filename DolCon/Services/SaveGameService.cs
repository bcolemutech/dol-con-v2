using System.Text.Json;
using DolCon.Services;
using Spectre.Console;

public interface ISaveGameService
{
}

public class SaveGameService : ISaveGameService
{
    private readonly Settings? _settings;
    private readonly string _settingsPath;
    
    public SaveGameService()
    {
        _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DolCon", "settings.json");
        AnsiConsole.WriteLine("Settings path: [yellow]{0}[/]", _settingsPath);
        if (File.Exists(_settingsPath))
        {
            AnsiConsole.WriteLine("Loading settings...");
            _settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_settingsPath));
        }
        else
        {
            AnsiConsole.WriteLine("Settings not found, creating new settings...");
            _settings = new Settings();
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings));
        }
    }
}
