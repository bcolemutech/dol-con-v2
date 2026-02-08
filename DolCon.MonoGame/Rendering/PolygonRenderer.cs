using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DolCon.MonoGame.Rendering;

/// <summary>
/// Reusable polygon renderer using vertex buffers and BasicEffect.
/// Supports filled polygons (fan triangulation) and polygon outlines.
/// </summary>
public class PolygonRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _basicEffect;
    private DynamicVertexBuffer? _vertexBuffer;
    private VertexPositionColor[] _vertexArray = Array.Empty<VertexPositionColor>();

    private readonly List<VertexPositionColor> _triangleVerts = new();

    public PolygonRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _basicEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false,
            FogEnabled = false
        };
    }

    /// <summary>
    /// Clears all queued polygons. Call before building a new frame.
    /// </summary>
    public void Clear()
    {
        _triangleVerts.Clear();
    }

    /// <summary>
    /// Adds a filled polygon using fan triangulation from center to consecutive vertex pairs.
    /// </summary>
    /// <param name="center">The center point (used as fan origin).</param>
    /// <param name="vertices">The polygon boundary vertices in order.</param>
    /// <param name="color">The fill color.</param>
    public void AddPolygon(Vector2 center, Vector2[] vertices, Color color)
    {
        if (vertices.Length < 3) return;

        var centerVert = new VertexPositionColor(new Vector3(center, 0), color);

        for (int i = 0; i < vertices.Length; i++)
        {
            int next = (i + 1) % vertices.Length;
            _triangleVerts.Add(centerVert);
            _triangleVerts.Add(new VertexPositionColor(new Vector3(vertices[i], 0), color));
            _triangleVerts.Add(new VertexPositionColor(new Vector3(vertices[next], 0), color));
        }
    }

    /// <summary>
    /// Adds a polygon outline rendered as thin quads along each edge.
    /// </summary>
    /// <param name="vertices">The polygon boundary vertices in order.</param>
    /// <param name="color">The outline color.</param>
    /// <param name="thickness">The line thickness in pixels.</param>
    public void AddOutline(Vector2[] vertices, Color color, float thickness = 2f)
    {
        if (vertices.Length < 2) return;

        float halfThick = thickness / 2f;

        for (int i = 0; i < vertices.Length; i++)
        {
            int next = (i + 1) % vertices.Length;
            var a = vertices[i];
            var b = vertices[next];

            // Perpendicular direction
            var edge = b - a;
            var normal = new Vector2(-edge.Y, edge.X);
            if (normal.LengthSquared() > 0)
                normal.Normalize();
            normal *= halfThick;

            // Quad as two triangles
            var v0 = new VertexPositionColor(new Vector3(a + normal, 0), color);
            var v1 = new VertexPositionColor(new Vector3(a - normal, 0), color);
            var v2 = new VertexPositionColor(new Vector3(b + normal, 0), color);
            var v3 = new VertexPositionColor(new Vector3(b - normal, 0), color);

            _triangleVerts.Add(v0);
            _triangleVerts.Add(v1);
            _triangleVerts.Add(v2);

            _triangleVerts.Add(v1);
            _triangleVerts.Add(v3);
            _triangleVerts.Add(v2);
        }
    }

    /// <summary>
    /// Renders all queued polygons to the screen.
    /// Call between SpriteBatch.End() and SpriteBatch.Begin().
    /// Vertices should already be in screen coordinates.
    /// </summary>
    public void Render()
    {
        if (_triangleVerts.Count < 3) return;

        int vertexCount = _triangleVerts.Count;

        // Resize array if needed
        if (_vertexArray.Length < vertexCount)
        {
            _vertexArray = new VertexPositionColor[vertexCount];
        }

        _triangleVerts.CopyTo(_vertexArray);
        int primitiveCount = vertexCount / 3;

        // Create or resize vertex buffer
        if (_vertexBuffer == null || _vertexBuffer.VertexCount < vertexCount)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = new DynamicVertexBuffer(
                _graphicsDevice,
                VertexPositionColor.VertexDeclaration,
                vertexCount,
                BufferUsage.WriteOnly);
        }

        _vertexBuffer.SetData(_vertexArray, 0, vertexCount, SetDataOptions.Discard);

        // Set up orthographic projection over the full device viewport
        // (vertices are already in screen coordinates)
        _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
            0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height, 0, 0, 1);
        _basicEffect.View = Matrix.Identity;
        _basicEffect.World = Matrix.Identity;

        // Set render state
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.Textures[0] = null;
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, primitiveCount);
        }

        // Clear vertex buffer binding
        _graphicsDevice.SetVertexBuffer(null);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;
        _basicEffect.Dispose();
    }
}
