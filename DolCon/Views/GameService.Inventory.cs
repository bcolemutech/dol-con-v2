using DolCon.Enums;
using DolCon.Models;
using DolCon.Services;
using Spectre.Console;

namespace DolCon.Views;

public partial class GameService
{
    private PaginatedList<Item>? _inventoryPagination;
    private const int InventoryPageSize = 10;

    private void RenderInventory()
    {
        var player = SaveGameService.Party.Players.First();

        // Initialize or refresh pagination when entering inventory screen
        if (_flow.Key is { Key: ConsoleKey.I } || _inventoryPagination is null)
        {
            _inventoryPagination = new PaginatedList<Item>(player.Inventory, InventoryPageSize);
        }

        // Handle navigation
        switch (_flow.Key)
        {
            case { Key: ConsoleKey.DownArrow } or { Key: ConsoleKey.S }:
                _inventoryPagination.MoveDown();
                break;
            case { Key: ConsoleKey.UpArrow } or { Key: ConsoleKey.W }:
                _inventoryPagination.MoveUp();
                break;
            case { Key: ConsoleKey.PageDown } or { Key: ConsoleKey.Z }:
                _inventoryPagination.NextPage();
                break;
            case { Key: ConsoleKey.PageUp } or { Key: ConsoleKey.A }:
                _inventoryPagination.PreviousPage();
                break;
        }

        // Handle delete
        if (_flow.Key is { Key: ConsoleKey.D } or { Key: ConsoleKey.Delete } && player.Inventory.Count > 0)
        {
            player.Inventory.RemoveAt(_inventoryPagination.SelectedIndex);
            _inventoryPagination.UpdateItems(player.Inventory);
        }

        // Handle equip/unequip
        if (_flow.Key is { Key: ConsoleKey.E } && player.Inventory.Count > 0)
        {
            ProcessEquip(player.Inventory, _inventoryPagination.SelectedIndex);
        }

        RenderInventoryTable(player);
    }

    private void ProcessEquip(List<Item> items, int selectedIndex)
    {
        if (selectedIndex >= items.Count) return;

        var item = items[selectedIndex];
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
                    shield.Count > 1 || (twoHanded.Count > 0 && shield.Count > 0) ||
                    (oneHanded.Count > 0 && twoHanded.Count > 0))
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

    private void RenderInventoryTable(Player player)
    {
        var table = new Table();
        table.AddColumn("Select");
        table.AddColumn("Equipped");
        table.AddColumn("Name");
        table.AddColumn("Description");
        table.AddColumn("Rarity");
        table.AddColumn("Tags");
        table.AddColumn("Selling Price");

        // Only display current page items
        var pageItems = _inventoryPagination!.CurrentPageItems.ToList();
        for (var i = 0; i < pageItems.Count; i++)
        {
            var item = pageItems[i];
            var selected = i == _inventoryPagination.CurrentPageSelectedIndex ? "[green bold]X[/]" : "";
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
            var tags = item.Tags.Count > 0
                ? item.Tags.Select(x => x.Name).Aggregate((x, y) => $"{x}, {y}")
                : "-";
            table.AddRow(
                selected,
                item.Equipped ? "[green bold]X[/]" : "",
                item.Name,
                item.Description,
                rarity,
                tags,
                $"[bold gold1]{gold}[/]|[bold silver]{silver}[/]|[bold tan]{copper}[/]"
            );
        }

        // Add page info caption
        if (_inventoryPagination.TotalItems > 0)
        {
            table.Caption = new TableTitle($"[dim]{_inventoryPagination.PageInfo}[/]");
        }

        _display.Update(table);
        _ctx.Refresh();

        var paginationHint = _inventoryPagination.TotalPages > 1
            ? " | ([green bold]A[/]) Prev Page | ([green bold]Z[/]) Next Page"
            : "";

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup(
                                "[bold]Standard controls[/]: ([green bold]H[/])ome | ([green bold]N[/])avigation | [Red bold]Esc[/] to exit"),
                            new Markup(
                                $"[bold]Inventory controls[/]: ([green bold]W[/]) Up | ([green bold]S[/]) Down | ([green bold]D[/]) Delete | ([green bold]E[/]) Equip/Unequip{paginationHint}")
                        ),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
    }
}
