using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Services;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Screen for selecting locations within the current cell or burg.
/// Accessed via L hotkey from Navigation or Home screens.
/// </summary>
public class LocationScreen : ScreenBase
{
    private readonly IMoveService _moveService;
    private readonly IEventService _eventService;
    private List<LocationDisplayItem> _locations = new();
    private int _selectedIndex;
    private int _scrollOffset;
    private const int VisibleItems = 12;
    private string _message = "";
    private Scene _currentScene = new();
    private bool _isInLocation;

    private record LocationDisplayItem(
        Guid Id,
        string Name,
        string Type,
        double ExploredPercent,
        bool HasServices,
        bool IsExplorable);

    public LocationScreen(IMoveService moveService, IEventService eventService)
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
        BuildLocationList();
    }

    private void BuildLocationList()
    {
        _locations.Clear();
        _selectedIndex = 0;
        _scrollOffset = 0;

        var location = SaveGameService.CurrentLocation;
        var burg = SaveGameService.CurrentBurg;
        var cell = SaveGameService.CurrentCell;

        _isInLocation = location != null;

        if (location != null)
        {
            // Already in a location - just show current location info
            return;
        }

        if (burg != null)
        {
            // In a burg - show burg locations
            foreach (var loc in burg.locations)
            {
                var isExplorable = loc.Type.Size != LocationSize.unexplorable;
                _locations.Add(new LocationDisplayItem(
                    loc.Id,
                    loc.Name,
                    loc.Type.Size.ToString(),
                    loc.ExploredPercent,
                    !isExplorable,
                    isExplorable));
            }
        }
        else
        {
            // In wilderness - show discovered cell locations
            foreach (var loc in cell.locations.Where(l => l.Discovered))
            {
                var isExplorable = loc.Type.Size != LocationSize.unexplorable;
                _locations.Add(new LocationDisplayItem(
                    loc.Id,
                    loc.Name,
                    loc.Type.Size.ToString(),
                    loc.ExploredPercent,
                    !isExplorable,
                    isExplorable));
            }

            // Also check if there's a burg we can enter
            var cellBurg = SaveGameService.GetBurg(cell.burg);
            if (cellBurg != null)
            {
                // Add burg as a special "location" entry
                _locations.Insert(0, new LocationDisplayItem(
                    Guid.Empty, // Special marker for burg
                    cellBurg.name,
                    $"Burg ({cellBurg.size})",
                    0,
                    true,
                    false));
            }
        }
    }

    public override void Update(GameTime gameTime, InputManager input)
    {
        // Handle screen navigation
        if (input.IsKeyPressed(Keys.H))
        {
            ScreenManager.SwitchTo(ScreenType.Home);
            return;
        }

        if (input.IsKeyPressed(Keys.N))
        {
            ScreenManager.SwitchTo(ScreenType.Navigation);
            return;
        }

        if (input.IsKeyPressed(Keys.Escape))
        {
            if (_isInLocation)
            {
                // Leave current location
                var party = SaveGameService.Party;
                party.Location = null;
                SaveHelper.TriggerSave();
                BuildLocationList();
                _message = "Left location.";
            }
            else
            {
                ScreenManager.SwitchTo(ScreenType.Navigation);
            }
            return;
        }

        // Handle exploration if in a location
        if (_isInLocation)
        {
            var location = SaveGameService.CurrentLocation!;

            if (input.IsKeyPressed(Keys.E) && location.Type.Size != LocationSize.unexplorable && location.ExploredPercent < 1)
            {
                ProcessExploration(location);
                return;
            }

            if (input.IsKeyPressed(Keys.C) && location.Type.Size != LocationSize.unexplorable)
            {
                ProcessCamp();
                return;
            }
            return;
        }

        // Handle list navigation
        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedIndex = Math.Max(0, _selectedIndex - 1);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedIndex = Math.Min(_locations.Count - 1, _selectedIndex + 1);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.PageUp))
        {
            _selectedIndex = Math.Max(0, _selectedIndex - VisibleItems);
            EnsureVisible();
        }
        else if (input.IsKeyPressed(Keys.PageDown))
        {
            _selectedIndex = Math.Min(_locations.Count - 1, _selectedIndex + VisibleItems);
            EnsureVisible();
        }

        // Handle number key selection (1-9)
        var numKey = input.GetPressedNumericKey();
        if (numKey.HasValue && numKey.Value >= 1 && numKey.Value <= 9)
        {
            var index = numKey.Value - 1;
            if (index < _locations.Count)
            {
                _selectedIndex = index;
                ProcessSelection();
            }
        }

        // Handle enter/space to select
        if ((input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space)) && _locations.Count > 0)
        {
            ProcessSelection();
        }
    }

    private void ProcessSelection()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _locations.Count) return;

        var selected = _locations[_selectedIndex];

        // Check for burg entry (special case with Guid.Empty)
        if (selected.Id == Guid.Empty)
        {
            ProcessEnterBurg();
            return;
        }

        // Move to location
        if (_moveService.MoveToLocation(selected.Id))
        {
            _message = $"Entered {selected.Name}";
            _isInLocation = true;

            // Check for events at this location
            var thisEvent = new Event(SaveGameService.CurrentLocation, SaveGameService.CurrentCell);
            _currentScene = _eventService.ProcessEvent(thisEvent);

            if (_currentScene.Type == SceneType.Battle)
            {
                ScreenManager.SwitchToBattle(_currentScene);
                return;
            }
            if (_currentScene.Type == SceneType.Shop)
            {
                ScreenManager.SwitchToShop(_currentScene);
                return;
            }

            BuildLocationList();
            SaveHelper.TriggerSave();
        }
        else
        {
            _message = "Not enough stamina!";
        }
    }

    private void ProcessEnterBurg()
    {
        var cell = SaveGameService.CurrentCell;
        var burg = SaveGameService.GetBurg(cell.burg);

        if (burg != null)
        {
            var party = SaveGameService.Party;
            party.Burg = burg.i;
            _message = $"Entered {burg.name}";
            BuildLocationList();
            SaveHelper.TriggerSave();
        }
    }

    private void ProcessExploration(Location location)
    {
        var cell = SaveGameService.CurrentCell;
        var thisEvent = new Event(location, cell);
        _currentScene = _eventService.ProcessEvent(thisEvent);

        if (_currentScene.Type == SceneType.Battle)
        {
            ScreenManager.SwitchToBattle(_currentScene);
            return;
        }
        if (_currentScene.Type == SceneType.Shop)
        {
            ScreenManager.SwitchToShop(_currentScene);
            return;
        }

        _message = _currentScene.Message ?? "Explored the area.";
        BuildLocationList();
        SaveHelper.TriggerSave();
    }

    private void ProcessCamp()
    {
        var party = SaveGameService.Party;
        var previousStamina = party.Stamina;

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

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = GraphicsDevice.Viewport;
        var padding = 20;
        var titleHeight = 50;

        // Title bar
        var titleRect = new Rectangle(0, 0, viewport.Width, titleHeight);
        DrawRect(spriteBatch, titleRect, new Color(30, 30, 50));
        DrawCenteredText(spriteBatch, "DOMINION OF LIGHT - Locations", 15, Color.Gold);

        // Current context info
        var y = titleHeight + padding;

        var cell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        var location = SaveGameService.CurrentLocation;
        var biome = SaveGameService.GetBiome(cell.biome);

        if (location != null)
        {
            // Show current location info
            DrawText(spriteBatch, $"Current Location: {location.Name}", new Vector2(padding, y), Color.LightGreen);
            y += 25;
            DrawText(spriteBatch, $"Type: {location.Type.Size}", new Vector2(padding, y), Color.LightGray);
            y += 25;

            if (location.Type.Size != LocationSize.unexplorable)
            {
                var exploredText = location.ExploredPercent >= 1
                    ? "Fully Explored"
                    : $"{location.ExploredPercent:P0} explored";
                DrawText(spriteBatch, $"Exploration: {exploredText}", new Vector2(padding, y), Color.Cyan);
                y += 35;

                // Show available actions
                DrawText(spriteBatch, "Actions:", new Vector2(padding, y), Color.White);
                y += 25;

                if (location.ExploredPercent < 1)
                {
                    DrawText(spriteBatch, "[E] Explore", new Vector2(padding + 20, y), Color.Yellow);
                    y += 22;
                }
                DrawText(spriteBatch, "[C] Camp (restore stamina to 50%)", new Vector2(padding + 20, y), Color.LightGreen);
                y += 22;
                DrawText(spriteBatch, "[ESC] Leave location", new Vector2(padding + 20, y), Color.Gray);
            }
            else
            {
                DrawText(spriteBatch, "Services available here.", new Vector2(padding, y), Color.Cyan);
                y += 35;
                DrawText(spriteBatch, "[ESC] Leave location", new Vector2(padding + 20, y), Color.Gray);
            }
        }
        else if (burg != null)
        {
            DrawText(spriteBatch, $"In Burg: {burg.name} ({burg.size})", new Vector2(padding, y), Color.Cyan);
            y += 25;
            DrawText(spriteBatch, $"Biome: {biome}", new Vector2(padding, y), Color.LightGray);
            y += 35;

            DrawLocationList(spriteBatch, y);
        }
        else
        {
            DrawText(spriteBatch, $"Wilderness - Cell {cell.i}", new Vector2(padding, y), Color.LightGray);
            y += 25;
            DrawText(spriteBatch, $"Biome: {biome}", new Vector2(padding, y), Color.LightGray);
            y += 35;

            if (_locations.Count > 0)
            {
                DrawLocationList(spriteBatch, y);
            }
            else
            {
                DrawText(spriteBatch, "No discovered locations in this area.", new Vector2(padding, y), Color.Gray);
                y += 25;
                DrawText(spriteBatch, "Explore the cell from the Navigation screen to discover locations.", new Vector2(padding, y), Color.Gray);
            }
        }

        // Message display
        if (!string.IsNullOrEmpty(_message))
        {
            DrawText(spriteBatch, _message, new Vector2(padding, viewport.Height - 120), Color.Yellow);
        }

        // Stamina display
        var party = SaveGameService.Party;
        DrawText(spriteBatch, $"Stamina: {party.Stamina:P0}", new Vector2(viewport.Width - 150, titleHeight + 10), Color.LightGreen);

        // Controls panel
        var controlsRect = new Rectangle(0, viewport.Height - 80, viewport.Width, 80);
        DrawRect(spriteBatch, controlsRect, new Color(30, 30, 50));

        var controls = _isInLocation
            ? "[E] Explore  [C] Camp  [ESC] Leave  [N] Navigation  [H] Home"
            : "[Up/Down] Navigate  [Enter] Select  [1-9] Quick Select  [N] Navigation  [H] Home";
        DrawCenteredText(spriteBatch, controls, viewport.Height - 50, Color.LightGray);
    }

    private void DrawLocationList(SpriteBatch spriteBatch, int startY)
    {
        var padding = 20;
        var y = startY;

        DrawText(spriteBatch, "Locations:", new Vector2(padding, y), Color.White);
        y += 30;

        // Column headers
        DrawText(spriteBatch, "#", new Vector2(padding, y), Color.Gray);
        DrawText(spriteBatch, "Name", new Vector2(padding + 40, y), Color.Gray);
        DrawText(spriteBatch, "Type", new Vector2(padding + 300, y), Color.Gray);
        DrawText(spriteBatch, "Status", new Vector2(padding + 450, y), Color.Gray);
        y += 25;

        // Draw separator
        DrawRect(spriteBatch, new Rectangle(padding, y, 600, 1), Color.Gray);
        y += 5;

        // Draw visible locations
        var endIndex = Math.Min(_scrollOffset + VisibleItems, _locations.Count);
        for (var i = _scrollOffset; i < endIndex; i++)
        {
            var loc = _locations[i];
            var isSelected = i == _selectedIndex;

            // Selection highlight
            if (isSelected)
            {
                DrawRect(spriteBatch, new Rectangle(padding - 5, y - 2, 620, 24), new Color(50, 50, 80));
            }

            var color = isSelected ? Color.White : Color.LightGray;
            var indexDisplay = i < 9 ? $"[{i + 1}]" : "";

            DrawText(spriteBatch, indexDisplay, new Vector2(padding, y), Color.Yellow);
            DrawText(spriteBatch, TruncateText(loc.Name, 25), new Vector2(padding + 40, y), color);
            DrawText(spriteBatch, loc.Type, new Vector2(padding + 300, y), Color.Gray);

            var statusText = loc.HasServices
                ? "Services"
                : loc.ExploredPercent >= 1
                    ? "Explored"
                    : $"{loc.ExploredPercent:P0}";
            var statusColor = loc.HasServices ? Color.Cyan : (loc.ExploredPercent >= 1 ? Color.Green : Color.Yellow);
            DrawText(spriteBatch, statusText, new Vector2(padding + 450, y), statusColor);

            y += 25;
        }

        // Scroll indicators
        if (_scrollOffset > 0)
        {
            DrawText(spriteBatch, "^ More above ^", new Vector2(padding + 200, startY + 25), Color.Gray);
        }
        if (endIndex < _locations.Count)
        {
            DrawText(spriteBatch, "v More below v", new Vector2(padding + 200, y + 5), Color.Gray);
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 2) + "..";
    }
}
