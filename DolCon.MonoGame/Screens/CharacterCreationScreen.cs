using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Models;
using DolCon.Core.Services;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

public class CharacterCreationScreen : ScreenBase
{
    private readonly IMapService _mapService;
    private readonly ISaveGameService _saveGameService;

    private FileInfo _selectedMap = null!;
    private CreationPhase _phase;
    private string _playerName = "";
    private PlayerAbilities _abilities = null!;
    private int _selectedAbility;
    private string _errorMessage = "";

    private static readonly string[] AbilityNames = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
    private const int MaxNameLength = 20;

    private enum CreationPhase
    {
        NameEntry,
        AbilityAllocation
    }

    public CharacterCreationScreen(IMapService mapService, ISaveGameService saveGameService)
    {
        _mapService = mapService;
        _saveGameService = saveGameService;
    }

    public void SetSelectedMap(FileInfo map)
    {
        _selectedMap = map;
    }

    public override void Initialize()
    {
        _phase = CreationPhase.NameEntry;
        _playerName = "";
        _abilities = new PlayerAbilities
        {
            Strength = PointBuySystem.MinScore,
            Dexterity = PointBuySystem.MinScore,
            Constitution = PointBuySystem.MinScore,
            Intelligence = PointBuySystem.MinScore,
            Wisdom = PointBuySystem.MinScore,
            Charisma = PointBuySystem.MinScore
        };
        _selectedAbility = 0;
        _errorMessage = "";
    }

    public override void Update(GameTime gameTime, InputManager input)
    {
        switch (_phase)
        {
            case CreationPhase.NameEntry:
                UpdateNameEntry(input);
                break;
            case CreationPhase.AbilityAllocation:
                UpdateAbilityAllocation(input);
                break;
        }
    }

    private void UpdateNameEntry(InputManager input)
    {
        if (input.IsKeyPressed(Keys.Escape))
        {
            ScreenManager.SwitchTo(ScreenType.MainMenu);
            return;
        }

        if (input.IsKeyPressed(Keys.Enter))
        {
            if (_playerName.Trim().Length == 0)
            {
                _errorMessage = "Please enter a name";
                return;
            }
            _errorMessage = "";
            _phase = CreationPhase.AbilityAllocation;
            input.ConsumeInput();
            return;
        }

        if (input.IsKeyPressed(Keys.Back) && _playerName.Length > 0)
        {
            _playerName = _playerName[..^1];
            _errorMessage = "";
            return;
        }

        // Handle character input
        if (_playerName.Length >= MaxNameLength) return;

        var typedChar = GetTypedCharacter(input);
        if (typedChar.HasValue)
        {
            _playerName += typedChar.Value;
            _errorMessage = "";
        }
    }

    private static char? GetTypedCharacter(InputManager input)
    {
        var shift = input.IsShiftHeld;

        // Letters A-Z
        for (var k = Keys.A; k <= Keys.Z; k++)
        {
            if (input.IsKeyPressed(k))
            {
                var c = (char)('a' + (k - Keys.A));
                return shift ? char.ToUpper(c) : c;
            }
        }

        // Space
        if (input.IsKeyPressed(Keys.Space)) return ' ';

        // Numbers 0-9
        for (var k = Keys.D0; k <= Keys.D9; k++)
        {
            if (input.IsKeyPressed(k))
                return (char)('0' + (k - Keys.D0));
        }

        // Hyphen/minus
        if (input.IsKeyPressed(Keys.OemMinus)) return '-';

        return null;
    }

    private void UpdateAbilityAllocation(InputManager input)
    {
        if (input.IsKeyPressed(Keys.Escape) || input.IsKeyPressed(Keys.Back))
        {
            _phase = CreationPhase.NameEntry;
            _errorMessage = "";
            return;
        }

        // Navigate abilities
        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedAbility = (_selectedAbility - 1 + AbilityNames.Length) % AbilityNames.Length;
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedAbility = (_selectedAbility + 1) % AbilityNames.Length;
        }

        // Adjust selected ability
        if (input.IsKeyPressed(Keys.Right) || input.IsKeyPressed(Keys.D))
        {
            TryIncreaseAbility(_selectedAbility);
        }
        else if (input.IsKeyPressed(Keys.Left) || input.IsKeyPressed(Keys.A))
        {
            TryDecreaseAbility(_selectedAbility);
        }

