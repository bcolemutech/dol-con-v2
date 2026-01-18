using Spectre.Console;

namespace DolCon.Views;

using Enums;
using Services;

public partial class GameService
{
    private int _sceneSelected;

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
        if (_scene.IsCompleted)
        {
            _flow.Screen = Screen.Navigation;
            _flow.Redirect = true;
            _scene.Reset();
            _sceneSelected = 0;
            return;
        }

        switch (_flow.Key)
        {
            case { Key: ConsoleKey.L }: // Leave or go back
                _scene.IsCompleted = true;
                _flow.Screen = Screen.Navigation;
                _flow.Redirect = true;
                _scene.Message = "You left the shop.";
                _scene.Reset();
                _sceneSelected = 0;
                return;
            case { Key: ConsoleKey.DownArrow } or { Key: ConsoleKey.S } when
                _sceneSelected < _scene.Selections.Count - 1:
                _sceneSelected++;
                break;
            case { Key: ConsoleKey.UpArrow } or { Key: ConsoleKey.W } when _sceneSelected > 0:
                _sceneSelected--;
                break;
            case { Key: ConsoleKey.Enter } when _scene.Selections.Count > 0:
                _scene.Selection = _sceneSelected + 1;
                _scene = _shopService.ProcessShop(_scene);
                break;
        }

        var selectionTable = new Table();
        selectionTable.AddColumn("Select");
        selectionTable.AddColumn("Selection");
        if (_scene.SelectedService is not null)
        {
            selectionTable.AddColumn("Price");
            var i = 0;
            foreach (var (key, selection) in _scene.Selections)
            {
                var selected = i == _sceneSelected ? "X" : "";
                var color = selection.Afford ? "white" : "grey";
                var copper = selection.Price % 10;
                var silver = (selection.Price / 10) % 10;
                var gold = selection.Price / 100;
                selectionTable.AddRow(ColorWrap(selected, color), ColorWrap(selection.Name, color),
                    $"[bold gold1]{gold}[/]|[bold silver]{silver}[/]|[bold tan]{copper}[/]");
                i++;
            }
        }
        else
        {
            var i = 0;
            foreach (var (key, selection) in _scene.Selections)
            {
                var selected = i == _sceneSelected ? "[green bold]X[/]" : "";
                selectionTable.AddRow(selected, selection.Name);
                i++;
            }
        }

        var player = SaveGameService.Party.Players.First();
        var coinDisplay = $"Your Coin: [bold gold1]{player.gold}[/]|[bold silver]{player.silver}[/]|[bold tan]{player.copper}[/]";

        _display.Update(
            new Panel(
                new Rows(
                    Align.Center(
                        new Markup($"[bold black on white]{_scene.Title}[/]")),
                    Align.Center(
                        new Markup($"[bold]{_scene.Description}[/]")),
                    Align.Center(
                        new Markup(coinDisplay)),
                    Align.Center(
                        selectionTable
                    )
                )));
        _ctx.Refresh();

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup(
                                $"[bold black on white]{_scene.Message}[/]"),
                            new Markup(
                                $"[bold black on white]L[/]eave Shop"))))
                .Expand());
        _ctx.Refresh();
    }

    private static string ColorWrap(string text, string color)
    {
        return $"[{color}]{text}[/]";
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