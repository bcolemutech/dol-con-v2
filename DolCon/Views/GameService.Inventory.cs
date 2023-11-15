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
        
        if (_flow.Key is { Key: ConsoleKey.E} && SaveGameService.Party.Players.First().Inventory.Count > 0)
        {
            var items = SaveGameService.Party.Players.First().Inventory;
            var item = items[_inventorySelected];
            var type = item.Equipment;
            switch (type)
            {
                case Equipment.None:
                    break;
                case Equipment.Head:
                case Equipment.Body:
                case Equipment.Legs:
                case Equipment.Feet:
                case Equipment.Hands:
                case Equipment.Neck:
                    item.Equipped = !item.Equipped;
                    var equipped = items.Where(x => x.Equipment == type && x.Equipped).ToList();
                    if (equipped.Count > 1)
                    {
                        equipped.ForEach(x => x.Equipped = false);
                        item.Equipped = true;
                    }
                    break;
                case Equipment.OneHanded:
                case Equipment.TwoHanded:
                case Equipment.Shield:
                    item.Equipped = !item.Equipped;
                    var oneHanded = items.Where(x => x is { Equipment: Equipment.OneHanded, Equipped: true }).ToList();
                    var twoHanded = items.Where(x => x is { Equipment: Equipment.TwoHanded, Equipped: true }).ToList();
                    var shield = items.Where(x => x is { Equipment: Equipment.Shield, Equipped: true }).ToList();
                    if (oneHanded.Count > 2 || twoHanded.Count > 1 || (oneHanded.Count > 1 && shield.Count > 1) ||
                        shield.Count > 1 || (twoHanded.Count > 0 && shield.Count > 0) || (oneHanded.Count > 0 && twoHanded.Count > 0))
                    {
                        oneHanded.ForEach(x => x.Equipped = false);
                        twoHanded.ForEach(x => x.Equipped = false);
                        shield.ForEach(x => x.Equipped = false);
                        item.Equipped = true;
                    }
                    break;
                case Equipment.Ring:
                    item.Equipped = !item.Equipped;
                    var rings = items.Where(x => x is { Equipment: Equipment.Ring, Equipped: true }).ToList();
                    if (rings.Count > 2)
                    {
                        rings.ForEach(x => x.Equipped = false);
                        item.Equipped = true;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }
        
        var table = new Table();
        table.AddColumn("Select");
        table.AddColumn("Equipped");
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
            var silver = (sellingPrice / 10) % 10;
            var gold = sellingPrice / 100;
            table.AddRow(
                selected,
                item.Equipped ? "[green bold]X[/]" : "",
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
                                "[bold]Standard controls[/]: ([green bold]H[/])ome | ([green bold]N[/])avigation | [Red bold]Esc[/] to exit"),
                            new Markup(
                                "[bold]Inventory controls[/]: ([green bold]W[/]) Up | ([green bold]S[/]) Down | ([green bold]D[/]) Delete | ([green bold]E[/]) Equip/Unequip")
                        ),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
    }
}