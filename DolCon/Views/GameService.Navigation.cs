namespace DolCon.Views;

using Models.BaseTypes;
using Services;
using Spectre.Console;
using Spectre.Console.Rendering;

public partial class GameService
{
    private Dictionary<int, int> _directionOptions = new();
    private Dictionary<int, Guid> _locationOptions = new();

    private void RenderNavigation(ConsoleKeyInfo value)
    {
        var currentCell = SaveGameService.CurrentCell;
        var localBurg = currentCell.burg > 0 ? SaveGameService.GetBurg(currentCell.burg) : null;

        if (char.ToLower(value.KeyChar) == 'l' &&
            (SaveGameService.Party.Burg != null || SaveGameService.Party.Location != null))
        {
            SaveGameService.Party.Burg = SaveGameService.Party.Location == null ? null : SaveGameService.Party.Burg;
            SaveGameService.Party.Location = null;
        }
        else if (char.ToLower(value.KeyChar) == 'b' && SaveGameService.CurrentBurg is null && localBurg != null)
        {
            SaveGameService.Party.Burg = localBurg.i;
        }
        else if ((value.Modifiers == ConsoleModifiers.Control || SaveGameService.CurrentBurg is not null) &&
                 int.TryParse(value.KeyChar.ToString(), out var selection))
        {
            SaveGameService.Party.Location = _locationOptions[selection];
        }
        else if (int.TryParse(value.KeyChar.ToString(), out var direction))
        {
            SaveGameService.Party.Cell = _directionOptions[direction];
            _imageService.ProcessSvg();
        }

        currentCell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        localBurg = currentCell.burg > 0 ? SaveGameService.GetBurg(currentCell.burg) : null;

        if (burg != null)
        {
            RenderBurgNavigation(burg);
        }
        else
        {
            RenderCellNavigation(localBurg, currentCell);
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

    private void RenderBurgNavigation(Burg burg)
    {
        var locationsTable = new Table();

        _display.Update(
            new Panel(
                new Rows(
                    Align.Center(
                        new Markup(
                            $"Current Burg: [green bold]{burg.name}[/]")),
                    Align.Center(
                        locationsTable
                    ))));
        _ctx.Refresh();

        locationsTable.AddColumn(new TableColumn("Key"));
        locationsTable.AddColumn(new TableColumn("Location"));
        locationsTable.AddColumn(new TableColumn("Type"));
        locationsTable.AddColumn(new TableColumn("Rarity"));
        _ctx.Refresh();

        _locationOptions = new Dictionary<int, Guid>();

        var i = 0;

        foreach (var location in burg.locations)
        {
            _locationOptions.Add(i, location.Id);
            locationsTable.AddRow(
                $"{i}",
                location.Name,
                location.Type.Type,
                location.Rarity.ToString());
            i++;
            _ctx.Refresh();
        }
    }

    private void RenderCellNavigation(Burg? localBurg, Cell currentCell)
    {
        var cellsTable = new Table();
        var locationsTable = new Table();

        _display.Update(
            new Panel(
                new Rows(
                    Align.Center(
                        new Markup(
                            $"Local Burg: [green bold]{(localBurg != null ? localBurg.name : "None")}[/]")),
                    Align.Center(
                        cellsTable
                    ),
                    Align.Center(
                        locationsTable
                    ))));
        _ctx.Refresh();

        cellsTable.AddColumn(new TableColumn("Key"));
        cellsTable.AddColumn(new TableColumn("Direction"));
        cellsTable.AddColumn(new TableColumn("Province"));
        cellsTable.AddColumn(new TableColumn("State"));
        cellsTable.AddColumn(new TableColumn("Biome"));
        cellsTable.AddColumn(new TableColumn("Burg"));
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
            var cellDirection = MapService.GetDirection(currentCell.p[0], currentCell.p[1], cell.p[0], cell.p[1]);
            var key = i++;
            _directionOptions.Add(key, cellId);

            cellsTable.AddRow(key.ToString(), cellDirection.ToString(), cellProvince.fullName,
                cellState.fullName ?? string.Empty, cellBiome, cellBurg?.name ?? "None");

            _ctx.Refresh();
        }

        locationsTable.AddColumn(new TableColumn("Key"));
        locationsTable.AddColumn(new TableColumn("Location"));
        locationsTable.AddColumn(new TableColumn("Type"));
        locationsTable.AddColumn(new TableColumn("Rarity"));
        _ctx.Refresh();

        _locationOptions = new Dictionary<int, Guid>();

        i = 0;

        foreach (var location in currentCell.locations)
        {
            var key = i++;
            var keyString = $"Ctrl+{key}";
            _locationOptions.Add(key, location.Id);

            locationsTable.AddRow(keyString, location.Name, location.Type.Type, location.Rarity.ToString());
            _ctx.Refresh();
        }
    }
}
