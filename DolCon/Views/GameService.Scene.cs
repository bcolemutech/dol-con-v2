using DolCon.Services;
using Spectre.Console;

namespace DolCon.Views;

using Enums;

public partial class GameService
{
    private void RenderScene(ConsoleKeyInfo value)
    {
        _controls.Update(
            new Panel(
                    Align.Center(
                        new Markup("Select an option above."),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
        
        switch (_scene.Type)
        {
            case SceneType.Dialogue:
                RenderDialog(value);
                break;
            case SceneType.Battle:
                RenderBattle(value);
                break;
            case SceneType.Shop:
                RenderShop(value);
                break;
            case SceneType.None:
            default:
                _scene.IsCompleted = true;
                RenderNotReady();
                break;
        }
        
        _screen = Screen.Navigation;
    }

    private void RenderShop(ConsoleKeyInfo value)
    {
        while (!_scene.IsCompleted)
        {
            var selectionTable = new Table();
            selectionTable.AddColumn("Key");
            selectionTable.AddColumn("Selection");
            selectionTable.AddColumn("Price");
            
            foreach (var (key, selection) in _scene.Selections)
            {
                var product = selection.Split('|');
                selectionTable.AddRow(key.ToString(), product[0], product[1]);
            }
            
            _display.Update(
                new Panel(
                    new Rows(
                        Align.Center(
                            new Markup($"[bold black on white]{_scene.Title}[/]")),
                        Align.Center(
                            new Markup($"[bold]{_scene.Description}[/]")),
                        Align.Center(
                            selectionTable
                        )
                    )));
            _ctx.Refresh();
            var click = Console.ReadKey(true);
            var thisChar = click.KeyChar.ToString();
            var cleanChar = thisChar.First().ToString();
            var tryParse = int.TryParse(cleanChar, out var choice);
            if (!tryParse) continue;
            choice = value.Modifiers == ConsoleModifiers.Alt ? choice + 10 : choice;
            _scene.Selection = choice;
            _scene = _shopService.ProcessShop(_scene);
        }
    }

    private void RenderBattle(ConsoleKeyInfo value)
    {
        throw new NotImplementedException();
    }

    private void RenderDialog(ConsoleKeyInfo value)
    {
        throw new NotImplementedException();
    }
}