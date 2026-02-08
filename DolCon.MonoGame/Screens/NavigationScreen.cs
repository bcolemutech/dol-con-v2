using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Services;
using DolCon.Core.Utilities;
using DolCon.MonoGame.Input;
using DolCon.MonoGame.Rendering;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Navigation screen showing the current cell and adjacent cells as Voronoi polygons.
/// Players press number keys (1-N) to move to adjacent cells, numbered clockwise from north.
/// </summary>
public class NavigationScreen : ScreenBase
{
    private readonly IMoveService _moveService;
    private readonly IEventService _eventService;
    private string _message = "";
    private Scene _currentScene = new();

    // Polygon data for current cell + neighbors
    private List<PolygonCellData> _cellPolygons = new();
    private CellClusterViewport? _viewport;
    private PolygonRenderer? _polygonRenderer;
    private int _maxSelectionNumber;

    // Special action flags for current cell
    private bool _canExplore;
    private bool _canCamp;
    private bool _canEnterBurg;
    private string? _burgName;

    private const int TitleBarHeight = 50;
    private const int ControlsBarHeight = 70;
    private const int ActionPanelWidth = 260;

    private record PolygonCellData(
        int CellId,
        Biome Biome,
        double ExploredPercent,
        string? BurgName,
        string Province,
        bool IsCurrent,
        int? SelectionNumber,
        bool IsBlocked,
        Vector2[] WorldVertices,
        Vector2 WorldCenter);

    public NavigationScreen(IMoveService moveService, IEventService eventService)
    {
        _moveService = moveService;
        _eventService = eventService;
    }

    public override void Initialize()
    {
        _message = "";
        _currentScene = new Scene();
    }

