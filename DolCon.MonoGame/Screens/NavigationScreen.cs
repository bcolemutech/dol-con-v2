using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Services;
using DolCon.MonoGame.Input;
using Direction = DolCon.Core.Enums.Direction;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Navigation screen for moving between cells, burgs, and locations.
/// </summary>
public class NavigationScreen : ScreenBase
{
    private readonly IMoveService _moveService;
    private readonly IEventService _eventService;
    private List<NavigationOption> _options = new();
    private int _selectedIndex;
    private int _scrollOffset;
    private const int VisibleItems = 12;
    private string _message = "";
    private Scene _currentScene = new();

    private record NavigationOption(string Label, int? CellId, Guid? LocationId, bool IsExplore = false, bool IsCamp = false, bool IsEnterBurg = false);

    // Separate lists for cells and locations
    private List<CellOption> _cellOptions = new();
    private List<LocationOption> _locationOptions = new();

    private record CellOption(int CellId, Direction Direction, string Biome, string Province, string Burg, string Explored);
    private record LocationOption(Guid LocationId, string Name, string Type, string Explored);

    public NavigationScreen(IMoveService moveService, IEventService eventService)
    {
        _moveService = moveService;
        _eventService = eventService;
    }

    public override void Initialize()
    {
        _message = "";
        _currentScene = new Scene();
        _selectedIndex = 0;
        _scrollOffset = 0;
        BuildOptions();
    }

    private void BuildOptions()
    {
        _options.Clear();
        _cellOptions.Clear();
        _locationOptions.Clear();
        _selectedIndex = 0;
        _scrollOffset = 0;

        var cell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        var location = SaveGameService.CurrentLocation;

        if (location != null)
        {
            // Only show explore option for explorable locations
            if (location.Type.Size != LocationSize.unexplorable)
            {
                if (location.ExploredPercent < 1)
                {
                    _options.Add(new NavigationOption($"Explore {location.Name} ({location.ExploredPercent:P0} explored)", null, null, IsExplore: true));
                }
                else
                {
                    _options.Add(new NavigationOption($"{location.Name} - Fully explored", null, null, IsExplore: false));
                }
                // Add camp option at explorable locations
                _options.Add(new NavigationOption("Camp here (restore stamina to 50%)", null, null, IsCamp: true));
            }
            else
            {
                // Unexplorable location - show services available (use lodging instead of camping)
                _options.Add(new NavigationOption($"{location.Name} - Services available", null, null, IsExplore: false));
            }
            return;
        }

        if (burg != null)
        {
            // In a burg - show locations (burgs are settlements, exploration is via locations)
            foreach (var loc in burg.locations)
            {
                // Show "Services" for unexplorable locations instead of exploration percentage
                var exploredStr = loc.Type.Size == LocationSize.unexplorable
                    ? "Services"
                    : (loc.ExploredPercent > 0 ? $"{loc.ExploredPercent:P0}" : "Unexplored");
                var typeStr = loc.Type.Size.ToString();
                _locationOptions.Add(new LocationOption(loc.Id, loc.Name, typeStr, exploredStr));
                _options.Add(new NavigationOption($"{loc.Name} ({typeStr})", null, loc.Id));
            }
        }
        else
        {
            // In a cell - show explore option, camp option, enter burg option, directions to neighboring cells, and local locations
            if (cell.ExploredPercent < 1)
            {
                _options.Add(new NavigationOption($"Explore area ({cell.ExploredPercent:P0} explored)", null, null, IsExplore: true));
            }
            // Add camp option in wilderness
            _options.Add(new NavigationOption("Camp in wilderness (restore stamina to 50%)", null, null, IsCamp: true));

            // Check if there's a burg in this cell that we can enter
            var cellBurg = SaveGameService.GetBurg(cell.burg);
            if (cellBurg != null)
            {
                _options.Add(new NavigationOption($"Enter {cellBurg.name} ({cellBurg.size})", null, null, IsEnterBurg: true));
            }

            // Add directions to neighboring cells with direction info
            foreach (var neighbor in cell.c)
            {
                if (neighbor >= 0)
                {
                    var targetCell = SaveGameService.GetCell(neighbor);
                    var biome = SaveGameService.GetBiome(targetCell.biome);
                    var province = SaveGameService.GetProvince(targetCell.province);
                    var targetBurg = SaveGameService.GetBurg(targetCell.burg);
                    var burgName = targetBurg?.name ?? "None";
                    var exploredStr = targetCell.ExploredPercent < 1 ? $"{targetCell.ExploredPercent:P0}" : "Explored";

                    // Get direction from current cell to target cell
                    var direction = MapService.GetDirection(
                        cell.p[0], cell.p[1],
                        targetCell.p[0], targetCell.p[1]);

                    _cellOptions.Add(new CellOption(neighbor, direction, biome, province.fullName, burgName, exploredStr));
                    _options.Add(new NavigationOption($"{direction} - {biome} ({province.fullName})", neighbor, null));
                }
            }

            // Add discovered cell locations
            foreach (var loc in cell.locations.Where(l => l.Discovered))
            {
                // Show "Services" for unexplorable locations instead of exploration percentage
                var exploredStr = loc.Type.Size == LocationSize.unexplorable
                    ? "Services"
                    : (loc.ExploredPercent > 0 ? $"{loc.ExploredPercent:P0}" : "Unexplored");
                var typeStr = loc.Type.Size.ToString();
                _locationOptions.Add(new LocationOption(loc.Id, loc.Name, typeStr, exploredStr));
                _options.Add(new NavigationOption($"{loc.Name} ({typeStr})", null, loc.Id));
            }
        }
    }

