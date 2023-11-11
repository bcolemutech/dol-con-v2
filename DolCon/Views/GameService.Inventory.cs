using DolCon.Enums;
using DolCon.Services;
using Spectre.Console;

namespace DolCon.Views;

public partial class GameService
{
    private int _inventorySelected;

    private void RenderInventory()
    {
        if (_flow.Key is { Key: ConsoleKey.I })
        {
            _inventorySelected = 0;
        }
        
        if (_flow.Key is { Key: ConsoleKey.DownArrow } or { Key: ConsoleKey.S } &&
            _inventorySelected < SaveGameService.Party.Players.First().Inventory.Count - 1)
        {
            _inventorySelected++;
        }
        
        if (_flow.Key is { Key: ConsoleKey.UpArrow } or { Key: ConsoleKey.W } && _inventorySelected > 0)
        {
            _inventorySelected--;
        }

        if (_flow.Key is { Key: ConsoleKey.D} or { Key: ConsoleKey.Delete } && SaveGameService.Party.Players.First().Inventory.Count > 0)
        {
            SaveGameService.Party.Players.First().Inventory.RemoveAt(_inventorySelected);
            _inventorySelected = 0;
        }
        
        var table = new Table();
        table.AddColumn("Select");
        table.AddColumn("Name");
        table.AddColumn("Description");
        table.AddColumn("Rarity");
        table.AddColumn("Tags");
        table.AddColumn("Selling Price");
        var player = SaveGameService.Party.Players.First();
        var i = 0;
        foreach (var item in player.Inventory)
        {
            var selected = i == _inventorySelected ? "[green bold]X[/]" : "";
            var rarity = item.Rarity switch
            {
                Rarity.Common => "[grey]Common[/]",
                Rarity.Uncommon => "[green]Uncommon[/]",
                Rarity.Rare => "[blue]Rare[/]",
                Rarity.Epic => "[purple]Epic[/]",
                Rarity.Legendary => "[gold1]Legendary[/]",
                _ => throw new ArgumentOutOfRangeException()
            };
            var sellingPrice = item.Price / 2;
            var copper = sellingPrice % 10;
            var silver = (sellingPrice / 10) % 100;
            var gold = sellingPrice / 100;
            table.AddRow(
                selected,
                item.Name,
                item.Description,
                rarity,
                item.Tags.Select(x => x.Name).Aggregate((x, y) => $"{x}, {y}"),
                $"[bold gold1]{gold}[/]|[bold silver]{silver}[/]|[bold tan]{copper}[/]"
            );
            i++;
        }
        _display.Update(table);
        _ctx.Refresh();
        _controls.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup(
                                "[bold]Standard controls[/]: ([green bold]H[/])ome | ([green bold]N[/])avigation | [Red bold]Esc[/] to exit")
                        ),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
    }
}