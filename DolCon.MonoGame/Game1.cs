using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.MonoGame.Input;
using DolCon.MonoGame.Screens;
using DolCon.Core.Services;

namespace DolCon.MonoGame;

/// <summary>
/// Main game class for DolCon MonoGame implementation.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private ScreenManager _screenManager = null!;
    private readonly InputManager _inputManager = new();

    // Core services
    private readonly ISaveGameService _saveGameService = new SaveGameService();
    private readonly IShopService _shopService;
    private readonly ICombatService _combatService;
    private readonly IEventService _eventService;
    private readonly IMoveService _moveService;
    private readonly IMapService _mapService;
    private readonly IPlayerService _playerService;

    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Initialize core services
        _playerService = new PlayerService();
        var positionHandler = new NoOpPositionUpdateHandler();
        _mapService = new MapService(_playerService, positionHandler);
        _moveService = new MoveService(positionHandler);

        var itemsService = new ItemsService();
        var servicesService = new ServicesService();
        _shopService = new ShopService(servicesService, _moveService, itemsService);
        _combatService = new CombatService(_shopService);
        _eventService = new EventService(_shopService);
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
        _graphics.ApplyChanges();

        Window.Title = "Dominion of Light";

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Initialize screen manager
        _screenManager = new ScreenManager(Content, GraphicsDevice);

        // Register screens
        _screenManager.RegisterScreen(ScreenType.MainMenu, new MainMenuScreen(_mapService, _saveGameService));
        _screenManager.RegisterScreen(ScreenType.Home, new HomeScreen());
        _screenManager.RegisterScreen(ScreenType.Navigation, new NavigationScreen(_moveService, _eventService));
        _screenManager.RegisterScreen(ScreenType.Battle, new BattleScreen(_combatService));
        _screenManager.RegisterScreen(ScreenType.Inventory, new InventoryScreen());

        // Start at main menu
        _screenManager.SwitchTo(ScreenType.MainMenu);
    }

    protected override void Update(GameTime gameTime)
    {
        _inputManager.Update(gameTime);

        // Global exit with Escape (when not in a screen that handles it)
        if (_inputManager.IsKeyPressed(Keys.Escape) &&
            _screenManager.CurrentScreenType == ScreenType.MainMenu)
        {
            Exit();
        }

        _screenManager.Update(gameTime, _inputManager);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();
        _screenManager.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void OnExiting(object sender, EventArgs args)
    {
        // Auto-save on exit
        if (SaveGameService.CurrentMap.info != null)
        {
            _saveGameService.SaveGame().Wait();
        }

        base.OnExiting(sender, args);
    }
}
