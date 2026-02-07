using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Services;
using DolCon.Core.Utilities;
using DolCon.MonoGame.Input;
using Direction = DolCon.Core.Enums.Direction;
using GridPosition = DolCon.Core.Utilities.DirectionGridMapper.GridPosition;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Navigation screen showing a 3x3 spatial grid of cells for directional movement.
/// Uses numpad-style key mapping (7=NW, 8=N, 9=NE, 4=W, 6=E, 1=SW, 2=S, 3=SE).
/// </summary>
public class NavigationScreen : ScreenBase
{
    private readonly IMoveService _moveService;
    private readonly IEventService _eventService;
    private string _message = "";
    private Scene _currentScene = new();

    // Grid data structure - 9 cells for 3x3 grid
    private GridCellData?[] _gridCells = new GridCellData?[9];
    private int? _selectedGridIndex;

    // Special action flags for current cell
    private bool _canExplore;
    private bool _canCamp;
    private bool _canEnterBurg;
    private string? _burgName;

    private record GridCellData(
        int CellId,
        Biome Biome,
        double ExploredPercent,
        string? BurgName,
        string Province,
        bool IsCurrent,
        int NumpadKey,
        bool IsBlocked);

    public NavigationScreen(IMoveService moveService, IEventService eventService)
    {
        _moveService = moveService;
        _eventService = eventService;
    }

    public override void Initialize()
    {
        _message = "";
        _currentScene = new Scene();
        _selectedGridIndex = null;
        BuildGrid();
    }

    private void BuildGrid()
    {
        // Clear grid
        Array.Clear(_gridCells);
        _selectedGridIndex = null;

        var cell = SaveGameService.CurrentCell;
        var location = SaveGameService.CurrentLocation;
        var burg = SaveGameService.CurrentBurg;

        // Check for special actions at current position
        // Can explore when in wilderness (not in burg or location) - even fully explored cells can trigger encounters
        _canExplore = location == null && burg == null;
        _canCamp = location == null && burg == null;
        var cellBurg = SaveGameService.GetBurg(cell.burg);
        _canEnterBurg = cellBurg != null && burg == null;
        _burgName = cellBurg?.name;

        // Center cell (grid position 5, array index 4)
        var centerProvince = SaveGameService.GetProvince(cell.province);
        _gridCells[4] = new GridCellData(
            cell.i,
            cell.Biome,
            cell.ExploredPercent,
            cellBurg?.name,
            centerProvince.fullName,
            true,
            5,
            false);

        // Map neighbors to grid positions
        foreach (var neighborId in cell.c)
        {
            if (neighborId < 0) continue;

            var neighbor = SaveGameService.GetCell(neighborId);
            var direction = MapService.GetDirection(
                cell.p[0], cell.p[1],
                neighbor.p[0], neighbor.p[1]);

            var gridPos = DirectionGridMapper.MapDirectionToGrid(direction);
            if (gridPos == null || gridPos == GridPosition.Center) continue;

            var arrayIndex = DirectionGridMapper.GetArrayIndex(gridPos.Value);
            var neighborBurg = SaveGameService.GetBurg(neighbor.burg);
            var province = SaveGameService.GetProvince(neighbor.province);
            var isBlocked = neighbor.Biome == Biome.Marine;

            _gridCells[arrayIndex] = new GridCellData(
                neighbor.i,
                neighbor.Biome,
                neighbor.ExploredPercent,
                neighborBurg?.name,
                province.fullName,
                false,
                DirectionGridMapper.GetNumpadKey(gridPos.Value),
                isBlocked);
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
        if (input.IsKeyPressed(Keys.I))
        {
            ScreenManager.SwitchTo(ScreenType.Inventory);
            return;
        }
        if (input.IsKeyPressed(Keys.L))
        {
            ScreenManager.SwitchTo(ScreenType.Location);
            return;
        }
        if (input.IsKeyPressed(Keys.M))
        {
            ScreenManager.SwitchTo(ScreenType.WorldMap);
            return;
        }

        if (input.IsKeyPressed(Keys.Escape))
        {
            // Leave current burg
            var party = SaveGameService.Party;
            if (party.Burg.HasValue)
            {
                party.Burg = null;
                _message = "Left burg";
                BuildGrid();
            }
            else
            {
                ScreenManager.SwitchTo(ScreenType.Home);
            }
            return;
        }

        // Quick actions
        if (input.IsKeyPressed(Keys.E) && _canExplore)
        {
            ProcessExploration();
            return;
        }
        if (input.IsKeyPressed(Keys.C) && _canCamp)
        {
            ProcessCamp();
            return;
        }
        if (input.IsKeyPressed(Keys.B) && _canEnterBurg)
        {
            ProcessEnterBurg();
            return;
        }

        // Number key selection (1-9 using numpad layout)
        var numKey = input.GetPressedNumericKey();
        if (numKey.HasValue && numKey.Value >= 1 && numKey.Value <= 9)
        {
            var gridPos = DirectionGridMapper.GetGridPosition(numKey.Value);
            if (gridPos.HasValue)
            {
                var arrayIndex = DirectionGridMapper.GetArrayIndex(gridPos.Value);
                var cellData = _gridCells[arrayIndex];

                if (cellData != null && !cellData.IsCurrent)
                {
                    if (cellData.IsBlocked)
                    {
                        _message = "Cannot travel to water!";
                    }
                    else
                    {
                        _selectedGridIndex = arrayIndex;
                        ProcessMovement(cellData.CellId);
                    }
                }
            }
        }
    }

    private void ProcessMovement(int targetCellId)
    {
        var result = _moveService.MoveToCell(targetCellId);
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
        BuildGrid();
    }

    private void ProcessExploration()
    {
        var location = SaveGameService.CurrentLocation;
        var cell = SaveGameService.CurrentCell;
        var thisEvent = new Event(location, cell);
        _currentScene = _eventService.ProcessEvent(thisEvent);

        if (_currentScene.Type == SceneType.Battle)
        {
            ScreenManager.SwitchToBattle(_currentScene);
        }
        else if (_currentScene.Type == SceneType.Shop)
        {
            ScreenManager.SwitchToShop(_currentScene);
        }
        else
        {
            _message = _currentScene.Message ?? "Exploration complete";
            BuildGrid();
        }
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

    private void ProcessEnterBurg()
    {
        var cell = SaveGameService.CurrentCell;
        var cellBurg = SaveGameService.GetBurg(cell.burg);
        if (cellBurg != null)
        {
            var party = SaveGameService.Party;
            party.Burg = cellBurg.i;
            _message = $"Entered {cellBurg.name}";
            SaveHelper.TriggerSave();
            BuildGrid();
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
                ScreenManager.SwitchToBattle(_currentScene);
            }
            else if (_currentScene.Type == SceneType.Shop)
            {
                ScreenManager.SwitchToShop(_currentScene);
            }
        }
        catch (Exception ex)
        {
            _message = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"NavigationScreen.ProcessEvent error: {ex}");
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = GraphicsDevice.Viewport;
        var padding = 20;

        // Title bar
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, 50), new Color(30, 30, 50));
        DrawCenteredText(spriteBatch, "DOMINION OF LIGHT - Navigation", 15, Color.Gold);

