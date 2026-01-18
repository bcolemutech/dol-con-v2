using DolCon.Models;
using Spectre.Console;

namespace DolCon.Views;

using Enums;
using Services;

public partial class GameService
{
    private PaginatedList<KeyValuePair<int, ShopSelection>>? _shopPagination;
    private const int ShopPageSize = 10;

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
            _shopPagination = null;
            return;
        }

        // Initialize or refresh pagination when selections change
        if (_shopPagination is null || _shopPagination.TotalItems != _scene.Selections.Count)
        {
            _shopPagination = new PaginatedList<KeyValuePair<int, ShopSelection>>(
                _scene.Selections.ToList(), ShopPageSize);
        }

        switch (_flow.Key)
        {
            case { Key: ConsoleKey.L }: // Leave or go back
                _scene.IsCompleted = true;
                _flow.Screen = Screen.Navigation;
                _flow.Redirect = true;
                _scene.Message = "You left the shop.";
                _scene.Reset();
                _shopPagination = null;
                return;
            case { Key: ConsoleKey.DownArrow } or { Key: ConsoleKey.S }:
                _shopPagination.MoveDown();
                break;
            case { Key: ConsoleKey.UpArrow } or { Key: ConsoleKey.W }:
                _shopPagination.MoveUp();
                break;
            case { Key: ConsoleKey.PageDown } or { Key: ConsoleKey.Z }:
                _shopPagination.NextPage();
                break;
            case { Key: ConsoleKey.PageUp } or { Key: ConsoleKey.A }:
                _shopPagination.PreviousPage();
                break;
            case { Key: ConsoleKey.Enter } when _scene.Selections.Count > 0:
                var selectedPair = _shopPagination.GetSelected();
                _scene.Selection = selectedPair.Key;
                _scene = _shopService.ProcessShop(_scene);
                // Refresh pagination after processing (list may have changed)
                _shopPagination = new PaginatedList<KeyValuePair<int, ShopSelection>>(
                    _scene.Selections.ToList(), ShopPageSize);
                break;
        }

        RenderShopTable();
    }

    private void RenderShopTable()
    {
        var selectionTable = new Table();
        selectionTable.AddColumn("Select");
        selectionTable.AddColumn("Selection");

        var pageItems = _shopPagination!.CurrentPageItems.ToList();

        if (_scene.SelectedService is not null)
        {
            selectionTable.AddColumn("Price");
            for (var i = 0; i < pageItems.Count; i++)
            {
                var (_, selection) = pageItems[i];
                var selected = i == _shopPagination.CurrentPageSelectedIndex ? "X" : "";
                var color = selection.Afford ? "white" : "grey";
                var copper = selection.Price % 10;
                var silver = (selection.Price / 10) % 10;
                var gold = selection.Price / 100;
                selectionTable.AddRow(ColorWrap(selected, color), ColorWrap(selection.Name, color),
                    $"[bold gold1]{gold}[/]|[bold silver]{silver}[/]|[bold tan]{copper}[/]");
            }
        }
        else
        {
            for (var i = 0; i < pageItems.Count; i++)
            {
                var (_, selection) = pageItems[i];
                var selected = i == _shopPagination.CurrentPageSelectedIndex ? "[green bold]X[/]" : "";
                selectionTable.AddRow(selected, selection.Name);
            }
        }

        // Add page info caption
        if (_shopPagination.TotalItems > 0 && _shopPagination.TotalPages > 1)
        {
            selectionTable.Caption = new TableTitle($"[dim]{_shopPagination.PageInfo}[/]");
        }

        var player = SaveGameService.Party.Players.First();
        var coinDisplay =
            $"Your Coin: [bold gold1]{player.gold}[/]|[bold silver]{player.silver}[/]|[bold tan]{player.copper}[/]";

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

        var paginationHint = _shopPagination.TotalPages > 1
            ? " | [bold black on white]A[/] Prev Page | [bold black on white]Z[/] Next Page"
            : "";

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup(
                                $"[bold black on white]{_scene.Message}[/]"),
                            new Markup(
                                $"[bold black on white]L[/]eave Shop{paginationHint}"))))
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
