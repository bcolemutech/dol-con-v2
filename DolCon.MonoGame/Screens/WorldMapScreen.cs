using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Enums;
using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Services;
using DolCon.Core.Utilities;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// World map screen showing explored areas with fog-of-war, pan/zoom, and location indicators.
/// Renders cells as actual Voronoi polygons using vertex data from the map.
/// </summary>
public class WorldMapScreen : ScreenBase
{
    private Vector2 _cameraOffset;
    private float _zoom = 1.0f;

    private const float MinZoom = 0.1f;
    private const float MaxZoom = 5.0f;
    private const float PanSpeed = 400f;
    private const float ZoomStep = 0.15f;
    private const float CullMargin = 30f;

    private const int TitleBarHeight = 50;
    private const int ControlsBarHeight = 70;

    private Dictionary<int, float> _cellVisibility = new();
    private Burg? _cityOfLight;
    private bool _needsCenter = true;
    private BasicEffect? _basicEffect;
    private DynamicVertexBuffer? _vertexBuffer;
    private VertexPositionColor[] _vertexArray = Array.Empty<VertexPositionColor>();
    private int _primitiveCount;

    public override void Initialize()
    {
        _needsCenter = true;

        PrecomputeVisibility();

        _cityOfLight = SaveGameService.CurrentMap.Collections.burgs
            .FirstOrDefault(b => b.isCityOfLight);
    }

    public override void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        base.LoadContent(content, graphicsDevice);

