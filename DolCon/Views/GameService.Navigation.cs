namespace DolCon.Views;

using System.Drawing;
using Services;
using Spectre.Console;
using Spectre.Console.Rendering;

public partial class GameService
{
    private Dictionary<int, int> _directionOptions = new();

    private void RenderNavigation(char value)
    {
        var currentCell = SaveGameService.CurrentCell;
        var localBurg = currentCell.burg > 0 ? SaveGameService.GetBurg(currentCell.burg) : null;

        if (char.ToLower(value) == 'l' && SaveGameService.Party.Burg != null)
        {
            SaveGameService.Party.Burg = null;
        }

        if (char.ToLower(value) == 'b' && SaveGameService.CurrentBurg is null && localBurg != null)
        {
            SaveGameService.Party.Burg = localBurg.i;
        }

        if (int.TryParse(value.ToString(), out var direction))
        {
            SaveGameService.Party.Cell = _directionOptions[direction];
        }

        currentCell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        localBurg = currentCell.burg > 0 ? SaveGameService.GetBurg(currentCell.burg) : null;

        if (burg != null)
        {
            _display.Update(
                new Panel(
                    Align.Center(
                        new Markup("Burg navigation is not implemented yet!"), VerticalAlignment.Middle
                    )
                ).Expand()
            );
            _ctx.Refresh();
        }
        else
        {
            var table = new Table();

            _display.Update(
                new Panel(
                    new Rows(
                        Align.Center(
                            new Markup(
                                $"Local Burg: [green bold]{(localBurg != null ? localBurg.name : "None")}[/]")),
                        Align.Center(
                            table
                        ))));
            _ctx.Refresh();

            table.AddColumn(new TableColumn("Key"));
            table.AddColumn(new TableColumn("Direction"));
            table.AddColumn(new TableColumn("Province"));
            table.AddColumn(new TableColumn("State"));
            table.AddColumn(new TableColumn("Biome"));
            table.AddColumn(new TableColumn("Burg"));
            _ctx.Refresh();

            _directionOptions = new Dictionary<int, int>();

            var i = 0;

            foreach (var cellId in currentCell.c)
            {
                var cell = SaveGameService.GetCell(cellId);
                var cellBurg = SaveGameService.GetBurg(cell.burg);
                var cellBiome = SaveGameService.GetBiome(cell.biome);
                var cellProvince = SaveGameService.GetProvince(cell.province);
                var cellState = SaveGameService.GetState(cell.state);
                var cellDirection = MapService.GetDirection(new Point((int)currentCell.p[0], (int)currentCell.p[1]),
                    new Point((int)cell.p[0], (int)cell.p[1])
                );
                var key = i++;
                _directionOptions.Add(key, cellId);

                table.AddRow(key.ToString(), cellDirection.ToString(), cellProvince.fullName,
                    cellState.fullName ?? string.Empty, cellBiome, cellBurg?.name ?? "None");

                _ctx.Refresh();
            }
        }

        var controlLines = new List<IRenderable>
        {
            new Markup(
                "[bold]Standard controls[/]: ([green bold]H[/])ome | ([green bold]N[/])avigation | ([Red bold]E[/])xit")
        };

        if (burg != null)
        {
            controlLines.Add(new Markup("To leave burg press [green bold]L[/]"));
        }
        else if (localBurg != null)
        {
            controlLines.Add(new Markup("To enter burg press [green bold]B[/]"));
            controlLines.Add(new Markup("Using the table above, press the number of the direction you want to go to."));
        }
        else
        {
            controlLines.Add(new Markup("Using the table above, press the number of the direction you want to go to."));
        }


        _controls.Update(
            new Panel(
                    Align.Center(
                        new Rows(
                            controlLines.ToArray()
                        ),
                        VerticalAlignment.Middle))
                .Expand());
        _ctx.Refresh();
    }
}