    public override void Update(GameTime gameTime, InputManager input)
    {
        // Screen navigation
        if (input.IsKeyPressed(Keys.H))
        {
            ScreenManager.SwitchTo(ScreenType.Home);
            return;
        }
        else if (input.IsKeyPressed(Keys.I))
        {
            ScreenManager.SwitchTo(ScreenType.Inventory);
            return;
        }
        else if (input.IsKeyPressed(Keys.Escape))
        {
            // Leave current location/burg
            var party = SaveGameService.Party;
            if (party.Location.HasValue)
            {
                party.Location = null;
                _message = "Left location";
                BuildOptions();
            }
            else if (party.Burg.HasValue)
            {
                party.Burg = null;
                _message = "Left burg";
                BuildOptions();
            }
            else
            {
                ScreenManager.SwitchTo(ScreenType.Home);
            }
            return;
        }

        if (_options.Count == 0) return;

        // Arrow key navigation
        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedIndex = Math.Max(0, _selectedIndex - 1);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedIndex = Math.Min(_options.Count - 1, _selectedIndex + 1);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.PageUp))
        {
            _selectedIndex = Math.Max(0, _selectedIndex - VisibleItems);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.PageDown))
        {
            _selectedIndex = Math.Min(_options.Count - 1, _selectedIndex + VisibleItems);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.C))
        {
            // Quick camp hotkey
            ProcessCamp();
        }
        else if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
        {
            if (_options.Count > 0 && _selectedIndex < _options.Count)
            {
                var option = _options[_selectedIndex];
                if (option.IsExplore)
                {
                    ProcessExploration();
                }
                else if (option.IsCamp)
                {
                    ProcessCamp();
                }
                else if (option.IsEnterBurg)
                {
                    ProcessEnterBurg();
                }
                else
                {
                    ProcessSelection();
                }
            }
        }
    }

    private void ProcessExploration()
    {
        var location = SaveGameService.CurrentLocation;
        var cell = SaveGameService.CurrentCell;
        var thisEvent = new Event(location, cell);
        _currentScene = _eventService.ProcessEvent(thisEvent);

        if (_currentScene.Type == SceneType.Battle)
        {
            // Use SwitchToBattle to pass scene with pending exploration
            ScreenManager.SwitchToBattle(_currentScene);
        }
        else if (_currentScene.Type == SceneType.Shop)
        {
            ScreenManager.SwitchToShop(_currentScene);
        }
        else
        {
            _message = _currentScene.Message ?? "Exploration complete";
            BuildOptions();
        }
    }

    private void ProcessCamp()
    {
        var party = SaveGameService.Party;
        var previousStamina = party.Stamina;

        // Sleep with no quality = wilderness camping (restores to 50%)
        if (_moveService.Sleep())
        {
            var newStamina = party.Stamina;
            _message = $"You set up camp and rest. Stamina restored from {previousStamina:P0} to {newStamina:P0}.";
            SaveHelper.TriggerSave();
        }
        else
        {
            _message = "You're not tired enough to camp here. (Stamina must be below 50%)";
        }
        BuildOptions();
    }

    private void ProcessEnterBurg()
    {
        var cell = SaveGameService.CurrentCell;
        var cellBurg = SaveGameService.GetBurg(cell.burg);
        if (cellBurg != null)
        {
            // Enter the burg by setting the party's burg
            var party = SaveGameService.Party;
            party.Burg = cellBurg.i;
            _message = $"Entered {cellBurg.name}";
            SaveHelper.TriggerSave();
            BuildOptions();
        }
    }

    private void EnsureVisible()
    {
        if (_selectedIndex < _scrollOffset)
        {
            _scrollOffset = _selectedIndex;
        }
        else if (_selectedIndex >= _scrollOffset + VisibleItems)
        {
            _scrollOffset = _selectedIndex - VisibleItems + 1;
        }
    }

    private void ProcessSelection()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _options.Count) return;

        var option = _options[_selectedIndex];

        try
        {
            if (option.CellId.HasValue)
            {
                // Move to cell
                var result = _moveService.MoveToCell(option.CellId.Value);
                switch (result)
                {
                    case MoveStatus.Success:
                        _message = "Moved to new cell";
                        ProcessEvent();
                        SaveHelper.TriggerSave();
                        break;
                    case MoveStatus.Failure:
                        _message = "Not enough stamina!";
                        break;
                    case MoveStatus.Blocked:
                        _message = "Cannot move there!";
                        break;
                }
                BuildOptions();
            }
            else if (option.LocationId.HasValue)
            {
                // Move to location
                if (_moveService.MoveToLocation(option.LocationId.Value))
                {
                    _message = "Entered location";
                    ProcessEvent();
                    SaveHelper.TriggerSave();
                }
                else
                {
                    _message = "Not enough stamina!";
                }
                BuildOptions();
            }
        }
        catch (Exception ex)
        {
            _message = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"NavigationScreen.ProcessSelection error: {ex}");
            BuildOptions();
        }
    }

    private void ProcessEvent()
    {
        try
        {
            var location = SaveGameService.CurrentLocation;
            var cell = SaveGameService.CurrentCell;
            var thisEvent = new Event(location, cell);
            _currentScene = _eventService.ProcessEvent(thisEvent);

            if (_currentScene.Type == SceneType.Battle)
            {
                // Switch to battle screen
                ScreenManager.SwitchTo(ScreenType.Battle);
            }
            else if (_currentScene.Type == SceneType.Shop)
            {
                // Switch to shop screen
                ScreenManager.SwitchToShop(_currentScene);
            }
        }
        catch (Exception ex)
        {
            _message = $"Error entering location: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"NavigationScreen.ProcessEvent error: {ex}");
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var padding = 20;
        var viewport = GraphicsDevice.Viewport;

        // Title bar
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, 50), new Color(30, 30, 50));
        DrawText(spriteBatch, "DOMINION OF LIGHT - Navigation", new Vector2(padding, 15), Color.Gold);

        // Message
        if (!string.IsNullOrEmpty(_message))
        {
            DrawText(spriteBatch, _message, new Vector2(padding, 60), Color.Yellow);
        }

        // Current location info
        var cell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        var location = SaveGameService.CurrentLocation;

        int y = 100;
        DrawText(spriteBatch, $"Current Cell: {cell.i} ({SaveGameService.CurrentBiome})",
            new Vector2(padding, y), Color.White);
        y += 30;

        if (burg != null)
        {
            DrawText(spriteBatch, $"In Burg: {burg.name} ({burg.size})", new Vector2(padding, y), Color.LightBlue);
            y += 30;
        }
        else
        {
            // Check if there's a burg in this cell we could enter
            var cellBurg = SaveGameService.GetBurg(cell.burg);
            if (cellBurg != null)
            {
                DrawText(spriteBatch, $"Nearby Burg: {cellBurg.name} ({cellBurg.size})", new Vector2(padding, y), Color.Cyan);
                y += 30;
            }
        }

        if (location != null)
        {
            DrawText(spriteBatch, $"At Location: {location.Name}", new Vector2(padding, y), Color.LightGreen);
            y += 30;
            DrawText(spriteBatch, "Press ESC to leave", new Vector2(padding, y), Color.Gray);
            y += 30;
        }

        y += 20;

        // Options header
        if (_options.Count > 0)
        {
            // Calculate visible range based on scroll offset
            var visibleStart = _scrollOffset;
            var visibleEnd = Math.Min(_options.Count, _scrollOffset + VisibleItems);
            var itemsDrawn = 0;

            // Count special options (explore, camp, enter burg) that come before cell/location options
            var specialOptions = _options.Where(o => !o.CellId.HasValue && !o.LocationId.HasValue).ToList();
            var specialOptionsCount = specialOptions.Count;
            var cellStartIndex = specialOptionsCount;
            var locStartIndex = cellStartIndex + _cellOptions.Count;

            // Draw special options (explore, camp, enter burg)
            for (int i = 0; i < specialOptions.Count && itemsDrawn < VisibleItems; i++)
            {
                if (i < visibleStart || i >= visibleEnd) continue;

                var opt = specialOptions[i];
                var isSelected = _selectedIndex == i;
                var optColor = isSelected ? Color.Yellow :
                               opt.IsExplore ? Color.Cyan :
                               opt.IsCamp ? Color.LightGreen :
                               opt.IsEnterBurg ? Color.LightBlue : Color.White;
                var prefix = isSelected ? "> " : "  ";
                DrawText(spriteBatch, $"{prefix}{opt.Label}", new Vector2(padding, y), optColor);
                y += 28;
                itemsDrawn++;
            }

            // Draw cells section if we have cell options and any are visible
            if (_cellOptions.Count > 0)
            {
                var cellEndIndex = cellStartIndex + _cellOptions.Count;

                // Check if any cells fall within visible range
                if (visibleStart < cellEndIndex && visibleEnd > cellStartIndex)
                {
                    // Column positions for table layout - expanded to use more width
                    var colDirection = padding + 40;
                    var colBiome = padding + 170;
                    var colProvince = padding + 340;
                    var colBurg = padding + 560;

                    DrawText(spriteBatch, "Nearby Cells:", new Vector2(padding, y), Color.White);
                    y += 25;

                    // Header row
                    DrawText(spriteBatch, "Direction", new Vector2(colDirection, y), Color.Gray);
                    DrawText(spriteBatch, "Biome", new Vector2(colBiome, y), Color.Gray);
                    DrawText(spriteBatch, "Province", new Vector2(colProvince, y), Color.Gray);
                    DrawText(spriteBatch, "Burg", new Vector2(colBurg, y), Color.Gray);
                    y += 22;

                    // Separator line - extended to match wider table
                    DrawRect(spriteBatch, new Rectangle(padding + 15, y, viewport.Width - 100, 1), Color.DarkGray);
                    y += 5;

                    // Calculate which cells are visible
                    var cellVisibleStart = Math.Max(0, visibleStart - cellStartIndex);
                    var cellVisibleEnd = Math.Min(_cellOptions.Count, visibleEnd - cellStartIndex);

                    for (int i = cellVisibleStart; i < cellVisibleEnd && itemsDrawn < VisibleItems; i++)
                    {
                        var actualIndex = cellStartIndex + i;
                        var cellOpt = _cellOptions[i];
                        var isSelected = actualIndex == _selectedIndex;

                        if (isSelected)
                        {
                            DrawRect(spriteBatch, new Rectangle(padding + 15, y - 2, viewport.Width - 100, 22), new Color(60, 60, 80));
                        }

                        var prefix = isSelected ? "> " : "  ";
                        var color = isSelected ? Color.Yellow : Color.LightGray;

                        // Draw each column separately for proper alignment
                        DrawText(spriteBatch, prefix, new Vector2(padding + 20, y), color);
                        DrawText(spriteBatch, cellOpt.Direction.ToString(), new Vector2(colDirection, y), color);
                        DrawText(spriteBatch, TruncateText(cellOpt.Biome, 16), new Vector2(colBiome, y), color);
                        DrawText(spriteBatch, TruncateText(cellOpt.Province, 22), new Vector2(colProvince, y), color);
                        DrawText(spriteBatch, TruncateText(cellOpt.Burg, 25), new Vector2(colBurg, y), color);
                        y += 22;
                        itemsDrawn++;
                    }
                    y += 10;
                }
            }

            // Draw locations section if we have location options and any are visible
            if (_locationOptions.Count > 0)
            {
                var locEndIndex = locStartIndex + _locationOptions.Count;

                // Check if any locations fall within visible range
                if (visibleStart < locEndIndex && visibleEnd > locStartIndex)
                {
                    DrawText(spriteBatch, "Discovered Locations:", new Vector2(padding, y), Color.White);
                    y += 25;

                    // Calculate which locations are visible
                    var locVisibleStart = Math.Max(0, visibleStart - locStartIndex);
                    var locVisibleEnd = Math.Min(_locationOptions.Count, visibleEnd - locStartIndex);

                    for (int i = locVisibleStart; i < locVisibleEnd && itemsDrawn < VisibleItems; i++)
                    {
                        var actualIndex = locStartIndex + i;
                        var locOpt = _locationOptions[i];
                        var isSelected = actualIndex == _selectedIndex;

                        if (isSelected)
                        {
                            DrawRect(spriteBatch, new Rectangle(padding + 15, y - 2, viewport.Width - 100, 22), new Color(60, 60, 80));
                        }

                        var prefix = isSelected ? "> " : "  ";
                        var color = isSelected ? Color.Yellow : Color.LightGreen;
                        DrawText(spriteBatch, $"{prefix}{locOpt.Name} ({locOpt.Type}) - {locOpt.Explored}",
                            new Vector2(padding + 20, y), color);
                        y += 22;
                        itemsDrawn++;
                    }
                }
            }

            // Scroll indicator
            if (_options.Count > VisibleItems)
            {
                var scrollText = $"({_scrollOffset + 1}-{Math.Min(_scrollOffset + VisibleItems, _options.Count)} of {_options.Count})";
                DrawText(spriteBatch, scrollText, new Vector2(viewport.Width - 200, 180), Color.Gray);
            }
        }
        else
        {
            DrawText(spriteBatch, "No options available", new Vector2(padding, y), Color.Gray);
        }

        // Stamina display
        var party = SaveGameService.Party;
        DrawText(spriteBatch, $"Stamina: {party.Stamina:P0}",
            new Vector2(viewport.Width - 200, 100), Color.Green);

        // Controls panel (bottom)
        var controlsY = viewport.Height - 80;
        DrawRect(spriteBatch, new Rectangle(0, controlsY, viewport.Width, 80), new Color(30, 30, 50));
        var canCamp = _options.Any(o => o.IsCamp);
        var canExplore = _options.Any(o => o.IsExplore);
        var controlsText = canExplore && canCamp
            ? "[Up/Down] Navigate  [Enter] Select  [C] Camp  [H] Home  [I] Inventory  [ESC] Back"
            : canCamp
            ? "[Up/Down] Navigate  [Enter] Select  [C] Camp  [H] Home  [I] Inventory  [ESC] Back"
            : "[Up/Down] Navigate  [Enter] Select  [H] Home  [I] Inventory  [ESC] Back";
        DrawText(spriteBatch, controlsText, new Vector2(padding, controlsY + 25), Color.Gray);
    }

    /// <summary>
    /// Truncates text to a maximum length, adding ".." if truncated.
    /// </summary>
    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? "";
        return text.Substring(0, maxLength - 2) + "..";
    }
}
