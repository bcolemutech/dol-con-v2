namespace DolCon.Views;

using ChanceNET;
using Enums;
using Models.BaseTypes;
using Services;
using Spectre.Console;
using Spectre.Console.Rendering;
using Location = Models.Location;

public partial class GameService
{
    private Dictionary<int, int> _directionOptions = new();
    private Dictionary<int, Guid> _locationOptions = new();

    private void RenderNavigation(ConsoleKeyInfo value)
    {
        var currentCell = SaveGameService.CurrentCell;
        var localBurg = currentCell.burg > 0 ? SaveGameService.GetBurg(currentCell.burg) : null;

        ProcessKey(value, localBurg);

        currentCell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        var location = SaveGameService.CurrentLocation;
        _locationOptions = new Dictionary<int, Guid>();
        _directionOptions = new Dictionary<int, int>();
        localBurg = currentCell.burg > 0 ? SaveGameService.GetBurg(currentCell.burg) : null;

        if (location != null)
        {
            RenderLocationNavigation(location);
        }
        else if (burg != null)
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
                "[bold]Standard controls[/]: ([green bold]H[/])ome | ([green bold]N[/])avigation | ([Red bold]Alt+E[/])xit")
        };

        if (location != null)
        {
            if (location.Type.Size != LocationSize.unexplorable && location.ExploredPercent < 1)
            {
                controlLines.Add(new Markup("To explore the location press [green bold]E[/]"));
            }

            if (location.Type.Size == LocationSize.unexplorable)
            {
                controlLines.Add(new Markup("To enter the location press [green bold]E[/]"));
            }

            controlLines.Add(new Markup("To leave the location press [green bold]L[/]"));
        }
        else if (burg != null)
        {
            controlLines.Add(new Markup("To leave burg press [green bold]L[/]"));
        }
        else
        {
            if (localBurg != null)
            {
                controlLines.Add(new Markup("To enter burg press [green bold]B[/]"));
            }

            if (currentCell.ExploredPercent < 1)
            {
                controlLines.Add(new Markup("To explore the area press [green bold]E[/]"));
            }

            controlLines.Add(
                new Markup("Using the table above, press the number of the direction or location you want to go to."));
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

    private void ProcessKey(ConsoleKeyInfo value, Burg? localBurg)
    {
        switch (char.ToLower(value.KeyChar))
        {
            case 'l' when
                (SaveGameService.Party.Burg != null || SaveGameService.Party.Location != null):
                SaveGameService.Party.Burg = SaveGameService.Party.Location == null ? null : SaveGameService.Party.Burg;
                SaveGameService.Party.Location = null;
                break;
            case 'b' when SaveGameService.CurrentBurg is null && localBurg != null:
                SaveGameService.Party.Burg = localBurg.i;
                break;
            case 'e' when (SaveGameService.CurrentLocation != null &&
                           SaveGameService.CurrentLocation.Type.Size != LocationSize.unexplorable &&
                           SaveGameService.CurrentLocation.ExploredPercent < 1) ||
                          SaveGameService.CurrentCell.ExploredPercent < 1:
                ProcessExploration();
                break;
            default:
            {
                var thisChar = value.KeyChar.ToString();
                var cleanChar = thisChar.First().ToString();
                var tryParse = int.TryParse(cleanChar, out var selection);
                selection = value.Modifiers == ConsoleModifiers.Alt ? selection + 10 : selection;
                if (tryParse && _directionOptions.TryGetValue(selection, out var option))
                {
                    SaveGameService.Party.Cell = option;
                    _imageService.ProcessSvg();
                }
                else if (tryParse && _locationOptions.TryGetValue(selection, out var locationId))
                {
                    SaveGameService.Party.Location = locationId;
                }

                break;
            }
        }
    }

    private void ProcessExploration()
    {
        var defaultExploration = 100;
        var currentLocation = SaveGameService.CurrentLocation;
        var locationExplorationSize = 0;
        double explored = 0;
        var currentBurg = SaveGameService.CurrentBurg;
        var inBurg = currentBurg is not null;
        if (inBurg)
        {
            locationExplorationSize = (int)currentBurg.size * 100;
            explored = currentLocation.ExploredPercent * locationExplorationSize;

            explored += defaultExploration;

            currentLocation.ExploredPercent = explored / locationExplorationSize;
        }
        else
        {
            var currentCell = SaveGameService.CurrentCell;

            locationExplorationSize = currentCell.CellSize == CellSize.small ? 300 : 500;

            explored = currentCell.ExploredPercent * locationExplorationSize;
            explored += defaultExploration;
            currentCell.ExploredPercent = explored / locationExplorationSize;

            if (Math.Abs(currentCell.ExploredPercent - 1) < .01)
            {
                currentCell.ExploredPercent = 1;
                currentCell.locations.ForEach(x => x.Discovered = true);
                return;
            }

            var chance = new Chance();
            var dice = chance.Dice(20);
            if (dice > 5)
            {
                var random1 = new Random();
                var pick1 = random1.Next(0, currentCell.locations.Count(x => !x.Discovered));
                var location1 = currentCell.locations.Where(x => !x.Discovered).Skip(pick1)
                    .Take(1).First();
                location1.Discovered = true;
            }

            if (dice < 18) return;

            var random2 = new Random();
            var pick2 = random2.Next(0, currentCell.locations.Count(x => !x.Discovered));
            var location2 = currentCell.locations.Where(x => !x.Discovered).Skip(pick2)
                .Take(1).First();
            location2.Discovered = true;
        }
    }

    private void RenderLocationNavigation(Location location)
    {
        var explorationString = location.ExploredPercent < 1 && location.Type.Size != LocationSize.unexplorable
            ? $"[green bold]{location.ExploredPercent * 100}%[/] explored"
            : "[green bold]Fully explored[/]";
        _display.Update(
            new Panel(
                Align.Center(
                    new Rows(
                        new Markup($"Current Location: [green bold]{location.Name}[/]"),
                        new Markup("[red bold]Location navigation is not currently implemented[/]"),
                        new Markup(explorationString),
                        new Markup("To leave location press [green bold]L[/]")
                    ))));
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

        var i = 0;

        foreach (var location in burg.locations)
        {
            var key = i++;
            _locationOptions.Add(key, location.Id);
            locationsTable.AddRow(
                key < 10 ? $"{key}" : $"Alt+{key - 10}",
                location.Name,
                location.Type.Type,
                location.Rarity.ToString());
            _ctx.Refresh();
        }
    }

    private void RenderCellNavigation(Burg? localBurg, Cell currentCell)
    {
        var cellsTable = new Table();
        var locationsTable = new Table();

        var explorationString = currentCell.ExploredPercent < 1
            ? $"[green bold]{currentCell.ExploredPercent * 100}%[/] explored"
            : "[green bold]Fully explored[/]";

        _display.Update(
            new Panel(
                new Rows(
                    Align.Center(
                        new Markup(
                            $"Local Burg: [green bold]{(localBurg != null ? localBurg.name : "None")}[/]")),
                    Align.Center(
                        new Markup(explorationString)
                    ),
                    Align.Center(
                        new Markup("Select from the table below to move to a new cell.")
                    ),
                    Align.Center(
                        cellsTable
                    ),
                    Align.Center(
                        new Markup("Select from the table below to move to a new location.")
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


        foreach (var location in currentCell.locations.Where(x => x.Discovered))
        {
            var key = i++;
            var keyString = key < 10 ? $"{key}" : $"Alt+{key - 10}";
            _locationOptions.Add(key, location.Id);

            locationsTable.AddRow(keyString, location.Name, location.Type.Type, location.Rarity.ToString());
            _ctx.Refresh();
        }
    }
}