        // Stamina display (top right)
        var party = SaveGameService.Party;
        DrawText(spriteBatch, $"Stamina: {party.Stamina:P0}",
            new Vector2(viewport.Width - 150, 15), Color.LightGreen);

        // Message display
        if (!string.IsNullOrEmpty(_message))
        {
            DrawText(spriteBatch, _message, new Vector2(padding, 60), Color.Yellow);
        }

        // Current status info
        var cell = SaveGameService.CurrentCell;
        var burg = SaveGameService.CurrentBurg;
        var y = 95;

        if (burg != null)
        {
            DrawText(spriteBatch, $"In Burg: {burg.name} ({burg.size})", new Vector2(padding, y), Color.Cyan);
            y += 25;
        }

        // Draw the 3x3 grid
        DrawNavigationGrid(spriteBatch);

        // Draw action panel (right side)
        DrawActionPanel(spriteBatch);

        // Controls panel (bottom)
        var controlsRect = new Rectangle(0, viewport.Height - 70, viewport.Width, 70);
        DrawRect(spriteBatch, controlsRect, new Color(30, 30, 50));

        var controls = "[1-9] Move to cell  [L] Locations  [M] Map";
        if (_canExplore) controls += "  [E] Explore";
        if (_canCamp) controls += "  [C] Camp";
        if (_canEnterBurg) controls += "  [B] Enter Burg";
        controls += "  [H] Home  [I] Inventory  [ESC] Back";
        DrawCenteredText(spriteBatch, controls, viewport.Height - 45, Color.LightGray);
    }

    private void DrawNavigationGrid(SpriteBatch spriteBatch)
    {
        var viewport = GraphicsDevice.Viewport;

        // Grid dimensions
        int cellWidth = 200;
        int cellHeight = 140;
        int gridWidth = cellWidth * 3 + 20; // 3 cells + gaps
        int gridHeight = cellHeight * 3 + 20;
        int gridStartX = (viewport.Width - gridWidth) / 2 - 100; // Shifted left to make room for action panel
        int gridStartY = 130;

        // Draw each grid cell
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int arrayIndex = row * 3 + col;
                var cellData = _gridCells[arrayIndex];

                int x = gridStartX + col * (cellWidth + 10);
                int y = gridStartY + row * (cellHeight + 10);
                var rect = new Rectangle(x, y, cellWidth, cellHeight);

                DrawGridCell(spriteBatch, rect, cellData, arrayIndex);
            }
        }

        // Draw numpad key legend
        var legendY = gridStartY + gridHeight + 10;
        DrawText(spriteBatch, "Numpad Keys:", new Vector2(gridStartX, legendY), Color.Gray);
        legendY += 20;
        DrawText(spriteBatch, "7=NW  8=N   9=NE", new Vector2(gridStartX, legendY), Color.DarkGray);
        legendY += 18;
        DrawText(spriteBatch, "4=W   5=C   6=E", new Vector2(gridStartX, legendY), Color.DarkGray);
        legendY += 18;
        DrawText(spriteBatch, "1=SW  2=S   3=SE", new Vector2(gridStartX, legendY), Color.DarkGray);
    }

    private void DrawGridCell(SpriteBatch spriteBatch, Rectangle rect, GridCellData? cellData, int arrayIndex)
    {
        if (cellData == null)
        {
            // Empty cell - draw dark background
            DrawRect(spriteBatch, rect, new Color(20, 20, 30));
            DrawBorder(spriteBatch, rect, new Color(40, 40, 50), 2);
            return;
        }

        // Get biome color
        var biomeColor = GetBiomeColor(cellData.Biome);

        // Adjust color for blocked (water) cells
        if (cellData.IsBlocked)
        {
            biomeColor = new Color(biomeColor.R / 2, biomeColor.G / 2, biomeColor.B / 2);
        }

        // Draw cell background
        DrawRect(spriteBatch, rect, biomeColor);

        // Draw border (highlight for current cell)
        var borderColor = cellData.IsCurrent ? Color.Gold : new Color(60, 60, 80);
        var borderThickness = cellData.IsCurrent ? 3 : 2;
        DrawBorder(spriteBatch, rect, borderColor, borderThickness);

        // Text color based on background brightness
        var textColor = GetContrastingTextColor(biomeColor);
        var shadowColor = new Color(0, 0, 0, 150);

        // Draw numpad key indicator (top-left corner)
        var keyText = cellData.NumpadKey.ToString();
        DrawTextWithShadow(spriteBatch, $"[{keyText}]", new Vector2(rect.X + 5, rect.Y + 3), Color.Yellow, shadowColor);

        // Draw biome name
        var biomeName = FormatBiomeName(cellData.Biome);
        DrawTextWithShadow(spriteBatch, biomeName, new Vector2(rect.X + 8, rect.Y + 25), textColor, shadowColor);

        // Draw exploration percentage
        var exploredText = cellData.ExploredPercent >= 1 ? "Explored" : $"{cellData.ExploredPercent:P0}";
        DrawTextWithShadow(spriteBatch, exploredText, new Vector2(rect.X + 8, rect.Y + 48), textColor, shadowColor);

        // Draw province (truncated)
        var provinceText = TruncateText(cellData.Province, 20);
        DrawTextWithShadow(spriteBatch, provinceText, new Vector2(rect.X + 8, rect.Y + 71), new Color(textColor, 180), shadowColor);

        // Draw burg indicator if present
        if (!string.IsNullOrEmpty(cellData.BurgName))
        {
            var burgText = $"* {TruncateText(cellData.BurgName, 18)}";
            DrawTextWithShadow(spriteBatch, burgText, new Vector2(rect.X + 8, rect.Y + 94), Color.White, shadowColor);
        }

        // Draw "CURRENT" label for center cell
        if (cellData.IsCurrent)
        {
            DrawTextWithShadow(spriteBatch, "CURRENT", new Vector2(rect.X + rect.Width - 70, rect.Y + rect.Height - 20), Color.Gold, shadowColor);
        }

        // Draw blocked indicator for water
        if (cellData.IsBlocked)
        {
            DrawTextWithShadow(spriteBatch, "BLOCKED", new Vector2(rect.X + rect.Width / 2 - 30, rect.Y + rect.Height - 20), Color.Red, shadowColor);
        }
    }

    private void DrawActionPanel(SpriteBatch spriteBatch)
    {
        var viewport = GraphicsDevice.Viewport;
        int panelX = viewport.Width - 280;
        int panelY = 130;
        int panelWidth = 260;
        int panelHeight = 350;

        // Panel background
        DrawRect(spriteBatch, new Rectangle(panelX, panelY, panelWidth, panelHeight), new Color(25, 25, 40));
        DrawBorder(spriteBatch, new Rectangle(panelX, panelY, panelWidth, panelHeight), new Color(60, 60, 80), 2);

        var y = panelY + 15;
        DrawText(spriteBatch, "Actions", new Vector2(panelX + 15, y), Color.White);
        y += 30;

        // Current cell info
        var cell = SaveGameService.CurrentCell;
        DrawText(spriteBatch, $"Cell: {cell.i}", new Vector2(panelX + 15, y), Color.LightGray);
        y += 22;
        DrawText(spriteBatch, $"Biome: {FormatBiomeName(cell.Biome)}", new Vector2(panelX + 15, y), Color.LightGray);
        y += 22;
        DrawText(spriteBatch, $"Explored: {cell.ExploredPercent:P0}", new Vector2(panelX + 15, y), Color.LightGray);
        y += 30;

        // Draw separator
        DrawRect(spriteBatch, new Rectangle(panelX + 10, y, panelWidth - 20, 1), Color.Gray);
        y += 15;

        // Available actions
        var burg = SaveGameService.CurrentBurg;

        if (burg != null)
        {
            // In a burg - show that exploration is via locations
            DrawText(spriteBatch, $"In Burg: {burg.name}", new Vector2(panelX + 15, y), Color.Cyan);
            y += 22;
            DrawText(spriteBatch, "[L] Explore Locations", new Vector2(panelX + 15, y), Color.Orange);
            y += 22;
            DrawText(spriteBatch, "[ESC] Leave Burg", new Vector2(panelX + 15, y), Color.Gray);
            y += 25;
        }
        else
        {
            // In wilderness
            if (_canExplore)
            {
                var exploredText = cell.ExploredPercent < 1 ? $" ({cell.ExploredPercent:P0})" : " (hunt)";
                DrawText(spriteBatch, $"[E] Explore Area{exploredText}", new Vector2(panelX + 15, y), Color.Cyan);
                y += 25;
            }

            if (_canCamp)
            {
                DrawText(spriteBatch, "[C] Camp (50% stamina)", new Vector2(panelX + 15, y), Color.LightGreen);
                y += 25;
            }

            if (_canEnterBurg && _burgName != null)
            {
                DrawText(spriteBatch, $"[B] Enter {TruncateText(_burgName, 15)}", new Vector2(panelX + 15, y), Color.LightBlue);
                y += 25;
            }

            DrawText(spriteBatch, "[L] View Locations", new Vector2(panelX + 15, y), Color.Orange);
        }
        y += 30;

        // Draw separator
        DrawRect(spriteBatch, new Rectangle(panelX + 10, y, panelWidth - 20, 1), Color.Gray);
        y += 15;

        // Navigation hints
        DrawText(spriteBatch, "Navigation:", new Vector2(panelX + 15, y), Color.White);
        y += 22;
        DrawText(spriteBatch, "Press 1-9 to move", new Vector2(panelX + 15, y), Color.Gray);
        y += 20;
        DrawText(spriteBatch, "(numpad layout)", new Vector2(panelX + 15, y), Color.DarkGray);
    }

    private void DrawTextWithShadow(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor)
    {
        // Draw shadow
        DrawText(spriteBatch, text, position + new Vector2(1, 1), shadowColor);
        // Draw text
        DrawText(spriteBatch, text, position, color);
    }

    private Color GetBiomeColor(Biome biome)
    {
        var hex = BiomeColors.GetHexColor(biome);
        var (r, g, b) = BiomeColors.ParseHexColor(hex);
        return new Color(r, g, b);
    }

    private Color GetContrastingTextColor(Color backgroundColor)
    {
        // Calculate perceived brightness
        var brightness = (backgroundColor.R * 299 + backgroundColor.G * 587 + backgroundColor.B * 114) / 1000;
        return brightness > 128 ? Color.Black : Color.White;
    }

    private static string FormatBiomeName(Biome biome)
    {
        // Insert spaces before capital letters for readability
        var name = biome.ToString();
        var result = "";
        foreach (var c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result += " ";
            }
            result += c;
        }
        return result;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? "";
        return text.Substring(0, maxLength - 2) + "..";
    }
}
