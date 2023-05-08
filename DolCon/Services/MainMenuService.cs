namespace DolCon.Services;

using Spectre.Console;

public interface IMainMenuService
{
    Task Show(CancellationToken cancellationToken);
}

public class MainMenuService : IMainMenuService
{
    private readonly IMapService _mapService;
    private readonly ISaveGameService _saveGameService;

    public MainMenuService(IMapService mapService, ISaveGameService saveGameService)
    {
        _mapService = mapService;
        _saveGameService = saveGameService;
    }

    public async Task Show(CancellationToken cancellationToken)
    {
        var startSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("Start a new game or load a save?")
            .AddChoices("New game", "Load game")
        );
        
        switch (startSelection)
        {
            case "New game":
                await StartNewGame();
                break;
            case "Load game":
                await LoadGame();
                break;
        }
        System.Environment.Exit(0);
    }

    private async Task LoadGame()
    {
        var saves = _saveGameService.GetSaves();

        if (!saves.Any())
        {
            return;
        }

        var saveSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a save to load")
                .AddChoices(saves.Select(x => x.Name)
                ));

        AnsiConsole.MarkupLine("Loading save [yellow]{0}[/]", saveSelection);
        
        var saveFile = saves.First(x => x.Name == saveSelection);
        
        await _saveGameService.LoadGame(saveFile);
        
        AnsiConsole.WriteLine("Save loaded, starting game...");
    }

    private async Task StartNewGame()
    {
        var maps = _mapService.GetMaps();

        if (!maps.Any())
        {
            return;
        }

        var newGameSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a map to play on")
                .AddChoices(maps.Select(x => x.Name)
                ));

        AnsiConsole.MarkupLine("Starting a new game on map [yellow]{0}[/]", newGameSelection);
        
        var mapFile = maps.First(x => x.Name == newGameSelection);
        
        var map = await _mapService.LoadMap(mapFile);
        
        AnsiConsole.WriteLine("Map loaded, saving game...");
        
        var path = await _saveGameService.SaveGame(map);
        
        AnsiConsole.WriteLine("Game saved, loading game...");
        
        await _saveGameService.LoadGame(new FileInfo(path));
        
        AnsiConsole.WriteLine("Game loaded, starting game...");
    }
}
