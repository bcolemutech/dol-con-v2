using Spectre.Console;

namespace DolCon.Views;

using Enums;

public partial class GameService
{
    private void RenderScene()
    {
        switch (_scene.Type)
        {
            case SceneType.Dialogue:
                RenderDialog();
                break;
            case SceneType.Battle:
                RenderBattle();
                break;
            case SceneType.Shop:
                RenderShop();
                break;
            case SceneType.None:
            default:
                SetMessage(MessageType.Error, "Bad redirect!!!");
                _scene.IsCompleted = true;
                _flow.Screen = Screen.Navigation;
                _flow.Redirect = true;
                break;
        }
    }

    private void RenderShop()
    {
        while (!_scene.IsCompleted)
        {
            var selectionTable = new Table();
            selectionTable.AddColumn("Key");
            selectionTable.AddColumn("Selection");
            if (_scene.SelectedService is not null)
            {
                selectionTable.AddColumn("Price");
                foreach (var (key, selection) in _scene.Selections)
                {
                    var product = selection.Split('|');
                    selectionTable.AddRow(key.ToString(), product[0], product[1]);
                }
            }
            else
            {
                foreach (var (key, selection) in _scene.Selections)
                {
                    selectionTable.AddRow(key.ToString(), selection);
                }
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
            _controls.Update(
                new Panel(
                        Align.Center(
                            new Markup(_scene.Message),
                            VerticalAlignment.Middle))
                    .Expand());
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
        _flow.Screen = Screen.Navigation;
        _scene.Reset();
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