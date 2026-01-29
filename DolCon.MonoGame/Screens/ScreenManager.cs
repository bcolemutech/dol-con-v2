using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Manages game screens and transitions between them.
/// </summary>
public class ScreenManager
{
    private readonly Dictionary<ScreenType, IScreen> _screens = new();
    private IScreen? _currentScreen;
    private readonly ContentManager _content;
    private readonly GraphicsDevice _graphicsDevice;

    public ScreenType CurrentScreenType { get; private set; } = ScreenType.MainMenu;
    public IScreen? CurrentScreen => _currentScreen;

    public ScreenManager(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _content = content;
        _graphicsDevice = graphicsDevice;
    }

    /// <summary>
    /// Register a screen with the manager.
    /// </summary>
    public void RegisterScreen(ScreenType type, IScreen screen)
    {
        _screens[type] = screen;
        if (screen is ScreenBase screenBase)
        {
            screenBase.SetScreenManager(this);
        }
    }

    /// <summary>
    /// Switch to a different screen.
    /// </summary>
    public void SwitchTo(ScreenType type)
    {
        if (!_screens.TryGetValue(type, out var screen))
        {
            throw new InvalidOperationException($"Screen {type} is not registered");
        }

        _currentScreen?.Unload();
        _currentScreen = screen;
        CurrentScreenType = type;
        _currentScreen.Initialize();
        _currentScreen.LoadContent(_content, _graphicsDevice);
    }

    /// <summary>
    /// Update the current screen.
    /// </summary>
    public void Update(GameTime gameTime, InputManager input)
    {
        _currentScreen?.Update(gameTime, input);
    }

    /// <summary>
    /// Draw the current screen.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        _currentScreen?.Draw(spriteBatch);
    }
}

/// <summary>
/// Types of game screens.
/// </summary>
public enum ScreenType
{
    MainMenu,
    Home,
    Navigation,
    Battle,
    Shop,
    Inventory
}
