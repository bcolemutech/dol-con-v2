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
        if (_flow.Key.HasValue)
        {
            var click = _flow.Key.Value;
            var thisChar = click.KeyChar.ToString();
            var cleanChar = thisChar.First().ToString();
            if (int.TryParse(cleanChar, out var choice))
            {
                choice = _flow.Key.Value.Modifiers == ConsoleModifiers.Alt ? choice + 10 : choice;
                _scene.Selection = choice;
                _scene = _shopService.ProcessShop(_scene);
            }
        }

        if (_scene.IsCompleted)
        {
            _flow.Screen = Screen.Navigation;
            _flow.Redirect = true;
            _scene.Reset();
            return;
        }

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
    }

    private void RenderBattle()
    {
        throw new NotImplementedException();
    }

    private void RenderDialog()
    {
        throw new NotImplementedException();
    }
}