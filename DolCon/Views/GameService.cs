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
    private Layout _display;
    private Layout _controls;
    private Layout _message;
    private Screen _screen;
    private LiveDisplayContext _ctx;
    private bool _exiting;
    
    private readonly IImageService _imageService;
    private readonly IMoveService _moveService;
    private readonly IEventService _eventService;
    private Scene _scene;

    public GameService(IImageService imageService, IMoveService moveService, IEventService eventService)
    {
        _imageService = imageService;
        _moveService = moveService;
        _eventService = eventService;
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
        _screen = Screen.Home;
        
        AnsiConsole.Clear();
        
        await AnsiConsole.Live(layout).StartAsync(async ctx =>
        {
            _ctx = ctx;
            
            SetMessage(MessageType.Info, "Welcome to Dominion of Light");

            RenderScreen();

            await ProcessKey(token);
        });
    }

    private async Task ProcessKey(CancellationToken token)
    {
        SetMessage(MessageType.Info, "Welcome to Dominion of Light");
        do
        {
            var key = Console.ReadKey(true);
            if(key is { Key: ConsoleKey.E, Modifiers: ConsoleModifiers.Alt })
            {
                _exiting = true;
            }
            else if (!_scene.IsCompleted)
            {
                _screen = Screen.Scene;
                RenderScreen();
            }
            else if (Enum.IsDefined((Screen)key.Key))
            {
                _screen = (Screen)key.Key;
                RenderScreen();
            }
            else if (Enum.IsDefined((HotKeys)key.Key))
            {
                ProcessHotKey((HotKeys)key.Key);
            }
            else
            {
                RenderScreen(key);
            }

        } while (token.IsCancellationRequested == false && !_exiting);
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

    private void RenderScreen(ConsoleKeyInfo? keyChar = null)
    {
        var value = keyChar ?? new ConsoleKeyInfo();
        switch (_screen)
        {
            case Screen.Home:
                RenderHome();
                break;
            case Screen.Navigation:
                RenderNavigation(value);
                break;
            case Screen.Scene:
                RenderScene(value);
                break;
            case Screen.Inventory:
                RenderNotReady();
                break;
            case Screen.Quests:
                RenderNotReady();
                break;
            default:
                RenderNotReady();
                break;
        }
    }

    private void SetMessage(MessageType type ,string message)
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
}
