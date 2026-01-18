namespace DolCon.Services;

using Spectre.Console;
using Views;

public interface IMainMenuService
{
    Task Show(CancellationToken cancellationToken);
}

public class MainMenuService : IMainMenuService
{
    private readonly IMapService _mapService;
    private readonly ISaveGameService _saveService;
    private readonly IGameService _gameService;

    public MainMenuService(IMapService mapService, ISaveGameService saveService, IGameService gameService)
    {
        _mapService = mapService;
        _saveService = saveService;
        _gameService = gameService;
    }

    public async Task Show(CancellationToken cancellationToken)
    {
        var startSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Start a new game or load a save?")
                .AddChoices("New game", "Load game", "Exit")
        );

        switch (startSelection)
        {
            case "New game":
                await StartNewGame(cancellationToken);
                break;
            case "Load game":
                await LoadGame(cancellationToken);
                break;
        }

        await _saveService.SaveGame();
        System.Environment.Exit(0);
    }

    private async Task LoadGame(CancellationToken cancellationToken)
    {
        var saves = _saveService.GetSaves();

        if (!saves.Any())
        {
            AnsiConsole.MarkupLine("[red]No saves found![/]");
            AnsiConsole.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey(true);
            await Show(cancellationToken);
            return;
        }

        var saveSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a save to load")
                .AddChoices(saves.Select(x => x.Name)
                ));

        AnsiConsole.MarkupLine("Loading save [yellow]{0}[/]", saveSelection);

        var saveFile = saves.First(x => x.Name == saveSelection);

        await _saveService.LoadGame(saveFile);

        AnsiConsole.WriteLine("Save loaded, starting game...");

        await _gameService.Start(cancellationToken);
    }

    private async Task StartNewGame(CancellationToken cancellationToken)
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

        _mapService.LoadMap(mapFile);

        AnsiConsole.WriteLine("Map loaded, saving game...");

        var path = await _saveService.SaveGame();

        AnsiConsole.WriteLine("Game saved, loading game...");

        await _saveService.LoadGame(new FileInfo(path));

        AnsiConsole.WriteLine("Game loaded, starting game...");

        await _gameService.Start(cancellationToken);
    }
}
