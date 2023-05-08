namespace DolCon.Services;

using Spectre.Console;

public interface IGameService
{
    Task Start(CancellationToken token);
}

public class GameService : IGameService
{
    private Layout _display;
    private Layout _controls;
    private Screen _screen;

    public async Task Start(CancellationToken token)
    {
        token.Register(() =>
        {
            AnsiConsole.MarkupLine("[red]Game cancelled[/]");
            System.Environment.Exit(0);
        });
        await HomeScreen(token);
    }

    private async Task HomeScreen(CancellationToken token)
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Display"),
                new Layout("Controls"));

        _display = layout["Display"];
        _controls = layout["Controls"];

        _display.Ratio = 3;
        _screen = Screen.Home;
        RenderHome();

        AnsiConsole.Write(layout);
        await ProcessKey(token);
    }

    private async Task ProcessKey(CancellationToken token)
    {
        do
        {
            var key = Console.ReadKey(true);
            switch (_screen)
            {
                case Screen.Home:
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            _screen = Screen.Exit;
                            break;
                        case ConsoleKey.D1:
                        case ConsoleKey.NumPad1:
                            _screen = Screen.Navigation;
                            break;
                        case ConsoleKey.D2:
                        case ConsoleKey.NumPad2:
                            _screen = Screen.Inventory;
                            break;
                        case ConsoleKey.D3:
                        case ConsoleKey.NumPad3:
                            _screen = Screen.Character;
                            break;
                        case ConsoleKey.D4:
                        case ConsoleKey.NumPad4:
                            _screen = Screen.Quests;
                            break;
                    }

                    break;
                case Screen.Navigation:
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            _screen = Screen.Home;
                            break;
                        case ConsoleKey.D1 or ConsoleKey.NumPad1 when SaveGameService.Party.Burg != null:
                            SaveGameService.Party.Burg = null;
                            _screen = Screen.Home;
                            break;
                    }

                    if (int.TryParse(key.KeyChar.ToString(), out var number))
                    {
                        _screen = Screen.Home;
                        SaveGameService.Party.Cell = SaveGameService.CurrentMap.cells.cells
                            .First(x => x.i == SaveGameService.Party.Cell).c.First(x => x == number);
                    }

                    break;
                case Screen.Inventory:
                    break;
                case Screen.Character:
                    break;
                case Screen.Quests:
                    break;
                case Screen.Exit:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            RenderScreen();
        } while (token.IsCancellationRequested == false || _screen != Screen.Exit);
    }

    private void RenderScreen()
    {
        switch (_screen)
        {
            case Screen.Home:
                RenderHome();
                break;
            case Screen.Navigation:
                RenderNavigation();
                break;
            case Screen.Inventory:
                break;
            case Screen.Character:
                break;
            case Screen.Quests:
                break;
            case Screen.Exit:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void RenderNavigation()
    {
        throw new NotImplementedException();
    }

    private void RenderHome()
    {
        var currentCell = SaveGameService.CurrentMap.cells.cells.First(x => x.i == SaveGameService.Party.Cell);
        var burg = SaveGameService.CurrentMap.cells.burgs.FirstOrDefault(x => x.i == SaveGameService.Party.Burg);
        var biome = SaveGameService.CurrentMap.biomes.name[currentCell.biome];
        _display.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup($"Burg name: [green]{burg?.name ?? "None"}[/]"),
                            new Markup("Biome: [green]" + biome + "[/]"),
                            new Markup("Population: [green]" + currentCell.pop + "[/]")
                        ),
                        VerticalAlignment.Middle))
                .Expand());

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Markup("[Green]1[/]: Navigate | [Red]Esc[/]: Exit"), VerticalAlignment.Middle))
                .Expand());
    }
}

enum Screen
{
    Home,
    Navigation,
    Inventory,
    Character,
    Quests,
    Exit
}
