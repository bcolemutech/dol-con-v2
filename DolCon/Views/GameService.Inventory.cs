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
        
        var table = new Table();
        table.AddColumn("Select");
        table.AddColumn("Name");
        table.AddColumn("Description");
        table.AddColumn("Rarity");
        table.AddColumn("Tags");
        table.AddColumn("Price");
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
            var copper = item.Price % 100;
            var silver = (item.Price / 100) % 100;
            var gold = item.Price / 10000;
            table.AddRow(
                selected,
                item.Name,
                item.Description,
                rarity,
                item.Tags.Select(x => x.ToString()).Aggregate((x, y) => $"{x}, {y}") ?? "",
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