    public override void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        base.LoadContent(content, graphicsDevice);
        _polygonRenderer ??= new PolygonRenderer(graphicsDevice);
        BuildPolygonData();
    }

    private void BuildPolygonData()
    {
        _cellPolygons.Clear();
        _maxSelectionNumber = 0;

        var map = SaveGameService.CurrentMap;
        var cell = SaveGameService.CurrentCell;
        var location = SaveGameService.CurrentLocation;
        var burg = SaveGameService.CurrentBurg;

        // Check for special actions at current position
        _canExplore = location == null && burg == null;
        _canCamp = location == null && burg == null;
        var cellBurg = SaveGameService.GetBurg(cell.burg);
        _canEnterBurg = cellBurg != null && burg == null;
        _burgName = cellBurg?.name;

        // Collect all vertices for viewport calculation
        var allVertices = new List<(double X, double Y)>();

        // Add current cell
        var currentVerts = GetCellVertices(cell, map);
        var centerProvince = SaveGameService.GetProvince(cell.province);
        allVertices.AddRange(currentVerts.Select(v => ((double)v.X, (double)v.Y)));

        _cellPolygons.Add(new PolygonCellData(
            cell.i,
            cell.Biome,
            cell.ExploredPercent,
            cellBurg?.name,
            centerProvince.fullName,
            true,
            null,
            false,
            currentVerts,
            new Vector2((float)cell.p[0], (float)cell.p[1])));

        // Build neighbor inputs for clockwise sorting
        var neighborInputs = new List<ClockwiseNeighborSorter.NeighborInput>();
        var neighborData = new Dictionary<int, (Cell Cell, Vector2[] Verts, Burg? Burg, Province Province)>();

        foreach (var neighborId in cell.c)
        {
            if (neighborId < 0) continue;

            var neighbor = SaveGameService.GetCell(neighborId);
            var nVerts = GetCellVertices(neighbor, map);
            var nBurg = SaveGameService.GetBurg(neighbor.burg);
            var nProvince = SaveGameService.GetProvince(neighbor.province);
            var isBlocked = neighbor.Biome == Biome.Marine;

            neighborData[neighborId] = (neighbor, nVerts, nBurg, nProvince);
            neighborInputs.Add(new ClockwiseNeighborSorter.NeighborInput(
                neighborId, neighbor.p[0], neighbor.p[1], isBlocked));

            allVertices.AddRange(nVerts.Select(v => ((double)v.X, (double)v.Y)));
        }

        // Sort neighbors clockwise from north
        var sorted = ClockwiseNeighborSorter.SortNeighborsClockwise(
            cell.p[0], cell.p[1], neighborInputs);

        foreach (var entry in sorted)
        {
            var (neighbor, verts, nBurg, province) = neighborData[entry.CellId];

            _cellPolygons.Add(new PolygonCellData(
                neighbor.i,
                neighbor.Biome,
                neighbor.ExploredPercent,
                nBurg?.name,
                province.fullName,
                false,
                entry.SelectionNumber,
                neighbor.Biome == Biome.Marine,
                verts,
                new Vector2((float)neighbor.p[0], (float)neighbor.p[1])));

            if (entry.SelectionNumber.HasValue)
                _maxSelectionNumber = entry.SelectionNumber.Value;
        }

        // Compute viewport transform
        var screenViewport = GraphicsDevice.Viewport;
        int polyAreaWidth = screenViewport.Width - ActionPanelWidth - 20;
        int polyAreaHeight = screenViewport.Height - TitleBarHeight - ControlsBarHeight - 20;
        _viewport = new CellClusterViewport(allVertices, polyAreaWidth, polyAreaHeight);
    }

    private Vector2[] GetCellVertices(Cell cell, Map map)
    {
        if (cell.v == null || cell.v.Count < 3)
            return Array.Empty<Vector2>();

        var vertices = new Vector2[cell.v.Count];
        for (int i = 0; i < cell.v.Count; i++)
        {
            int vi = cell.v[i];
            if (vi >= 0 && vi < map.vertices.Count)
            {
                var v = map.vertices[vi];
                vertices[i] = new Vector2((float)v.p[0], (float)v.p[1]);
            }
        }
        return vertices;
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
                BuildPolygonData();
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

        // Number key selection (1-N, mapped to clockwise-sorted neighbors)
        var numKey = input.GetPressedNumericKey();
        if (numKey.HasValue && numKey.Value >= 1 && numKey.Value <= _maxSelectionNumber)
        {
            var target = _cellPolygons.FirstOrDefault(c => c.SelectionNumber == numKey.Value);
            if (target != null)
            {
                if (target.IsBlocked)
                {
                    _message = "Cannot travel to water!";
                }
                else
                {
                    ProcessMovement(target.CellId);
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
        BuildPolygonData();
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
            BuildPolygonData();
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
            BuildPolygonData();
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

        // Title bar
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, TitleBarHeight), new Color(30, 30, 50));
        DrawCenteredText(spriteBatch, "DOMINION OF LIGHT - Navigation", 15, Color.Gold);

        // Stamina display (top right)
        var party = SaveGameService.Party;
        DrawText(spriteBatch, $"Stamina: {party.Stamina:P0}",
            new Vector2(viewport.Width - 150, 15), Color.LightGreen);

        // Message display
        if (!string.IsNullOrEmpty(_message))
        {
            DrawText(spriteBatch, _message, new Vector2(20, 60), Color.Yellow);
        }

        // Current burg status
        var burg = SaveGameService.CurrentBurg;
        if (burg != null)
        {
            DrawText(spriteBatch, $"In Burg: {burg.name} ({burg.size})", new Vector2(20, 85), Color.Cyan);
        }

        // Polygon area background
        int polyAreaX = 0;
        int polyAreaY = TitleBarHeight;
        int polyAreaWidth = viewport.Width - ActionPanelWidth - 20;
        int polyAreaHeight = viewport.Height - TitleBarHeight - ControlsBarHeight;
        DrawRect(spriteBatch, new Rectangle(polyAreaX, polyAreaY, polyAreaWidth, polyAreaHeight), new Color(10, 10, 20));

        // Draw polygons
        spriteBatch.End();
        DrawCellPolygons(polyAreaX, polyAreaY, polyAreaWidth, polyAreaHeight);
        spriteBatch.Begin();

        // Draw text overlays on polygons
        DrawCellTextOverlays(spriteBatch, polyAreaX, polyAreaY);

        // Draw action panel (right side)
        DrawActionPanel(spriteBatch);

        // Controls panel (bottom)
        var controlsRect = new Rectangle(0, viewport.Height - ControlsBarHeight, viewport.Width, ControlsBarHeight);
        DrawRect(spriteBatch, controlsRect, new Color(30, 30, 50));

        var controls = $"[1-{_maxSelectionNumber}] Move to cell  [L] Locations  [M] Map";
        if (_canExplore) controls += "  [E] Explore";
        if (_canCamp) controls += "  [C] Camp";
        if (_canEnterBurg) controls += "  [B] Enter Burg";
        controls += "  [H] Home  [I] Inventory  [ESC] Back";
        DrawCenteredText(spriteBatch, controls, viewport.Height - 45, Color.LightGray);
    }

    private void DrawCellPolygons(int areaX, int areaY, int areaWidth, int areaHeight)
    {
        if (_polygonRenderer == null || _viewport == null) return;

        _polygonRenderer.Clear();

        foreach (var cellData in _cellPolygons)
        {
            if (cellData.WorldVertices.Length < 3) continue;

            var biomeColor = GetBiomeColor(cellData.Biome);

            // Dim marine/blocked cells
            if (cellData.IsBlocked)
            {
                biomeColor = new Color(biomeColor.R / 2, biomeColor.G / 2, biomeColor.B / 2, 150);
            }

            // Convert to screen space
            var screenVerts = new Vector2[cellData.WorldVertices.Length];
            for (int i = 0; i < cellData.WorldVertices.Length; i++)
            {
                var (sx, sy) = _viewport.WorldToScreen(
                    cellData.WorldVertices[i].X, cellData.WorldVertices[i].Y);
                screenVerts[i] = new Vector2(sx + areaX, sy + areaY);
            }

            var (cx, cy) = _viewport.WorldToScreen(
                cellData.WorldCenter.X, cellData.WorldCenter.Y);
            var screenCenter = new Vector2(cx + areaX, cy + areaY);

            _polygonRenderer.AddPolygon(screenCenter, screenVerts, biomeColor);

            // Add outline
            if (cellData.IsCurrent)
            {
                _polygonRenderer.AddOutline(screenVerts, Color.Gold, 3f);
            }
            else if (!cellData.IsBlocked)
            {
                _polygonRenderer.AddOutline(screenVerts, new Color(60, 60, 80), 1.5f);
            }
        }

        var renderViewport = new Rectangle(areaX, areaY, areaWidth, areaHeight);
        _polygonRenderer.Render(renderViewport);
    }

    private void DrawCellTextOverlays(SpriteBatch spriteBatch, int areaX, int areaY)
    {
        if (_viewport == null || Font == null) return;

        var shadowColor = new Color(0, 0, 0, 180);
        const float lineHeight = 20f;

        foreach (var cellData in _cellPolygons)
        {
            if (cellData.WorldVertices.Length < 3) continue;

            var (cx, cy) = _viewport.WorldToScreen(
                cellData.WorldCenter.X, cellData.WorldCenter.Y);
            float screenX = cx + areaX;
            float screenY = cy + areaY;

            // Estimate polygon radius to limit text to what fits
            float polyRadius = EstimatePolygonRadius(cellData);

            var biomeColor = GetBiomeColor(cellData.Biome);
            var textColor = GetContrastingTextColor(biomeColor);

            // Build lines to render
            var lines = new List<(string Text, Color Color)>();

            if (cellData.IsCurrent)
            {
                lines.Add(("YOU", Color.Gold));
                lines.Add((FormatBiomeName(cellData.Biome), textColor));
                var exploredText = cellData.ExploredPercent >= 1 ? "Explored" : $"{cellData.ExploredPercent:P0}";
                lines.Add((exploredText, textColor));
                if (!string.IsNullOrEmpty(cellData.BurgName))
                    lines.Add(($"* {cellData.BurgName}", Color.White));
            }
            else if (cellData.IsBlocked)
            {
                lines.Add(("Ocean", new Color(150, 150, 180)));
            }
            else
            {
                if (cellData.SelectionNumber.HasValue)
                    lines.Add(($"[{cellData.SelectionNumber}]", Color.Yellow));
                lines.Add((FormatBiomeName(cellData.Biome), textColor));
                var exploredText = cellData.ExploredPercent >= 1 ? "Explored" : $"{cellData.ExploredPercent:P0}";
                lines.Add((exploredText, textColor));
                if (!string.IsNullOrEmpty(cellData.BurgName))
                    lines.Add(($"* {cellData.BurgName}", Color.White));
            }

            // Only show lines that fit within polygon radius
            int maxLines = Math.Max(1, (int)(polyRadius * 2 / lineHeight));
            if (lines.Count > maxLines)
                lines = lines.Take(maxLines).ToList();

            // Center the block of text vertically on the cell center
            float totalHeight = lines.Count * lineHeight;
            float startY = screenY - totalHeight / 2;

            for (int i = 0; i < lines.Count; i++)
            {
                var (text, color) = lines[i];
                var textSize = Font.MeasureString(text);
                float textX = screenX - textSize.X / 2;
                float textY = startY + i * lineHeight;

                DrawTextWithShadow(spriteBatch, text, new Vector2(textX, textY), color, shadowColor);
            }
        }
    }

    private float EstimatePolygonRadius(PolygonCellData cellData)
    {
        if (_viewport == null) return 50f;

        var (cx, cy) = _viewport.WorldToScreen(
            cellData.WorldCenter.X, cellData.WorldCenter.Y);

        float minDist = float.MaxValue;
        foreach (var vert in cellData.WorldVertices)
        {
            var (vx, vy) = _viewport.WorldToScreen(vert.X, vert.Y);
            float dx = vx - cx;
            float dy = vy - cy;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < minDist) minDist = dist;
        }

        return minDist;
    }

    private void DrawActionPanel(SpriteBatch spriteBatch)
    {
        var viewport = GraphicsDevice.Viewport;
        int panelX = viewport.Width - ActionPanelWidth - 10;
        int panelY = TitleBarHeight + 10;
        int panelHeight = viewport.Height - TitleBarHeight - ControlsBarHeight - 20;

        // Panel background
        DrawRect(spriteBatch, new Rectangle(panelX, panelY, ActionPanelWidth, panelHeight), new Color(25, 25, 40));
        DrawBorder(spriteBatch, new Rectangle(panelX, panelY, ActionPanelWidth, panelHeight), new Color(60, 60, 80), 2);

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
        DrawRect(spriteBatch, new Rectangle(panelX + 10, y, ActionPanelWidth - 20, 1), Color.Gray);
        y += 15;

        // Available actions
        var burg = SaveGameService.CurrentBurg;

        if (burg != null)
        {
            DrawText(spriteBatch, $"In Burg: {burg.name}", new Vector2(panelX + 15, y), Color.Cyan);
            y += 22;
            DrawText(spriteBatch, "[L] Explore Locations", new Vector2(panelX + 15, y), Color.Orange);
            y += 22;
            DrawText(spriteBatch, "[ESC] Leave Burg", new Vector2(panelX + 15, y), Color.Gray);
            y += 25;
        }
        else
        {
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
        DrawRect(spriteBatch, new Rectangle(panelX + 10, y, ActionPanelWidth - 20, 1), Color.Gray);
        y += 15;

        // Navigation hints
        DrawText(spriteBatch, "Navigation:", new Vector2(panelX + 15, y), Color.White);
        y += 22;
        DrawText(spriteBatch, $"Press 1-{_maxSelectionNumber} to move", new Vector2(panelX + 15, y), Color.Gray);
        y += 20;
        DrawText(spriteBatch, "(clockwise from north)", new Vector2(panelX + 15, y), Color.DarkGray);
    }

    private void DrawTextWithShadow(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor)
    {
        DrawText(spriteBatch, text, position + new Vector2(1, 1), shadowColor);
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
        var brightness = (backgroundColor.R * 299 + backgroundColor.G * 587 + backgroundColor.B * 114) / 1000;
        return brightness > 128 ? Color.Black : Color.White;
    }

    private static string FormatBiomeName(Biome biome)
    {
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

    public override void Unload()
    {
        _polygonRenderer?.Dispose();
        _polygonRenderer = null;
        base.Unload();
    }
}