        // Confirm
        if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
        {
            if (PointBuySystem.IsValid(_abilities))
            {
                ConfirmCharacter();
            }
            else
            {
                var remaining = PointBuySystem.GetRemainingPoints(_abilities);
                _errorMessage = $"You have {remaining} point{(remaining == 1 ? "" : "s")} remaining";
            }
        }
    }

    private int GetAbilityScore(int index)
    {
        return index switch
        {
            0 => _abilities.Strength,
            1 => _abilities.Dexterity,
            2 => _abilities.Constitution,
            3 => _abilities.Intelligence,
            4 => _abilities.Wisdom,
            5 => _abilities.Charisma,
            _ => 0
        };
    }

    private void SetAbilityScore(int index, int value)
    {
        switch (index)
        {
            case 0: _abilities.Strength = value; break;
            case 1: _abilities.Dexterity = value; break;
            case 2: _abilities.Constitution = value; break;
            case 3: _abilities.Intelligence = value; break;
            case 4: _abilities.Wisdom = value; break;
            case 5: _abilities.Charisma = value; break;
        }
    }

    private void TryIncreaseAbility(int index)
    {
        var current = GetAbilityScore(index);
        if (PointBuySystem.CanAffordIncrease(_abilities, current))
        {
            SetAbilityScore(index, current + 1);
            _errorMessage = "";
        }
    }

    private void TryDecreaseAbility(int index)
    {
        var current = GetAbilityScore(index);
        if (PointBuySystem.CanDecrease(current))
        {
            SetAbilityScore(index, current - 1);
            _errorMessage = "";
        }
    }

    private void ConfirmCharacter()
    {
        _mapService.LoadMap(_selectedMap, _playerName.Trim(), _abilities);

        var existingFiles = _saveGameService.GetSaves()
            .Select(f => f.Name).ToArray();
        var mapName = SaveGameService.CurrentMap.info?.mapName ?? "unknown";
        var sanitizedName = SaveGameService.SanitizePlayerName(_playerName.Trim());
        SaveGameService.CurrentSaveName = SaveGameService.GenerateSaveName(
            mapName, sanitizedName, existingFiles);

        var path = _saveGameService.SaveGame().Result;
        _saveGameService.LoadGame(new FileInfo(path)).Wait();

        ScreenManager.SwitchTo(ScreenType.Home);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Title
        DrawCenteredText(spriteBatch, "CREATE YOUR CHARACTER", 60, Color.Gold);

        switch (_phase)
        {
            case CreationPhase.NameEntry:
                DrawNameEntry(spriteBatch);
                break;
            case CreationPhase.AbilityAllocation:
                DrawAbilityAllocation(spriteBatch);
                break;
        }

        // Error message
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            DrawCenteredText(spriteBatch, _errorMessage, 620, Color.Red);
        }
    }

    private void DrawNameEntry(SpriteBatch spriteBatch)
    {
        DrawCenteredText(spriteBatch, "Enter Character Name", 160, Color.White);

        // Name input box
        var boxWidth = 400;
        var boxX = (GraphicsDevice.Viewport.Width - boxWidth) / 2;
        var boxRect = new Rectangle(boxX, 220, boxWidth, 40);
        DrawRect(spriteBatch, boxRect, new Color(30, 30, 30));
        DrawBorder(spriteBatch, boxRect, Color.Gray, 2);

        // Name text with cursor
        var displayName = _playerName + "_";
        DrawText(spriteBatch, displayName, new Vector2(boxX + 10, 228), Color.White);

        // Controls
        DrawCenteredText(spriteBatch, "[Enter] Continue    [Esc] Back", 660, Color.Gray);
    }

    private void DrawAbilityAllocation(SpriteBatch spriteBatch)
    {
        var remaining = PointBuySystem.GetRemainingPoints(_abilities);
        var pointsColor = remaining == 0 ? Color.Green : Color.Yellow;
        DrawCenteredText(spriteBatch, $"Points Remaining: {remaining}", 140, pointsColor);

        DrawCenteredText(spriteBatch, $"Character: {_playerName}", 180, Color.White);

        var startY = 240;
        var centerX = GraphicsDevice.Viewport.Width / 2;

        for (var i = 0; i < AbilityNames.Length; i++)
        {
            var y = startY + i * 50;
            var score = GetAbilityScore(i);
            var isSelected = i == _selectedAbility;
            var color = isSelected ? Color.Yellow : Color.White;
            var prefix = isSelected ? "> " : "  ";

            // Ability name and score
            var canDecrease = PointBuySystem.CanDecrease(score);
            var canIncrease = PointBuySystem.CanAffordIncrease(_abilities, score);
            var leftArrow = canDecrease ? "<" : " ";
            var rightArrow = canIncrease ? ">" : " ";

            var modifier = (int)Math.Floor((score - 10) / 2.0);
            var modStr = modifier >= 0 ? $"+{modifier}" : $"{modifier}";

            var line = $"{prefix}{AbilityNames[i]}:  {leftArrow} {score,2} {rightArrow}  ({modStr})";
            DrawText(spriteBatch, line, new Vector2(centerX - 120, y), color);

            // Show cost for next point if selected and can increase
            if (isSelected && canIncrease)
            {
                var nextCost = PointBuySystem.GetCost(score + 1) - PointBuySystem.GetCost(score);
                DrawText(spriteBatch, $"cost: {nextCost}", new Vector2(centerX + 120, y), Color.Gray);
            }
        }

        // Controls
        var confirmColor = remaining == 0 ? Color.White : Color.DarkGray;
        DrawCenteredText(spriteBatch, "[Up/Down] Select    [Left/Right] Adjust", 640, Color.Gray);
        DrawCenteredText(spriteBatch, "[Enter] Confirm    [Esc] Back to Name", 665, confirmColor);
    }
}