        _basicEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false,
            FogEnabled = false
        };

        if (_needsCenter)
        {
            CenterOnPlayer();
            _needsCenter = false;
        }
    }

    private void PrecomputeVisibility()
    {
        _cellVisibility.Clear();
        var cells = SaveGameService.CurrentMap.Collections.cells;
        var currentCellId = SaveGameService.Party.Cell;

        // Always show the player's current cell at full visibility
        _cellVisibility[currentCellId] = 1.0f;

        // First pass: set visibility for explored cells
        foreach (var cell in cells)
        {
            if (cell.ExploredPercent > 0)
            {
                _cellVisibility[cell.i] = (float)Math.Max(
                    cell.ExploredPercent,
                    _cellVisibility.GetValueOrDefault(cell.i, 0f));
            }
        }

        // Second pass: propagate to unexplored neighbors
        var cellsToPropagate = _cellVisibility.Keys.ToList();
        foreach (var cellId in cellsToPropagate)
        {
            if (cellId < 0 || cellId >= cells.Count) continue;
            var cell = cells[cellId];
            float sourceVisibility = _cellVisibility[cellId];

            foreach (var neighborId in cell.c)
            {
                if (neighborId < 0 || neighborId >= cells.Count) continue;

                // Adjacent to visible cell -> at least 0.30
                float neighborMin = sourceVisibility >= 1.0f ? 0.50f : 0.30f;

                float existing = _cellVisibility.GetValueOrDefault(neighborId, 0f);
                _cellVisibility[neighborId] = Math.Max(existing, neighborMin);
            }
        }
    }

    public override void Update(GameTime gameTime, InputManager input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Exit
        if (input.IsKeyPressed(Keys.Escape) || input.IsKeyPressed(Keys.M))
        {
            ScreenManager.SwitchTo(ScreenType.Home);
            return;
        }

        // Center on player
        if (input.IsKeyPressed(Keys.P) || input.IsKeyPressed(Keys.Home))
        {
            CenterOnPlayer();
            return;
        }

        // Center on City of Light
        if (input.IsKeyPressed(Keys.C))
        {
            CenterOnCityOfLight();
            return;
        }

        // Panning with arrow keys / WASD (held for smooth scrolling)
        float panAmount = PanSpeed * dt / _zoom;
        if (input.IsKeyDown(Keys.Left) || input.IsKeyDown(Keys.A))
            _cameraOffset.X -= panAmount;
        if (input.IsKeyDown(Keys.Right) || input.IsKeyDown(Keys.D))
            _cameraOffset.X += panAmount;
        if (input.IsKeyDown(Keys.Up) || input.IsKeyDown(Keys.W))
            _cameraOffset.Y -= panAmount;
        if (input.IsKeyDown(Keys.Down) || input.IsKeyDown(Keys.S))
            _cameraOffset.Y += panAmount;

        // Zooming with +/- keys
        if (input.IsKeyPressed(Keys.OemPlus) || input.IsKeyPressed(Keys.Add))
        {
            ZoomAtCenter(ZoomStep);
        }
        if (input.IsKeyPressed(Keys.OemMinus) || input.IsKeyPressed(Keys.Subtract))
        {
            ZoomAtCenter(-ZoomStep);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var viewport = GraphicsDevice.Viewport;
        var mapViewport = new Rectangle(0, TitleBarHeight, viewport.Width, viewport.Height - TitleBarHeight - ControlsBarHeight);

        // Black background for map area
        DrawRect(spriteBatch, mapViewport, Color.Black);

        // End SpriteBatch to draw polygons with vertex buffer
        spriteBatch.End();

        DrawCellPolygons(mapViewport);

        // Resume SpriteBatch for UI elements
        spriteBatch.Begin();

        // Render indicators (on top of cells)
        DrawCityOfLightIndicator(spriteBatch, mapViewport);
        DrawPlayerIndicator(spriteBatch, mapViewport);

        // Title bar
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, TitleBarHeight), new Color(30, 30, 50));
        DrawCenteredText(spriteBatch, "DOMINION OF LIGHT - World Map", 15, Color.Gold);

        // Zoom level display (top right)
        DrawText(spriteBatch, $"Zoom: {_zoom:F1}x",
            new Vector2(viewport.Width - 130, 15), Color.LightGray);

        // Controls bar
        var controlsRect = new Rectangle(0, viewport.Height - ControlsBarHeight, viewport.Width, ControlsBarHeight);
        DrawRect(spriteBatch, controlsRect, new Color(30, 30, 50));
        DrawCenteredText(spriteBatch,
            "[Arrows/WASD] Pan  [+/-] Zoom  [P] Player  [C] City of Light  [ESC/M] Back",
            viewport.Height - 45, Color.LightGray);
    }

    private void DrawCellPolygons(Rectangle mapViewport)
    {
        if (_basicEffect == null) return;

        var map = SaveGameService.CurrentMap;
        var cells = map.Collections.cells;
        var vertices = map.vertices;

        if (vertices == null || vertices.Count == 0) return;

        // Calculate visible world bounds with margin for culling
        float worldLeft = _cameraOffset.X - CullMargin;
        float worldRight = _cameraOffset.X + mapViewport.Width / _zoom + CullMargin;
        float worldTop = _cameraOffset.Y - CullMargin;
        float worldBottom = _cameraOffset.Y + mapViewport.Height / _zoom + CullMargin;

        // Build triangle list for all visible cells
        var triangleVerts = new List<VertexPositionColor>();

        foreach (var cell in cells)
        {
            if (!_cellVisibility.TryGetValue(cell.i, out float visibility))
                continue;

            // Frustum culling using cell center
            float cx = (float)cell.p[0];
            float cy = (float)cell.p[1];
            if (cx < worldLeft || cx > worldRight || cy < worldTop || cy > worldBottom)
                continue;

            if (cell.v == null || cell.v.Count < 3) continue;

            // Get biome color dimmed by visibility
            var baseColor = GetBiomeColor(cell.Biome);
            var dimmedColor = new Color(
                (int)(baseColor.R * visibility),
                (int)(baseColor.G * visibility),
                (int)(baseColor.B * visibility));

            // Fan triangulation: center -> v[i] -> v[i+1]
            var centerScreen = WorldToScreen(cx, cy, mapViewport);
            var centerVert = new VertexPositionColor(new Vector3(centerScreen, 0), dimmedColor);

            for (int i = 0; i < cell.v.Count; i++)
            {
                int vi = cell.v[i];
                int viNext = cell.v[(i + 1) % cell.v.Count];

                if (vi < 0 || vi >= vertices.Count || viNext < 0 || viNext >= vertices.Count)
                    continue;

                var v1 = vertices[vi];
                var v2 = vertices[viNext];

                var s1 = WorldToScreen((float)v1.p[0], (float)v1.p[1], mapViewport);
                var s2 = WorldToScreen((float)v2.p[0], (float)v2.p[1], mapViewport);

                triangleVerts.Add(centerVert);
                triangleVerts.Add(new VertexPositionColor(new Vector3(s1, 0), dimmedColor));
                triangleVerts.Add(new VertexPositionColor(new Vector3(s2, 0), dimmedColor));
            }
        }

        if (triangleVerts.Count < 3) return;

        _vertexArray = triangleVerts.ToArray();
        _primitiveCount = _vertexArray.Length / 3;

        // Create or resize the vertex buffer
        if (_vertexBuffer == null || _vertexBuffer.VertexCount < _vertexArray.Length)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = new DynamicVertexBuffer(
                GraphicsDevice,
                VertexPositionColor.VertexDeclaration,
                _vertexArray.Length,
                BufferUsage.WriteOnly);
        }

        _vertexBuffer.SetData(_vertexArray, 0, _vertexArray.Length, SetDataOptions.Discard);

        // Set up BasicEffect for 2D rendering
        var viewport = GraphicsDevice.Viewport;
        _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
            0, viewport.Width, viewport.Height, 0, 0, 1);
        _basicEffect.View = Matrix.Identity;
        _basicEffect.World = Matrix.Identity;

        // Reset all render state for proper cross-platform rendering
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.DepthStencilState = DepthStencilState.None;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.Textures[0] = null;
        GraphicsDevice.SetVertexBuffer(_vertexBuffer);

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _primitiveCount);
        }

        // Clear vertex buffer binding before SpriteBatch resumes
        GraphicsDevice.SetVertexBuffer(null);
    }

    private void DrawPlayerIndicator(SpriteBatch spriteBatch, Rectangle mapViewport)
    {
        var cell = SaveGameService.CurrentCell;
        var screenPos = WorldToScreen((float)cell.p[0], (float)cell.p[1], mapViewport);

        int cx = (int)screenPos.X;
        int cy = (int)screenPos.Y;

        // Fixed-size crosshair (does not scale with zoom)
        const int armLen = 8;
        DrawRect(spriteBatch, new Rectangle(cx - armLen, cy, armLen * 2 + 1, 1), Color.Gold);
        DrawRect(spriteBatch, new Rectangle(cx, cy - armLen, 1, armLen * 2 + 1), Color.Gold);
    }

    private void DrawCityOfLightIndicator(SpriteBatch spriteBatch, Rectangle mapViewport)
    {
        if (_cityOfLight?.x == null || _cityOfLight.y == null)
            return;

        var screenPos = WorldToScreen((float)_cityOfLight.x.Value, (float)_cityOfLight.y.Value, mapViewport);

        int cx = (int)screenPos.X;
        int cy = (int)screenPos.Y;

        // Fixed-size diamond marker (does not scale with zoom)
        const int r = 6;
        DrawRect(spriteBatch, new Rectangle(cx - r, cy, r * 2 + 1, 1), Color.White);
        DrawRect(spriteBatch, new Rectangle(cx, cy - r, 1, r * 2 + 1), Color.White);
        DrawRect(spriteBatch, new Rectangle(cx - 1, cy - 1, 3, 3), Color.White);

        // Label
        if (_cityOfLight.name != null)
        {
            var shadowColor = new Color(0, 0, 0, 180);
            var labelPos = new Vector2(cx + r + 4, cy - 8);
            DrawText(spriteBatch, _cityOfLight.name, labelPos + new Vector2(1, 1), shadowColor);
            DrawText(spriteBatch, _cityOfLight.name, labelPos, Color.White);
        }
    }

    private Vector2 WorldToScreen(float worldX, float worldY, Rectangle viewport)
    {
        float screenX = (worldX - _cameraOffset.X) * _zoom + viewport.X;
        float screenY = (worldY - _cameraOffset.Y) * _zoom + viewport.Y;
        return new Vector2(screenX, screenY);
    }

    private void CenterOnPlayer()
    {
        var cell = SaveGameService.CurrentCell;
        CenterOn((float)cell.p[0], (float)cell.p[1]);
    }

    private void CenterOnCityOfLight()
    {
        if (_cityOfLight?.x != null && _cityOfLight.y != null)
        {
            CenterOn((float)_cityOfLight.x.Value, (float)_cityOfLight.y.Value);
        }
    }

    private void CenterOn(float worldX, float worldY)
    {
        var viewport = GraphicsDevice.Viewport;
        float viewW = viewport.Width;
        float viewH = viewport.Height - TitleBarHeight - ControlsBarHeight;

        _cameraOffset.X = worldX - viewW / (2f * _zoom);
        _cameraOffset.Y = worldY - viewH / (2f * _zoom);
    }

    private void ZoomAtCenter(float delta)
    {
        var viewport = GraphicsDevice.Viewport;
        float viewW = viewport.Width;
        float viewH = viewport.Height - TitleBarHeight - ControlsBarHeight;

        // World point at center before zoom
        float centerWorldX = _cameraOffset.X + viewW / (2f * _zoom);
        float centerWorldY = _cameraOffset.Y + viewH / (2f * _zoom);

        _zoom = MathHelper.Clamp(_zoom + delta, MinZoom, MaxZoom);

        // Recalculate offset so center stays fixed
        _cameraOffset.X = centerWorldX - viewW / (2f * _zoom);
        _cameraOffset.Y = centerWorldY - viewH / (2f * _zoom);
    }

    private static Color GetBiomeColor(Biome biome)
    {
        var hex = BiomeColors.GetHexColor(biome);
        var (r, g, b) = BiomeColors.ParseHexColor(hex);
        return new Color(r, g, b);
    }

    public override void Unload()
    {
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;
        base.Unload();
    }
}
