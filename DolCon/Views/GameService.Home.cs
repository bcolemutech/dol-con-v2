namespace DolCon.Views;

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
        var homePanels = new List<IRenderable>
        {
            new Panel(
                Align.Center(
                    new Rows(
                        new Markup("[bold]Current Area[/]"),
                        new Markup($"Biome: [green]{biome}[/]"),
                        new Markup($"Rural Population: [green]{Convert.ToInt32(currentCell.pop * 1000)}[/]"),
                        new Markup($"Province: [green]{province.fullName}[/]"),
                        new Markup($"State: [green]{state.fullName}[/]")
                    ),
                    VerticalAlignment.Middle))
        };
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
            homePanels.Add(new Panel(
                Align.Center(
                    new Rows(
                        new Markup(cityTitle),
                        new Markup($"Population: [green]{Convert.ToInt32(burg.population * 1000)}[/]"),
                        new Text(featuresString)
                    )
                    , VerticalAlignment.Middle)));
        }

        _display.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            homePanels
                        ),
                        VerticalAlignment.Middle))
                .Expand());
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
