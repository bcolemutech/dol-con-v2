using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.Combat;
using DolCon.Core.Services;
using DolCon.MonoGame.Input;

namespace DolCon.MonoGame.Screens;

/// <summary>
/// Battle screen for turn-based combat with D&D-style turn order cards.
/// </summary>
public class BattleScreen : ScreenBase
{
    private readonly ICombatService _combatService;
    private readonly ISkillService _skillService;
    private CombatState? _combatState;
    private BattlePhase _phase = BattlePhase.Init;
    private int _selectedAction;
    private readonly string[] _actions = { "Attack", "Defend", "Flee" };
    private string _lastActionResult = "";
    private InputManager? _lastInput;
    private Scene? _currentScene;

    // Turn order card constants
    private const int CardWidth = 130;
    private const int CardHeight = 130;
    private const int CardSpacing = 10;
    private const int TurnOrderY = 55;

    private enum BattlePhase
    {
        Init,
        PlayerTurn,
        ShowingResult,
        EnemyTurn,
        Victory,
        Defeat,
        Fled
    }

    public BattleScreen(ICombatService combatService, ISkillService skillService)
    {
        _combatService = combatService;
        _skillService = skillService;
    }

    /// <summary>
    /// Initialize with a scene containing pending exploration data.
    /// </summary>
    public void InitializeWithScene(Scene scene)
    {
        _currentScene = scene;
        Initialize();
    }

    public override void Initialize()
    {
        // Generate a random encounter based on current location
        var cell = SaveGameService.CurrentCell;
        var party = SaveGameService.Party;
        var players = party.Players;

        if (players.Count > 0)
        {
            // Get biome type for enemy spawning (cell.biome is an index matching Biome enum)
            var gameBiome = (Biome)cell.biome;
            var biome = BiomeMapper.MapToCombatBiome(gameBiome);

            // Get challenge rating from cell (default to 1.0 if not set)
            var challengeRating = cell.ChallengeRating > 0 ? cell.ChallengeRating : 1.0;

            _combatState = _combatService.StartCombat(players, party.Stamina, biome, challengeRating);
            _selectedAction = 0;
            _lastActionResult = "";

            // Check who won initiative
            if (_combatState.IsPlayerTurn())
            {
                _phase = BattlePhase.PlayerTurn;
            }
            else
            {
                // Enemy goes first - process their turn and show result
                _combatService.ProcessEnemyTurn(_combatState);
                _combatService.AdvanceTurn(_combatState); // Advance to next combatant after enemy acts
                _lastActionResult = _combatState.CombatLog.LastOrDefault() ?? "Enemy attacks first!";

                // Check if enemy attack defeated the player
                if (_combatState.Result == CombatResult.Defeat)
                {
                    _phase = BattlePhase.Defeat;
                    _lastActionResult = "You have been defeated...";
                }
                else
                {
                    _phase = BattlePhase.ShowingResult;
                }
            }
        }
        else
        {
            // No players - shouldn't happen but handle gracefully
            ScreenManager.SwitchTo(ScreenType.Navigation);
        }
    }

    public override void Update(GameTime gameTime, InputManager input)
    {
        _lastInput = input;
        if (_combatState == null) return;

        switch (_phase)
        {
            case BattlePhase.PlayerTurn:
                UpdatePlayerTurn(input);
                break;
            case BattlePhase.ShowingResult:
                if (input.AnyKeyPressed())
                {
                    AdvanceCombat();
                }
                break;
            case BattlePhase.Victory:
            case BattlePhase.Defeat:
            case BattlePhase.Fled:
                if (input.AnyKeyPressed())
                {
                    ApplyPostCombatEffects();
                    ScreenManager.SwitchTo(ScreenType.Navigation);
                }
                break;
        }
    }

