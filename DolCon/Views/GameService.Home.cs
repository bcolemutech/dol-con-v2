namespace DolCon.Views;

using Services;
using Spectre.Console;

public partial class GameService
{
    private void RenderHome()
    {
        var currentCell = SaveGameService.CurrentMap.cells.cells.First(x => x.i == SaveGameService.Party.Cell);
        var burg = SaveGameService.CurrentMap.cells.burgs.FirstOrDefault(x => x.i == SaveGameService.Party.Burg);
        var biome = SaveGameService.CurrentMap.biomes.name[currentCell.biome];
        _display.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            new Markup($"Burg name: [green]{burg?.name ?? "None"}[/]"),
                            new Markup("Biome: [green]" + biome + "[/]"),
                            new Markup("Population: [green]" + currentCell.pop + "[/]")
                        ),
                        VerticalAlignment.Middle))
                .Expand());

        _controls.Update(
            new Panel(
                    Align.Center(
                        new Markup("[Green]1[/]: Navigate | [Red]Esc[/]: Exit"), VerticalAlignment.Middle))
                .Expand());
    }
}
