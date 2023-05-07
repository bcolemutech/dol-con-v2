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
            .AddChoices(new[] { "New game", "Load game" })
        );
        
        if (startSelection == "New game")
        {
            await StartNewGame();
        }
        else if (startSelection == "Load game")
        {
            AnsiConsole.WriteLine("Loading a save...");
        }
        System.Environment.Exit(0);
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
        
        await _saveGameService.SaveNewGame(map);
        
        AnsiConsole.WriteLine("Game saved, starting game...");
    }
}
