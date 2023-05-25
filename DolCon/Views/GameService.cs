namespace DolCon.Views;

using DolCon.Enums;
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
    private Screen _screen;
    private LiveDisplayContext _ctx;
    
    private readonly IImageService _imageService;

    public GameService(IImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task Start(CancellationToken token)
    {
        token.Register(() =>
        {
            AnsiConsole.MarkupLine("[red]Game cancelled[/]");
            System.Environment.Exit(0);
        });
        
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Display"),
                new Layout("Controls"));

        _display = layout["Display"];
        _controls = layout["Controls"];
        
        _display.Ratio = 4;
        _screen = Screen.Home;
        
        AnsiConsole.Clear();
        
        await AnsiConsole.Live(layout).StartAsync(async ctx =>
        {
            _ctx = ctx;
            
            RenderScreen(' ');

            await ProcessKey(token);
        });
    }

    private async Task ProcessKey(CancellationToken token)
    {
        do
        {
            var key = Console.ReadKey(true);
            
            if (Enum.IsDefined((Screen)key.Key))
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
                RenderScreen(key.KeyChar);
            }

        } while (token.IsCancellationRequested == false && _screen != Screen.Exit);
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

    private void RenderScreen(char? keyChar = null)
    {
        var value = keyChar ?? ' ';
        switch (_screen)
        {
            case Screen.Home:
                RenderHome();
                break;
            case Screen.Navigation:
                RenderNavigation(value);
                break;
            case Screen.Inventory:
                RenderNotReady();
                break;
            case Screen.Character:
                RenderNotReady();
                break;
            case Screen.Quests:
                RenderNotReady();
                break;
            case Screen.Exit:
                break;
            default:
                RenderNotReady();
                break;
        }
    }
}
