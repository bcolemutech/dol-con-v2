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
/// Battle screen for turn-based combat.
/// </summary>
public class BattleScreen : ScreenBase
{
    private readonly ICombatService _combatService;
    private CombatState? _combatState;
    private BattlePhase _phase = BattlePhase.Init;
    private int _selectedAction;
    private readonly string[] _actions = { "Attack", "Defend", "Flee" };
    private string _lastActionResult = "";
    private InputManager? _lastInput;

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

    public BattleScreen(ICombatService combatService)
    {
        _combatService = combatService;
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
            _phase = BattlePhase.PlayerTurn;
            _selectedAction = 0;
            _lastActionResult = "";
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
        if (input.IsKeyPressed(Keys.Up) || input.IsKeyPressed(Keys.W))
        {
            _selectedAction = (_selectedAction - 1 + _actions.Length) % _actions.Length;
        }
        else if (input.IsKeyPressed(Keys.Down) || input.IsKeyPressed(Keys.S))
        {
            _selectedAction = (_selectedAction + 1) % _actions.Length;
        }
        else if (input.IsKeyPressed(Keys.Enter) || input.IsKeyPressed(Keys.Space))
        {
            ExecuteAction();
        }

        // Quick keys
        if (input.IsKeyPressed(Keys.A))
        {
            _selectedAction = 0;
            ExecuteAction();
        }
        else if (input.IsKeyPressed(Keys.D))
        {
            _selectedAction = 1;
            ExecuteAction();
        }
        else if (input.IsKeyPressed(Keys.F))
        {
            _selectedAction = 2;
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

        // Get the first enemy that's still alive as target
        var target = _combatState.Enemies.FirstOrDefault(e => e.CurrentHitPoints > 0);
        if (target == null && action == CombatAction.Attack)
        {
            return; // No valid target
        }

        _combatService.ProcessPlayerAction(_combatState, action, target?.Id ?? Guid.Empty);

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

        // Process enemy turn
        _combatService.ProcessEnemyTurn(_combatState);

        // Check again after enemy turn
        if (_combatState.Result == CombatResult.Defeat)
        {
            _phase = BattlePhase.Defeat;
            _lastActionResult = "You have been defeated...";
            return;
        }

        _phase = BattlePhase.PlayerTurn;
    }

    private void ApplyPostCombatEffects()
    {
        if (_combatState == null) return;

        var party = SaveGameService.Party;

        // Apply stamina changes based on combat result
        party.Stamina = CombatService.CalculatePostCombatStamina(_combatState, party.Stamina);

        // Apply victory rewards
        if (_combatState.Result == CombatResult.Victory)
        {
            // Give coin rewards based on XP
            var coinReward = _combatState.TotalXPEarned / 2;
            foreach (var player in party.Players)
            {
                player.coin += coinReward;
            }
        }

        // Auto-save after combat
        SaveHelper.TriggerSave();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var padding = 20;
        var viewport = GraphicsDevice.Viewport;

        // Title bar
        DrawRect(spriteBatch, new Rectangle(0, 0, viewport.Width, 50), new Color(50, 20, 20));
        DrawText(spriteBatch, "COMBAT", new Vector2(padding, 15), Color.Red);

        if (_combatState == null)
        {
            DrawText(spriteBatch, "No combat state!", new Vector2(padding, 100), Color.White);
            return;
        }

        // Player status (left)
        var playerPanel = new Rectangle(padding, 60, 300, 200);
        DrawBorder(spriteBatch, playerPanel, Color.Blue, 2);
        DrawText(spriteBatch, "Player", new Vector2(playerPanel.X + 10, playerPanel.Y + 10), Color.LightBlue);

        var player = _combatState.Players.FirstOrDefault();
        if (player == null) return;

        int y = playerPanel.Y + 40;
        DrawText(spriteBatch, $"HP: {player.CurrentHitPoints}/{player.MaxHitPoints}",
            new Vector2(playerPanel.X + 10, y), Color.White);
        y += 25;

        // HP bar
        var hpPercent = (float)player.CurrentHitPoints / player.MaxHitPoints;
        var hpBarBg = new Rectangle(playerPanel.X + 10, y, 200, 20);
        var hpBar = new Rectangle(playerPanel.X + 10, y, (int)(200 * hpPercent), 20);
        DrawRect(spriteBatch, hpBarBg, Color.DarkRed);
        DrawRect(spriteBatch, hpBar, Color.Green);
        y += 35;

        DrawText(spriteBatch, $"AC: {player.ArmorClass}", new Vector2(playerPanel.X + 10, y), Color.LightGray);

        // Enemies (right)
        var enemyPanel = new Rectangle(viewport.Width - 350 - padding, 60, 350, 300);
        DrawBorder(spriteBatch, enemyPanel, Color.Red, 2);
        DrawText(spriteBatch, "Enemies", new Vector2(enemyPanel.X + 10, enemyPanel.Y + 10), Color.LightCoral);

        y = enemyPanel.Y + 40;
        foreach (var enemy in _combatState.Enemies)
        {
            var color = enemy.CurrentHitPoints > 0 ? Color.White : Color.Gray;
            DrawText(spriteBatch, $"{enemy.Name} (CR {enemy.ChallengeRating})",
                new Vector2(enemyPanel.X + 10, y), color);
            y += 25;

            var enemyHpPercent = (float)enemy.CurrentHitPoints / enemy.MaxHitPoints;
            var enemyHpBarBg = new Rectangle(enemyPanel.X + 10, y, 150, 15);
            var enemyHpBar = new Rectangle(enemyPanel.X + 10, y, (int)(150 * Math.Max(0, enemyHpPercent)), 15);
            DrawRect(spriteBatch, enemyHpBarBg, Color.DarkRed);
            if (enemy.CurrentHitPoints > 0)
            {
                DrawRect(spriteBatch, enemyHpBar, Color.Red);
            }
            DrawText(spriteBatch, $"{enemy.CurrentHitPoints}/{enemy.MaxHitPoints}",
                new Vector2(enemyPanel.X + 170, y), color);
            y += 30;
        }

        // Action result / message with formula display
        DrawAttackResult(spriteBatch, padding, 280);

        // Combat log
        var logPanel = new Rectangle(padding, 310, viewport.Width - 2 * padding, 200);
        DrawBorder(spriteBatch, logPanel, Color.DarkGray, 1);
        y = logPanel.Y + 10;
        var logEntries = _combatState.CombatLog.TakeLast(7).ToList();
        foreach (var entry in logEntries)
        {
            DrawText(spriteBatch, entry, new Vector2(logPanel.X + 10, y), Color.LightGray);
            y += 22;
        }

        // Actions (bottom)
        var controlsY = viewport.Height - 120;

        if (_phase == BattlePhase.PlayerTurn)
        {
            DrawText(spriteBatch, "Choose your action:", new Vector2(padding, controlsY), Color.White);
            for (int i = 0; i < _actions.Length; i++)
            {
                var color = i == _selectedAction ? Color.Yellow : Color.Gray;
                var prefix = i == _selectedAction ? "> " : "  ";
                DrawText(spriteBatch, $"{prefix}[{_actions[i][0]}] {_actions[i]}",
                    new Vector2(padding + 200 + i * 150, controlsY), color);
            }
        }
        else if (_phase == BattlePhase.ShowingResult)
        {
            DrawText(spriteBatch, "Press any key to continue...", new Vector2(padding, controlsY), Color.Gray);
        }
        else if (_phase == BattlePhase.Victory || _phase == BattlePhase.Defeat || _phase == BattlePhase.Fled)
        {
            DrawText(spriteBatch, "Press any key to continue...", new Vector2(padding, controlsY), Color.Gray);
        }
    }

    private void DrawAttackResult(SpriteBatch spriteBatch, int x, int y)
    {
        if (_combatState?.LastAttackResult != null &&
            (_phase == BattlePhase.ShowingResult || _phase == BattlePhase.EnemyTurn))
        {
            var result = _combatState.LastAttackResult;

            // Attacker and target
            DrawText(spriteBatch, $"{result.AttackerName} attacks {result.TargetName}",
                new Vector2(x, y), Color.Cyan);
            y += 25;

            // Attack formula
            var formula = result.GetAttackFormula();
            DrawText(spriteBatch, $"Roll: {formula}", new Vector2(x, y), Color.White);
            y += 25;

            // Hit/Miss result
            var hitColor = result.IsCritical ? Color.Gold :
                           result.IsHit ? Color.Green : Color.Red;
            var hitText = result.IsCritical ? "CRITICAL HIT!" :
                          result.IsHit ? $"HIT for {result.TotalDamage} damage" :
                          result.IsNatural1 ? "MISS (Natural 1)" : "MISS";
            DrawText(spriteBatch, hitText, new Vector2(x, y), hitColor);
            y += 25;

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
    }
}
