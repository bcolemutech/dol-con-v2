using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Services;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Main menu screen - New Game / Load Game / Exit
/// </summary>
public class MainMenuScreen : ScreenBase
{
    private readonly IMapService _mapService;
    private readonly ISaveGameService _saveGameService;
    private readonly string[] _menuOptions = { "New Game", "Load Game", "Exit" };
    private int _selectedIndex;
    private MenuState _state = MenuState.MainMenu;
    private FileInfo[] _availableMaps = Array.Empty<FileInfo>();
    private FileInfo[] _availableSaves = Array.Empty<FileInfo>();
    private string _statusMessage = "";

    private enum MenuState
    {
        MainMenu,
        SelectMap,
        SelectSave,
        Loading
    }

    public MainMenuScreen(IMapService mapService, ISaveGameService saveGameService)
    {
        _mapService = mapService;
        _saveGameService = saveGameService;
    }

    public override void Initialize()
    {
        _selectedIndex = 0;
        _state = MenuState.MainMenu;
        _statusMessage = "";
    }

    public override void Update(GameTime gameTime, InputManager input)
    {
        switch (_state)
        {
            case MenuState.MainMenu:
                UpdateMainMenu(input);
                break;
            case MenuState.SelectMap:
                UpdateMapSelection(input);
                break;
            case MenuState.SelectSave:
                UpdateSaveSelection(input);
                break;
        }
    }

    private void UpdateMainMenu(InputManager input)
    {
        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedIndex = (_selectedIndex - 1 + _menuOptions.Length) % _menuOptions.Length;
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedIndex = (_selectedIndex + 1) % _menuOptions.Length;
        }
        else if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
        {
            switch (_selectedIndex)
            {
                case 0: // New Game
                    _availableMaps = _mapService.GetMaps().ToArray();
                    if (_availableMaps.Length == 0)
                    {
                        _statusMessage = "No maps found! Place map files in %APPDATA%/DolCon/Maps/";
                    }
                    else
                    {
                        _state = MenuState.SelectMap;
                        _selectedIndex = 0;
                    }
                    break;
                case 1: // Load Game
                    _availableSaves = _saveGameService.GetSaves().ToArray();
                    if (_availableSaves.Length == 0)
                    {
                        _statusMessage = "No saves found!";
                    }
                    else
                    {
                        _state = MenuState.SelectSave;
                        _selectedIndex = 0;
                    }
                    break;
                case 2: // Exit
                    Environment.Exit(0);
                    break;
            }
        }
    }

    private void UpdateMapSelection(InputManager input)
    {
        if (input.IsKeyPressed(Keys.Escape))
        {
            _state = MenuState.MainMenu;
            _selectedIndex = 0;
            return;
        }

        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedIndex = (_selectedIndex - 1 + _availableMaps.Length) % _availableMaps.Length;
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedIndex = (_selectedIndex + 1) % _availableMaps.Length;
        }
        else if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
        {
            _state = MenuState.Loading;
            _statusMessage = "Loading map...";

            // Load the selected map
            _mapService.LoadMap(_availableMaps[_selectedIndex]);

            // Save and load to initialize properly
            var path = _saveGameService.SaveGame().Result;
            _saveGameService.LoadGame(new FileInfo(path)).Wait();

            // Switch to home screen
            ScreenManager.SwitchTo(ScreenType.Home);
        }
    }

    private void UpdateSaveSelection(InputManager input)
    {
        if (input.IsKeyPressed(Keys.Escape))
        {
            _state = MenuState.MainMenu;
            _selectedIndex = 1;
            return;
        }

        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedIndex = (_selectedIndex - 1 + _availableSaves.Length) % _availableSaves.Length;
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedIndex = (_selectedIndex + 1) % _availableSaves.Length;
        }
        else if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
        {
            _state = MenuState.Loading;
            _statusMessage = "Loading save...";

            // Load the selected save
            _saveGameService.LoadGame(_availableSaves[_selectedIndex]).Wait();

            // Switch to home screen
            ScreenManager.SwitchTo(ScreenType.Home);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var centerX = GraphicsDevice.Viewport.Width / 2;
        var startY = 200;

        // Title
        DrawCenteredText(spriteBatch, "DOMINION OF LIGHT", 100, Color.Gold);

        switch (_state)
        {
            case MenuState.MainMenu:
                DrawMenu(spriteBatch, _menuOptions, centerX, startY);
                break;
            case MenuState.SelectMap:
                DrawCenteredText(spriteBatch, "Select a Map", 150, Color.White);
                var mapNames = _availableMaps.Select(m => m.Name).ToArray();
                DrawMenu(spriteBatch, mapNames, centerX, startY);
                DrawCenteredText(spriteBatch, "Press ESC to go back", 500, Color.Gray);
                break;
            case MenuState.SelectSave:
                DrawCenteredText(spriteBatch, "Select a Save", 150, Color.White);
                var saveNames = _availableSaves.Select(s => s.Name).ToArray();
                DrawMenu(spriteBatch, saveNames, centerX, startY);
                DrawCenteredText(spriteBatch, "Press ESC to go back", 500, Color.Gray);
                break;
            case MenuState.Loading:
                DrawCenteredText(spriteBatch, _statusMessage, 300, Color.Yellow);
                break;
        }

        // Status message
        if (!string.IsNullOrEmpty(_statusMessage) && _state == MenuState.MainMenu)
        {
            DrawCenteredText(spriteBatch, _statusMessage, 450, Color.Red);
        }
    }

    private void DrawMenu(SpriteBatch spriteBatch, string[] options, int centerX, int startY)
    {
        for (int i = 0; i < options.Length; i++)
        {
            var color = i == _selectedIndex ? Color.Yellow : Color.White;
            var prefix = i == _selectedIndex ? "> " : "  ";
            DrawCenteredText(spriteBatch, prefix + options[i], startY + i * 40, color);
        }
    }
}
