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

    private record NavigationOption(string Label, int? CellId, Guid? LocationId, bool IsExplore = false);

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
            // In a location - show explore option if not fully explored
            if (location.ExploredPercent < 1)
            {
                _options.Add(new NavigationOption($"Explore {location.Name} ({location.ExploredPercent:P0} explored)", null, null, IsExplore: true));
            }
            else
            {
                _options.Add(new NavigationOption($"{location.Name} - Fully explored", null, null, IsExplore: true));
            }
            return;
        }

        if (burg != null)
        {
            // In a burg - show locations (burgs are settlements, exploration is via locations)
            foreach (var loc in burg.locations)
            {
                var exploredStr = loc.ExploredPercent > 0 ? $"{loc.ExploredPercent:P0}" : "Unexplored";
                var typeStr = loc.Type.Size.ToString();
                _locationOptions.Add(new LocationOption(loc.Id, loc.Name, typeStr, exploredStr));
                _options.Add(new NavigationOption($"{loc.Name} ({typeStr})", null, loc.Id));
            }
        }
        else
        {
            // In a cell - show explore option, directions to neighboring cells, and local locations
            if (cell.ExploredPercent < 1)
            {
                _options.Add(new NavigationOption($"Explore area ({cell.ExploredPercent:P0} explored)", null, null, IsExplore: true));
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
                var exploredStr = loc.ExploredPercent > 0 ? $"{loc.ExploredPercent:P0}" : "Unexplored";
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
        else if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
        {
            if (_options.Count > 0 && _selectedIndex < _options.Count && _options[_selectedIndex].IsExplore)
            {
                ProcessExploration();
            }
            else
            {
                ProcessSelection();
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
            ScreenManager.SwitchTo(ScreenType.Battle);
        }
        else
        {
            _message = _currentScene.Message ?? "Exploration complete";
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

    private void ProcessEvent()
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
            // Check if explore option is available
            var hasExploreOption = _options.Any(o => o.IsExplore);
            if (hasExploreOption)
            {
                var exploreOpt = _options.FirstOrDefault(o => o.IsExplore);
                var isExploreSelected = _selectedIndex == 0 && exploreOpt != null && exploreOpt.IsExplore;
                var exploreColor = isExploreSelected ? Color.Yellow : Color.Cyan;
                var explorePrefix = isExploreSelected ? "> " : "  ";
                DrawText(spriteBatch, $"{explorePrefix}{exploreOpt?.Label ?? "Explore"}", new Vector2(padding, y), exploreColor);
                y += 30;
            }

            // Draw cells section if we have cell options
            if (_cellOptions.Count > 0)
            {
                DrawText(spriteBatch, "Nearby Cells:", new Vector2(padding, y), Color.White);
                y += 25;
                DrawText(spriteBatch, "Direction | Biome       | Province    | Burg",
                    new Vector2(padding + 20, y), Color.Gray);
                y += 20;

                var cellStartIndex = hasExploreOption ? 1 : 0;
                for (int i = 0; i < _cellOptions.Count; i++)
                {
                    var actualIndex = cellStartIndex + i;
                    var cellOpt = _cellOptions[i];
                    var isSelected = actualIndex == _selectedIndex;

                    if (isSelected)
                    {
                        DrawRect(spriteBatch, new Rectangle(padding + 15, y - 2, viewport.Width - 250, 22), new Color(60, 60, 80));
                    }

                    var prefix = isSelected ? "> " : "  ";
                    var color = isSelected ? Color.Yellow : Color.LightGray;
                    var displayText = $"{prefix}{cellOpt.Direction,-9} | {cellOpt.Biome,-11} | {cellOpt.Province,-11} | {cellOpt.Burg}";
                    DrawText(spriteBatch, displayText, new Vector2(padding + 20, y), color);
                    y += 22;
                }
                y += 10;
            }

            // Draw locations section if we have location options
            if (_locationOptions.Count > 0)
            {
                DrawText(spriteBatch, "Discovered Locations:", new Vector2(padding, y), Color.White);
                y += 25;

                var locStartIndex = (hasExploreOption ? 1 : 0) + _cellOptions.Count;
                for (int i = 0; i < _locationOptions.Count; i++)
                {
                    var actualIndex = locStartIndex + i;
                    var locOpt = _locationOptions[i];
                    var isSelected = actualIndex == _selectedIndex;

                    if (isSelected)
                    {
                        DrawRect(spriteBatch, new Rectangle(padding + 15, y - 2, viewport.Width - 250, 22), new Color(60, 60, 80));
                    }

                    var prefix = isSelected ? "> " : "  ";
                    var color = isSelected ? Color.Yellow : Color.LightGreen;
                    DrawText(spriteBatch, $"{prefix}{locOpt.Name} ({locOpt.Type}) - {locOpt.Explored}",
                        new Vector2(padding + 20, y), color);
                    y += 22;
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
        var controlsText = _options.Any(o => o.IsExplore)
            ? "[Up/Down] Navigate  [Enter] Select/Explore  [H] Home  [I] Inventory  [ESC] Back"
            : "[Up/Down] Navigate  [Enter] Select  [H] Home  [I] Inventory  [ESC] Back";
        DrawText(spriteBatch, controlsText, new Vector2(padding, controlsY + 25), Color.Gray);
    }
}
