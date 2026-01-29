using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Interface for game screens (menus, gameplay, etc.)
/// </summary>
public interface IScreen
{
    /// <summary>
    /// Called when the screen becomes active.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Load content for this screen.
    /// </summary>
    void LoadContent(ContentManager content, GraphicsDevice graphicsDevice);

    /// <summary>
    /// Update the screen logic.
    /// </summary>
    void Update(GameTime gameTime, InputManager input);

    /// <summary>
    /// Draw the screen.
    /// </summary>
    void Draw(SpriteBatch spriteBatch);

    /// <summary>
    /// Called when the screen is being removed.
    /// </summary>
    void Unload();
}

/// <summary>
/// Base implementation of IScreen with common functionality.
/// </summary>
public abstract class ScreenBase : IScreen
{
    private static FontSystem? _fontSystem;
    private static SpriteFontBase? _font;

    protected ScreenManager ScreenManager { get; private set; } = null!;
    protected SpriteFontBase? Font => _font;
    protected Texture2D? PixelTexture { get; private set; }
    protected GraphicsDevice GraphicsDevice { get; private set; } = null!;

    public void SetScreenManager(ScreenManager manager) => ScreenManager = manager;

    public virtual void Initialize() { }

    public virtual void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;

        // Create a 1x1 white pixel texture for drawing shapes
        PixelTexture = new Texture2D(graphicsDevice, 1, 1);
        PixelTexture.SetData(new[] { Color.White });

        // Initialize font system once (shared across all screens)
        if (_fontSystem == null)
        {
            _fontSystem = new FontSystem();

            // Try to load a system font
            var fontPaths = new[]
            {
                // Windows
                @"C:\Windows\Fonts\consola.ttf",
                @"C:\Windows\Fonts\arial.ttf",
                // Linux
                "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf",
                // macOS
                "/System/Library/Fonts/Menlo.ttc",
                "/System/Library/Fonts/Monaco.ttf"
            };

            foreach (var path in fontPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        _fontSystem.AddFont(File.ReadAllBytes(path));
                        break;
                    }
                    catch
                    {
                        // Try next font
                    }
                }
            }

            _font = _fontSystem.GetFont(18);
        }
    }

    public abstract void Update(GameTime gameTime, InputManager input);

    public abstract void Draw(SpriteBatch spriteBatch);

    public virtual void Unload()
    {
        PixelTexture?.Dispose();
    }

    /// <summary>
    /// Draw a filled rectangle.
    /// </summary>
    protected void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        if (PixelTexture != null)
        {
            spriteBatch.Draw(PixelTexture, rect, color);
        }
    }

    /// <summary>
    /// Draw a rectangle border.
    /// </summary>
    protected void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
    {
        if (PixelTexture == null) return;

        // Top
        spriteBatch.Draw(PixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(PixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(PixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(PixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    /// <summary>
    /// Draw text at the specified position.
    /// </summary>
    protected void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
    {
        if (_font != null)
        {
            spriteBatch.DrawString(_font, text, position, color);
        }
    }

    /// <summary>
    /// Draw centered text at the specified Y position.
    /// </summary>
    protected void DrawCenteredText(SpriteBatch spriteBatch, string text, int y, Color color)
    {
        if (_font != null)
        {
            var size = _font.MeasureString(text);
            var x = (GraphicsDevice.Viewport.Width - size.X) / 2;
            spriteBatch.DrawString(_font, text, new Vector2(x, y), color);
        }
    }
}