    private void UpdatePlayerTurn(InputManager input)
    {
        // Arrow key navigation (Up/Down or W/S)
        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedAction = (_selectedAction - 1 + _actions.Length) % _actions.Length;
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedAction = (_selectedAction + 1) % _actions.Length;
        }
        // Left/Right also works for horizontal menu
        else if (input.IsKeyPressed(Keys.Left) || input.IsKeyPressed(Keys.A))
        {
            _selectedAction = (_selectedAction - 1 + _actions.Length) % _actions.Length;
        }
        else if (input.IsKeyPressed(Keys.Right) || input.IsKeyPressed(Keys.D))
        {
            _selectedAction = (_selectedAction + 1) % _actions.Length;
        }
        // Enter or Space to select
        else if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
        {
            ExecuteAction();
        }
    }

    private void ExecuteAction()
    {
        if (_combatState == null) return;

        var action = _selectedAction switch
        {
            0 => CombatAction.Attack,
            1 => CombatAction.Defend,
            2 => CombatAction.Flee,
            _ => CombatAction.Attack
        };

        // Check flee eligibility BEFORE processing
        if (action == CombatAction.Flee && !_combatState.CanFlee)
        {
            _lastActionResult = "Cannot flee after the first turn!";
            return;
        }

        // Get the first enemy that's still alive as target
        var target = _combatState.Enemies.FirstOrDefault(e => e.CurrentHitPoints > 0);
        if (target == null && action == CombatAction.Attack)
        {
            return; // No valid target
        }

        _combatService.ProcessPlayerAction(_combatState, action, target?.Id ?? Guid.Empty);

        // Check result AFTER processing
        if (_combatState.Result == CombatResult.Fled)
        {
            _phase = BattlePhase.Fled;
            _lastActionResult = "You fled from battle!";
        }
        else
        {
            _phase = BattlePhase.ShowingResult;
            _lastActionResult = _combatState.CombatLog.LastOrDefault() ?? "";
        }

        // Consume input to prevent immediate phase transition
        _lastInput?.ConsumeInput();
    }

    private void AdvanceCombat()
    {
        if (_combatState == null) return;

        // Check for victory/defeat
        if (_combatState.Result == CombatResult.Victory)
        {
            _phase = BattlePhase.Victory;
            _lastActionResult = $"Victory! Gained {_combatState.TotalXPEarned} XP";
            return;
        }

        if (_combatState.Result == CombatResult.Defeat)
        {
            _phase = BattlePhase.Defeat;
            _lastActionResult = "You have been defeated...";
            return;
        }

        // Check if it's already the player's turn (e.g., after enemy attacked first in init)
        if (_combatState.IsPlayerTurn())
        {
            _phase = BattlePhase.PlayerTurn;
            return;
        }

        // Process enemy turn
        _combatService.ProcessEnemyTurn(_combatState);
        _combatService.AdvanceTurn(_combatState); // Advance to next combatant after enemy acts
        _lastActionResult = _combatState.CombatLog.LastOrDefault() ?? "";

        // Check again after enemy turn
        if (_combatState.Result == CombatResult.Defeat)
        {
            _phase = BattlePhase.Defeat;
            _lastActionResult = "You have been defeated...";
            return;
        }

        // Stay in ShowingResult to display the enemy's attack
        // Next call to AdvanceCombat will check IsPlayerTurn at the start
        _phase = BattlePhase.ShowingResult;
    }

    private void ApplyPostCombatEffects()
    {
        if (_combatState == null) return;

        var party = SaveGameService.Party;

        // Apply stamina changes based on combat result
        party.Stamina = CombatService.CalculatePostCombatStamina(_combatState, party.Stamina);

        // Apply victory rewards and commit exploration only on victory
        if (_combatState.Result == CombatResult.Victory)
        {
            // Give coin rewards based on XP
            var coinReward = _combatState.TotalXPEarned / 2;
            foreach (var player in party.Players)
            {
                player.coin += coinReward;
                _skillService.ApplySkillGains(player, _combatState);
            }

            // Only commit exploration progress on victory
            if (_currentScene != null)
            {
                EventService.CommitPendingExploration(_currentScene);
            }
        }

        // Clear scene reference
        _currentScene = null;

        // Auto-save after combat
        SaveHelper.TriggerSave();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var padding = 20;
        var viewport = GraphicsDevice.Viewport;

        // Title bar (Y=0-50)
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, 50), new Color(50, 20, 20));
        DrawText(spriteBatch, "COMBAT", new Vector2(padding, 15), Color.Red);

        // Show round number
        if (_combatState != null)
        {
            var roundText = $"Round {_combatState.CurrentTurn + 1}";
            DrawText(spriteBatch, roundText, new Vector2(viewport.Width - 150, 15), Color.White);
        }

        if (_combatState == null)
        {
            DrawText(spriteBatch, "No combat state!", new Vector2(padding, 100), Color.White);
            return;
        }

        // Turn order cards (Y=55-185)
        DrawTurnOrderCards(spriteBatch, padding);

        // Middle section (Y=195+)
        var middleY = TurnOrderY + CardHeight + 10;

        // LEFT COLUMN: Player status panel
        var playerPanel = new Rectangle(padding, middleY, 280, 150);
        DrawBorder(spriteBatch, playerPanel, Color.Blue, 2);
        DrawPlayerPanel(spriteBatch, playerPanel);

        // CENTER COLUMN: Combat log
        var logPanel = new Rectangle(padding + 290, middleY, 350, 280);
        DrawBorder(spriteBatch, logPanel, Color.DarkGray, 1);
        DrawCombatLog(spriteBatch, logPanel);

        // RIGHT COLUMN: Attack result
        var resultPanel = new Rectangle(padding + 650, middleY, 350, 280);
        DrawBorder(spriteBatch, resultPanel, new Color(80, 80, 40), 1);
        DrawAttackResult(spriteBatch, resultPanel.X + 10, resultPanel.Y + 10);

        // Enemy strip (Y=480-570)
        var enemyStripY = middleY + 290;
        DrawEnemyStrip(spriteBatch, padding, enemyStripY, viewport.Width - padding * 2);

        // Controls panel (bottom)
        var controlsY = viewport.Height - 100;
        DrawRect(spriteBatch, new Rectangle(0, controlsY - 10, viewport.Width, 110), new Color(30, 30, 40));
        DrawControls(spriteBatch, padding, controlsY);
    }

    private void DrawTurnOrderCards(SpriteBatch spriteBatch, int padding)
    {
        if (_combatState == null) return;

        var x = padding;
        var maxCards = (GraphicsDevice.Viewport.Width - padding * 2) / (CardWidth + CardSpacing);

        // If too many combatants, compress cards
        var cardWidth = CardWidth;
        var cardCount = _combatState.TurnOrder.Count;
        if (cardCount > maxCards)
        {
            cardWidth = Math.Max(100, (GraphicsDevice.Viewport.Width - padding * 2 - CardSpacing * (cardCount - 1)) / cardCount);
        }

        for (int i = 0; i < _combatState.TurnOrder.Count; i++)
        {
            var entity = _combatState.TurnOrder[i];
            var isActive = i == _combatState.CurrentTurnIndex;
            var cardRect = new Rectangle(x, TurnOrderY, cardWidth, CardHeight);

            DrawCombatCard(spriteBatch, entity, cardRect, isActive);
            x += cardWidth + CardSpacing;

            // Stop if cards go off screen
            if (x + cardWidth > GraphicsDevice.Viewport.Width - padding)
                break;
        }
    }

    private void DrawCombatCard(SpriteBatch spriteBatch, CombatEntity entity, Rectangle cardRect, bool isActive)
    {
        if (_combatState == null) return;

        var isPlayer = _combatState.Players.Any(p => p.Id == entity.Id);
        var isDead = !entity.IsAlive;

        // Card background color
        var bgColor = isDead ? new Color(50, 50, 50) :
                      isPlayer ? new Color(40, 60, 100) : new Color(100, 40, 40);

        // Active glow effect
        if (isActive && !isDead)
        {
            var glowRect = new Rectangle(cardRect.X - 4, cardRect.Y - 4,
                                          cardRect.Width + 8, cardRect.Height + 8);
            DrawRect(spriteBatch, glowRect, new Color(255, 215, 0, 80)); // Gold glow
        }

        DrawRect(spriteBatch, cardRect, bgColor);

        // Border
        var borderColor = isActive ? Color.Gold : (isDead ? Color.DarkGray : Color.White);
        var borderThickness = isActive ? 3 : 1;
        DrawBorder(spriteBatch, cardRect, borderColor, borderThickness);

        // Content
        DrawCardContent(spriteBatch, entity, cardRect, isPlayer, isDead);
    }

    private void DrawCardContent(SpriteBatch spriteBatch, CombatEntity entity, Rectangle cardRect, bool isPlayer, bool isDead)
    {
        int x = cardRect.X + 5;
        int y = cardRect.Y + 5;
        var textColor = isDead ? Color.Gray : Color.White;
        var cardWidth = cardRect.Width;

        // Row 1: Initiative + Type indicator
        DrawText(spriteBatch, $"Init:{entity.Initiative}", new Vector2(x, y), textColor);
        var indicator = isPlayer ? "[P]" : "[E]";
        var indicatorColor = isDead ? Color.Gray : (isPlayer ? Color.LightBlue : Color.LightCoral);
        DrawText(spriteBatch, indicator, new Vector2(cardRect.Right - 35, y), indicatorColor);
        y += 22;

        // Row 2: Name (truncated)
        var maxNameLen = cardWidth > 110 ? 12 : 10;
        var name = TruncateText(entity.Name, maxNameLen);
        if (isDead) name = "X " + TruncateText(entity.Name, maxNameLen - 2);
        DrawText(spriteBatch, name, new Vector2(x, y), textColor);
        y += 24;

        // Row 3: HP bar
        var hpPercent = isDead ? 0f : (float)entity.CurrentHitPoints / entity.MaxHitPoints;
        var barWidth = cardWidth - 15;
        var hpBarBg = new Rectangle(x, y, barWidth, 14);
        var hpBarFill = new Rectangle(x, y, (int)(barWidth * hpPercent), 14);
        DrawRect(spriteBatch, hpBarBg, Color.DarkRed);
        if (!isDead)
        {
            DrawRect(spriteBatch, hpBarFill, isPlayer ? Color.Green : Color.Red);
        }
        y += 16;

        // HP values
        var hpText = isDead ? "DEAD" : $"{entity.CurrentHitPoints}/{entity.MaxHitPoints}";
        DrawText(spriteBatch, hpText, new Vector2(x, y), textColor);
        y += 20;

        // Row 4: AC
        DrawText(spriteBatch, $"AC: {entity.ArmorClass}", new Vector2(x, y), textColor);
        y += 20;

        // Row 5: Modifiers
        if (entity.HasUsedDefend && !isDead)
        {
            DrawText(spriteBatch, "Def +2", new Vector2(x, y), Color.Cyan);
        }
    }

    private void DrawPlayerPanel(SpriteBatch spriteBatch, Rectangle panel)
    {
        if (_combatState == null) return;

        var player = _combatState.Players.FirstOrDefault();
        if (player == null) return;

        int x = panel.X + 10;
        int y = panel.Y + 10;

        DrawText(spriteBatch, "Player Status", new Vector2(x, y), Color.LightBlue);
        y += 25;

        DrawText(spriteBatch, player.Name, new Vector2(x, y), Color.White);
        y += 22;

        DrawText(spriteBatch, $"HP: {player.CurrentHitPoints}/{player.MaxHitPoints}", new Vector2(x, y), Color.White);
        y += 20;

        // HP bar
        var hpPercent = (float)player.CurrentHitPoints / player.MaxHitPoints;
        var hpBarBg = new Rectangle(x, y, 200, 16);
        var hpBar = new Rectangle(x, y, (int)(200 * hpPercent), 16);
        DrawRect(spriteBatch, hpBarBg, Color.DarkRed);
        DrawRect(spriteBatch, hpBar, Color.Green);
        y += 22;

        // AC with defend indicator
        var acText = $"AC: {player.ArmorClass}";
        if (player.HasUsedDefend)
        {
            acText += " (Defending +2)";
        }
        DrawText(spriteBatch, acText, new Vector2(x, y), Color.LightGray);
    }

    private void DrawCombatLog(SpriteBatch spriteBatch, Rectangle panel)
    {
        if (_combatState == null) return;

        int x = panel.X + 10;
        int y = panel.Y + 5;

        DrawText(spriteBatch, "Combat Log", new Vector2(x, y), Color.Gray);
        y += 25;

        var logEntries = _combatState.CombatLog.TakeLast(10).ToList();
        foreach (var entry in logEntries)
        {
            DrawText(spriteBatch, TruncateText(entry, 45), new Vector2(x, y), Color.LightGray);
            y += 22;
        }
    }

    private void DrawEnemyStrip(SpriteBatch spriteBatch, int x, int y, int width)
    {
        if (_combatState == null) return;

        var stripPanel = new Rectangle(x, y, width, 90);
        DrawRect(spriteBatch, stripPanel, new Color(40, 30, 30));
        DrawBorder(spriteBatch, stripPanel, Color.DarkRed, 1);

        DrawText(spriteBatch, "Enemies:", new Vector2(x + 10, y + 5), Color.LightCoral);

        var enemyX = x + 10;
        var enemyY = y + 28;
        var enemyWidth = Math.Min(200, (width - 20) / Math.Max(1, _combatState.Enemies.Count));

        foreach (var enemy in _combatState.Enemies)
        {
            var color = enemy.CurrentHitPoints > 0 ? Color.White : Color.Gray;
            var nameText = TruncateText($"{enemy.Name} (CR{enemy.ChallengeRating:F1})", 22);
            DrawText(spriteBatch, nameText, new Vector2(enemyX, enemyY), color);

            // HP bar
            var hpPercent = (float)enemy.CurrentHitPoints / enemy.MaxHitPoints;
            var barY = enemyY + 22;
            var barWidth = Math.Min(150, enemyWidth - 10);
            var hpBarBg = new Rectangle(enemyX, barY, barWidth, 12);
            var hpBar = new Rectangle(enemyX, barY, (int)(barWidth * Math.Max(0, hpPercent)), 12);
            DrawRect(spriteBatch, hpBarBg, Color.DarkRed);
            if (enemy.CurrentHitPoints > 0)
            {
                DrawRect(spriteBatch, hpBar, Color.Red);
            }

            // HP text
            var hpText = enemy.CurrentHitPoints > 0 ? $"{enemy.CurrentHitPoints}/{enemy.MaxHitPoints}" : "DEAD";
            DrawText(spriteBatch, hpText, new Vector2(enemyX + barWidth + 5, barY - 2), color);

            enemyX += enemyWidth + 20;
        }
    }

    private void DrawControls(SpriteBatch spriteBatch, int padding, int controlsY)
    {
        if (_combatState == null) return;

        if (_phase == BattlePhase.PlayerTurn)
        {
            DrawText(spriteBatch, "Choose your action:", new Vector2(padding, controlsY), Color.White);
            for (int i = 0; i < _actions.Length; i++)
            {
                var actionColor = i == _selectedAction ? Color.Yellow : Color.Gray;
                var prefix = i == _selectedAction ? "> " : "  ";
                var fleeDisabled = i == 2 && !_combatState.CanFlee;
                if (fleeDisabled) actionColor = Color.DarkGray;
                DrawText(spriteBatch, $"{prefix}{_actions[i]}",
                    new Vector2(padding + 200 + i * 120, controlsY), actionColor);
            }
            // Show controls hint and flee restriction
            var hintY = controlsY + 30;
            DrawText(spriteBatch, "[Arrow Keys] Select  [Enter] Confirm", new Vector2(padding, hintY), Color.Gray);
            if (!_combatState.CanFlee)
            {
                DrawText(spriteBatch, "(Can only flee on first turn)", new Vector2(padding + 350, hintY), Color.DarkGray);
            }
        }
        else if (_phase == BattlePhase.ShowingResult)
        {
            DrawText(spriteBatch, "Press any key to continue...", new Vector2(padding, controlsY), Color.Gray);
        }
        else if (_phase == BattlePhase.Victory)
        {
            DrawText(spriteBatch, _lastActionResult, new Vector2(padding, controlsY), Color.Gold);
            DrawText(spriteBatch, "Press any key to continue...", new Vector2(padding, controlsY + 30), Color.Gray);
        }
        else if (_phase == BattlePhase.Defeat)
        {
            DrawText(spriteBatch, _lastActionResult, new Vector2(padding, controlsY), Color.Red);
            DrawText(spriteBatch, "Press any key to continue...", new Vector2(padding, controlsY + 30), Color.Gray);
        }
        else if (_phase == BattlePhase.Fled)
        {
            DrawText(spriteBatch, _lastActionResult, new Vector2(padding, controlsY), Color.Yellow);
            DrawText(spriteBatch, "Press any key to continue...", new Vector2(padding, controlsY + 30), Color.Gray);
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? "";
        return text.Substring(0, maxLength - 2) + "..";
    }

    private void DrawAttackResult(SpriteBatch spriteBatch, int x, int y)
    {
        DrawText(spriteBatch, "Last Action", new Vector2(x, y), Color.Gray);
        y += 25;

        if (_combatState?.LastAttackResult != null &&
            (_phase == BattlePhase.ShowingResult || _phase == BattlePhase.EnemyTurn || _phase == BattlePhase.PlayerTurn))
        {
            var result = _combatState.LastAttackResult;

            // Attacker and target
            DrawText(spriteBatch, $"{result.AttackerName} attacks {result.TargetName}",
                new Vector2(x, y), Color.Cyan);
            y += 22;

            // Attack formula
            var formula = result.GetAttackFormula();
            DrawText(spriteBatch, $"Roll: {formula}", new Vector2(x, y), Color.White);
            y += 22;

            // Hit/Miss result
            var hitColor = result.IsCritical ? Color.Gold :
                           result.IsHit ? Color.Green : Color.Red;
            var hitText = result.IsCritical ? "CRITICAL HIT!" :
                          result.IsHit ? $"HIT for {result.TotalDamage} damage" :
                          result.IsNatural1 ? "MISS (Natural 1)" : "MISS";
            DrawText(spriteBatch, hitText, new Vector2(x, y), hitColor);
            y += 22;

            // Damage formula (if hit)
            if (result.IsHit)
            {
                var damageFormula = result.GetDamageFormula();
                DrawText(spriteBatch, $"Damage: {damageFormula}", new Vector2(x, y), Color.Orange);
            }
        }
        else if (!string.IsNullOrEmpty(_lastActionResult))
        {
            DrawText(spriteBatch, _lastActionResult, new Vector2(x, y), Color.Yellow);
        }
        else
        {
            DrawText(spriteBatch, "No actions yet", new Vector2(x, y), Color.DarkGray);
        }
    }
}
