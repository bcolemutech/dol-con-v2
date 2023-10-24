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
            
            controlLines.Add(new Markup("To camp press [green bold]C[/]"));

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
        var moveStatus = MoveStatus.None;
        var message = string.Empty;
        switch (char.ToLower(value.KeyChar))
        {
            case 'l' when
                (SaveGameService.Party.Burg != null || SaveGameService.Party.Location != null):
                SaveGameService.Party.Burg = SaveGameService.Party.Location == null ? null : SaveGameService.Party.Burg;
                SaveGameService.Party.Location = null;
                break;
            case 'b' when SaveGameService.CurrentBurg is null && localBurg != null:
                moveStatus = _moveService.MoveToBurg(localBurg.i.Value) ? MoveStatus.Success : MoveStatus.Failure;
                break;
            case 'e' when SaveGameService.CurrentLocation != null || SaveGameService.CurrentCell.ExploredPercent < 1:
                moveStatus = _moveService.ProcessExploration();
                if (moveStatus == MoveStatus.Success)
                {
                    var totalCoin = 0;
                    foreach (var player in SaveGameService.Party.Players)
                    {
                        var random = new Chance().New();
                        var playerCoin = random.Dice(100) * 10;
                        player.coin += playerCoin;
                        totalCoin += playerCoin;
                    }

                    message = "You have explored the area. You have found " + totalCoin + " coin.";
                }

                break;
            case 'c':
                if (SaveGameService.CurrentLocation != null || SaveGameService.CurrentBurg != null)
                {
                    SetMessage(MessageType.Error, "You cannot camp here.");
                }
                else if (Math.Abs(SaveGameService.Party.Stamina - 1) < .01)
                {
                    SetMessage(MessageType.Info, "You are already fully rested.");
                }
                else
                {
                    _moveService.Camp();
                    SetMessage(MessageType.Success, "You have camped and recovered your stamina.");
                }
                
                moveStatus = MoveStatus.Hold;

                break;
            default:
            {
                var thisChar = value.KeyChar.ToString();
                var cleanChar = thisChar.First().ToString();
                var tryParse = int.TryParse(cleanChar, out var selection);
                selection = value.Modifiers == ConsoleModifiers.Alt ? selection + 10 : selection;
                moveStatus = tryParse switch
                {
                    true when _directionOptions.TryGetValue(selection, out var option) => _moveService.MoveToCell(option),
                    true when _locationOptions.TryGetValue(selection, out var locationId) =>
                        _moveService.MoveToLocation(locationId) ? MoveStatus.Success : MoveStatus.Failure,
                    _ => MoveStatus.None
                };


                break;
            }
        }

        switch (moveStatus)
        {
            case MoveStatus.Success:
                message = message == string.Empty ? "You successfully moved." : message;
                SetMessage(MessageType.Success, message);
                break;
            case MoveStatus.Failure:
                message = message == string.Empty ? "You do not have enough stamina to make that move." : message;
                SetMessage(MessageType.Error, message);
                break;
            case MoveStatus.None:
                message = message == string.Empty ? "Make a move." : message;
                SetMessage(MessageType.Info, message);
                break;
            case MoveStatus.Blocked:
                message = message == string.Empty ? "You cannot move in that direction." : message;
                SetMessage(MessageType.Error, message);
                break;
            case MoveStatus.Hold:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(moveStatus), moveStatus, "Invalid move status.");
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
                        new Markup($"Stamina: [green bold]{SaveGameService.Party.Stamina:P}[/]"),
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
                        new Markup(
                            $"Stamina: [green bold]{SaveGameService.Party.Stamina:P}[/]")),
                    Align.Center(
                        locationsTable
                    ))));
        _ctx.Refresh();

        locationsTable.AddColumn(new TableColumn("Key"));
        locationsTable.AddColumn(new TableColumn("Location"));
        locationsTable.AddColumn(new TableColumn("Type"));
        locationsTable.AddColumn(new TableColumn("Rarity"));
        locationsTable.AddColumn(new TableColumn("Explored"));
        _ctx.Refresh();

        var i = 0;

        foreach (var location in burg.locations)
        {
            var key = i++;
            _locationOptions.Add(key, location.Id);
            var exploredString = location.ExploredPercent < 1 && location.Type.Size != LocationSize.unexplorable
                ? $"[green bold]{location.ExploredPercent:P}[/] explored"
                : "[green bold]Fully explored[/]";
            locationsTable.AddRow(
                key < 10 ? $"{key}" : $"Alt+{key - 10}",
                location.Name,
                location.Type.Type,
                location.Rarity.ToString(),
                exploredString);
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
                        new Markup($"Stamina: [green bold]{SaveGameService.Party.Stamina:P}[/]")
                    ),
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
        cellsTable.AddColumn(new TableColumn("Explored"));
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

            var exploredString = cell.ExploredPercent < 1
                ? $"[green bold]{cell.ExploredPercent:P}[/] explored"
                : "[green bold]Fully explored[/]";

            cellsTable.AddRow(key.ToString(), cellDirection.ToString(), cellProvince.fullName,
                cellState.fullName ?? string.Empty, cellBiome, cellBurg?.name ?? "None", exploredString);

            _ctx.Refresh();
        }

        locationsTable.AddColumn(new TableColumn("Key"));
        locationsTable.AddColumn(new TableColumn("Location"));
        locationsTable.AddColumn(new TableColumn("Type"));
        locationsTable.AddColumn(new TableColumn("Rarity"));
        locationsTable.AddColumn(new TableColumn("Explored"));
        _ctx.Refresh();


        foreach (var location in currentCell.locations.Where(x => x.Discovered))
        {
            var key = i++;
            var keyString = key < 10 ? $"{key}" : $"Alt+{key - 10}";
            _locationOptions.Add(key, location.Id);

            var exploredString = location.ExploredPercent < 1 && location.Type.Size != LocationSize.unexplorable
                ? $"[green bold]{location.ExploredPercent:P}[/] explored"
                : "[green bold]Fully explored[/]";

            locationsTable.AddRow(keyString, location.Name, location.Type.Type, location.Rarity.ToString(),
                exploredString);
            _ctx.Refresh();
        }
    }
}
