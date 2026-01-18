namespace DolCon.Views;

using Models;
using Enums;
using Services;
using Spectre.Console;

public interface IGameService
{
    Task Start(CancellationToken token);
}

public partial class GameService : IGameService
{
    private Layout _display = null!;
    private Layout _controls = null!;
    private Layout _message = null!;
    private LiveDisplayContext _ctx = null!;
    private readonly Flow _flow = new();
    

    private readonly IImageService _imageService;
    private readonly IMoveService _moveService;
    private readonly IEventService _eventService;
    private Scene _scene = new();
    private readonly IShopService _shopService;

    public GameService(IImageService imageService, IMoveService moveService, IEventService eventService,
        IShopService shopService)
    {
        _imageService = imageService;
        _moveService = moveService;
        _eventService = eventService;
        _shopService = shopService;
    }

    public async Task Start(CancellationToken token)
    {
        token.Register(() =>
        {
            AnsiConsole.MarkupLine("[red]Game cancelled[/]");
            Environment.Exit(0);
        });

        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Message"),
                new Layout("Display"),
                new Layout("Controls"));

        _display = layout["Display"];
        _controls = layout["Controls"];
        _message = layout["Message"];

        _display.Ratio = 5;

        AnsiConsole.Clear();

        await AnsiConsole.Live(layout).StartAsync(async ctx =>
        {
            _ctx = ctx;

            SetMessage(MessageType.Info, "Welcome to Dominion of Light");

            await ProcessKey(token);
        });
    }

    private async Task ProcessKey(CancellationToken token)
    {
        SetMessage(MessageType.Info, "Welcome to Dominion of Light");

        do
        {
            if (_flow.Key is null)
            {
                RenderScreen();
            }
            else if (_flow.ShowExitConfirmation)
            {
                var result = KeyProcessor.ProcessExitConfirmation(_flow, _flow.Key.Value);
                switch (result)
                {
                    case ExitConfirmationResult.Exit:
                        return;
                    case ExitConfirmationResult.Cancel:
                        RenderScreen();
                        break;
                    case ExitConfirmationResult.Ignored:
                        RenderExitConfirmation();
                        break;
                }
            }
            else if (_flow.Key.Value is { Key: ConsoleKey.Escape })
            {
                _flow.ShowExitConfirmation = true;
                RenderExitConfirmation();
            }
            else if (!_scene.IsCompleted)
            {
                RenderScreen();
            }
            else if (Enum.IsDefined((Screen)_flow.Key.Value.Key))
            {
                _flow.Screen = (Screen)_flow.Key.Value.Key;
                RenderScreen();
            }
            else if (Enum.IsDefined((HotKeys)_flow.Key.Value.Key))
            {
                ProcessHotKey((HotKeys)_flow.Key.Value.Key);
            }
            else
            {
                RenderScreen();
            }

            if (_flow.Redirect)
            {
                _flow.Redirect = false;
            }
            else
            {
                _flow.Key = Console.ReadKey(true);
            }
        } while (token.IsCancellationRequested == false);
    }

    private void ProcessHotKey(HotKeys hotKey)
    {
        switch (hotKey)
        {
            case HotKeys.Map:
                _imageService.OpenImage();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(hotKey), hotKey, null);
        }
    }

    private void RenderScreen()
    {
        var value = _flow.Key ?? new ConsoleKeyInfo();
        switch (_flow.Screen)
        {
            case Screen.Home:
                RenderHome();
                break;
            case Screen.Navigation:
                RenderNavigation(value);
                break;
            case Screen.Scene:
                RenderScene();
                break;
            case Screen.Inventory:
                RenderInventory();
                break;
            case Screen.Quests:
                RenderNotReady();
                break;
            default:
                RenderNotReady();
                break;
        }
    }

    private void SetMessage(MessageType type, string message)
    {
        var markup = type switch
        {
            MessageType.Success => new Markup($"[green]{message}[/]"),
            MessageType.Error => new Markup($"[red bold]{message}[/]"),
            MessageType.Info => new Markup($"{message}"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        _message.Update(new Panel(markup).Collapse());
        _ctx.Refresh();
    }

    private void RenderExitConfirmation()
    {
        _display.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup("[bold yellow]Are you sure you want to exit?[/]"),
                            new Markup(""),
                            new Markup("Your game will be saved automatically."),
                            new Markup(""),
                            new Markup("[green bold]Y[/]es - Exit and save"),
                            new Markup("[red bold]N[/]o - Return to game")
                        ),
                        VerticalAlignment.Middle))
                .Border(BoxBorder.Double)
                .Expand());

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Markup("Press [green bold]Y[/] to confirm exit or [red bold]N[/] to cancel"),
                        VerticalAlignment.Middle))
                .Expand());

        _ctx.Refresh();
    }
}