namespace DolCon.Views;

using Enums;
using Services;
using Spectre.Console;
using Spectre.Console.Rendering;

public partial class GameService
{
    private void RenderHome()
    {
        var currentCell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        var biome = SaveGameService.CurrentBiome;
        var province = SaveGameService.CurrentProvince;
        var state = SaveGameService.CurrentState;
        var availableBurg = currentCell.burg > 0
            ? $"[green]{SaveGameService.GetBurg(currentCell.burg)?.name}"
            : "[red]None";
        var homePanels = new List<IRenderable>();

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
        };
        if (currentCell.locations.Any())
        {
            areaLines.Add(new Markup($"[green bold]Locations[/]"));
            areaLines.AddRange(
                currentCell.locations
                    .OrderByDescending(x => x.Rarity)
                    .Select(x => new Markup($"[green]{x.Name}[/]"))
            );
        }

        var areaRows = new Rows(
            areaLines.ToArray()
        );
        
        var areaPanel = new Panel(Align.Center(areaRows));

        homePanels.Add(areaPanel);

        if (burg != null)
        {
            var cityTitle = burg.isCityOfLight
                ? $"[rapidblink yellow Underline]The City of Light - {burg.name}[/]"
                : $"[bold]Current Burg: {burg.name}[/]";
            var features = new List<string>
            {
                $"{(burg.capital == 1 ? "Capitol" : string.Empty)}",
                $"{(burg.port == 1 ? "Port" : string.Empty)}",
                $"{(burg.citadel == 1 ? "Castle" : string.Empty)}",
                $"{(burg.walls == 1 ? "Walls" : string.Empty)}",
                $"{(burg.plaza == 1 ? "Marketplace" : string.Empty)}",
                $"{(burg.temple == 1 ? "Temple" : string.Empty)}",
                $"{(burg.shanty == 1 ? "Shanty Town" : string.Empty)}"
            };
            var featuresString = string.Join(" | ", features.Where(x => !string.IsNullOrEmpty(x)));

            var rows = new List<IRenderable>
            {
                new Markup(cityTitle),
                new Markup($"Population: [green]{Convert.ToInt32(burg.population * 1000)}[/]"),
                new Markup(featuresString)
            };
            rows.AddRange(from location in burg.locations.OrderByDescending(x => x.Rarity)
                let color = location.Rarity switch
                {
                    Rarity.uncommon => "green",
                    Rarity.rare => "blue",
                    Rarity.epic => "purple",
                    Rarity.legendary => "yellow",
                    _ => "white"
                }
                select new Markup($"[{color}]{location.Name}[/]"));

            homePanels.Add(new Panel(
                Align.Center(
                    new Rows(
                        rows.ToArray()
                    ))));
        }
        else
        {
            homePanels.Add(new Panel(
                Align.Center(
                    new Rows(
                        new Markup("[bold]Current Burg[/]"),
                        new Markup("[green]None[/]")
                    ))));
        }

        homePanels.Add(new Panel(
            Align.Center(
                new Rows(
                    new Markup("[bold]Current Location[/]"),
                    new Markup("[green]None[/]")
                ))));

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(homePanels.ToArray());

        _display.Update(grid);
        _ctx.Refresh();

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup(
                                "[bold]Standard controls[/]: ([green bold]H[/])ome | ([green bold]N[/])avigation | ([Red bold]E[/])xit")
                        ),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
    }
}
