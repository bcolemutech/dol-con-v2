namespace DolCon.Views;

using Enums;
using Services;
using Spectre.Console;
using Spectre.Console.Rendering;

public partial class GameService
{
    private void RenderHome()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(
            AreaPanel(),
            LocationPanel(),
            PlayerPanel()
        );

        _display.Update(grid);
        _ctx.Refresh();

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup(
                                "[bold]Standard controls[/]: ([green bold]N[/])avigation | ([green bold]I[/])nventory | [Red bold]Esc[/] to exit")
                        ),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
    }

    private static Panel PlayerPanel()
    {
        var party = SaveGameService.Party;
        var player = party.Players.First();

        var playerLines = new List<IRenderable>
        {
            new Markup($"[bold]Player Details[/]"),
            new Markup($"Name: [green]{player.Name}[/]"),
            new Markup($"Stamina: [green]{party.Stamina:P}[/]"),
            new Markup(
                $"Coin: [bold gold1]{player.gold}[/]|[bold silver]{player.silver}[/]|[bold tan]{player.copper}[/]"),
            new Markup($"Inventory Count: [green]{player.Inventory.Count}[/]")
        };

        var playerRows = new Rows(
            playerLines.ToArray()
        );

        return new Panel(Align.Center(playerRows));
    }

    private static Panel LocationPanel()
    {
        var rows = new List<IRenderable>();
        var location = SaveGameService.CurrentLocation;
        var burg = SaveGameService.CurrentBurg;

        if (burg is null)
        {
            rows.Add(new Markup("[bold]In the wild[/]"));
        }
        else
        {
            var cityTitle = burg.isCityOfLight
                ? $"[yellow Underline]The City of Light - {burg.name}[/]"
                : $"[bold]Current Burg: {burg.name}[/]";

            rows.Add(new Markup(cityTitle));
            rows.Add(new Markup($"Population: [green]{Convert.ToInt32(burg.population * 1000)}[/]"));
            rows.Add(new Markup($"Size: [green]{burg.size}[/]"));
            rows.Add(new Markup($"Locations: [green]{burg.locations.Count}[/]"));
        }
        
        if (location is not null)
        {
            rows.Add(new Markup($"Location: [green]{location.Name}[/]"));
            rows.Add(new Markup($"Type: [green]{location.Type.Type}[/]"));
            rows.Add(new Markup($"Rarity: [green]{location.Rarity}[/]"));
            if (location.Type.Size == LocationSize.unexplorable)
            {
                rows.Add(new Markup($"[red]This location is not explorable[/]"));
                if (location.Type.Services.Any())
                {
                    rows.Add(new Markup($"Services: [green]{string.Join(", ", location.Type.Services)}[/]"));
                }
                if (location.Type.Goods.Any())
                {
                    rows.Add(new Markup($"Shops: [green]{string.Join(", ", location.Type.Goods.Select(x => x.Name))}[/]"));
                }
            }
            else
            {
                rows.Add(new Markup($"[bold green]{location.ExploredPercent:P}[/] Explored"));
            }
        }
        else
        {
            rows.Add(new Markup($"Location: [red]None[/]"));
        }

        return new Panel(
            Align.Center(
                new Rows(
                    rows.ToArray()
                )));
    }

    private static Panel AreaPanel()
    {
        var currentCell = SaveGameService.CurrentCell;
        var biome = SaveGameService.CurrentBiome;
        var province = SaveGameService.CurrentProvince;
        var state = SaveGameService.CurrentState;
        
        var availableBurg = currentCell.burg > 0
            ? $"[green]{SaveGameService.GetBurg(currentCell.burg)?.name}"
            : "[red]None";
        var areaLines = new List<IRenderable>
        {
            new Markup($"[bold]Current Area[/]"),
            new Markup($"Biome: [green]{biome}[/]"),
            new Markup($"Rural Population: [green]{Convert.ToInt32(currentCell.pop * 1000)}[/]"),
            new Markup($"Province: [green]{province.fullName}[/]"),
            new Markup($"State: [green]{state.fullName}[/]"),
            new Markup($"Burg: {availableBurg}[/]"),
            new Markup($"Size: [green]{currentCell.CellSize}[/]"),
            new Markup($"Pop Density: [green]{currentCell.PopDensity}[/]"),
            new Markup($"Locations Count: [green]{currentCell.locations.Count(x => x.Discovered)}[/]")
        };

        var areaRows = new Rows(
            areaLines.ToArray()
        );

        var areaPanel = new Panel(Align.Center(areaRows));

        return areaPanel;
    }
}
