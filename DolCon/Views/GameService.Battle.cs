using DolCon.Core.Enums;
using DolCon.Core.Models.Combat;
using DolCon.Core.Services;
using DolCon.Enums;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DolCon.Views;

public partial class GameService
{
    // Combat display states
    private enum CombatDisplayState
    {
        WaitingForPlayerInput,
        ShowingPlayerResult,
        ShowingEnemyResult
    }

    private CombatDisplayState _combatDisplayState = CombatDisplayState.WaitingForPlayerInput;

    private void RenderBattle()
    {
        // Initialize combat if needed
        if (_scene.CombatState == null)
        {
            var biome = BiomeMapper.MapToCombatBiome(SaveGameService.CurrentCell.Biome);
            _scene.CombatState = _combatService.StartCombat(
                SaveGameService.Party.Players,
                SaveGameService.Party.Stamina,
                biome,
                _scene.EncounterCR);
            _combatDisplayState = CombatDisplayState.WaitingForPlayerInput;
        }

        var state = _scene.CombatState;

        // Check if combat ended
        if (state.Result != CombatResult.InProgress)
        {
            RenderBattleResult(state);
            return;
        }

        switch (_combatDisplayState)
        {
            case CombatDisplayState.ShowingPlayerResult:
                // Show player's action result, wait for any key to continue
                RenderBattleUI(state);

                // Any key press advances to enemy turn
                if (_flow.Key != null && _flow.Key.Value.Key != ConsoleKey.NoName)
                {
                    // Process enemy turn if it's their turn (result is always InProgress here)
                    if (!state.IsPlayerTurn())
                    {
                        _combatService.ProcessEnemyTurn(state);
                        _combatDisplayState = CombatDisplayState.ShowingEnemyResult;
                    }
                    else
                    {
                        // Back to player's turn (e.g., after defend or if enemy died)
                        _combatDisplayState = CombatDisplayState.WaitingForPlayerInput;
                    }
                }
                break;

            case CombatDisplayState.ShowingEnemyResult:
                // Show enemy's action result, wait for any key to continue
                RenderBattleUI(state);

                // Any key press advances turn
                if (_flow.Key != null && _flow.Key.Value.Key != ConsoleKey.NoName)
                {
                    _combatService.AdvanceTurn(state);
                    _combatService.CheckCombatEnd(state);

                    // Check if next is another enemy
                    if (!state.IsPlayerTurn() && state.Result == CombatResult.InProgress)
                    {
                        _combatService.ProcessEnemyTurn(state);
                        // Stay in ShowingEnemyResult to show this enemy's action
                    }
                    else
                    {
                        _combatDisplayState = CombatDisplayState.WaitingForPlayerInput;
                    }
                }
                break;

            case CombatDisplayState.WaitingForPlayerInput:
            default:
                // Process player input
                if (state.IsPlayerTurn())
                {
                    var actionTaken = ProcessPlayerCombatInput(state);

                    if (actionTaken)
                    {
                        _combatDisplayState = CombatDisplayState.ShowingPlayerResult;
                        // Clear the key so it doesn't immediately advance past the result
                        _flow.Key = null;
                    }
                }
                else
                {
                    // Shouldn't happen, but handle gracefully
                    _combatService.ProcessEnemyTurn(state);
                    _combatDisplayState = CombatDisplayState.ShowingEnemyResult;
                }

                RenderBattleUI(state);
                break;
        }
    }

    /// <summary>
    /// Process player combat input. Returns true if a combat action was taken (attack/defend/flee).
    /// </summary>
    private bool ProcessPlayerCombatInput(CombatState state)
    {
        switch (_flow.Key)
        {
            case { Key: ConsoleKey.A }:
                var targetId = GetSelectedTargetId(state);
                _combatService.ProcessPlayerAction(state, CombatAction.Attack, targetId);
                return true;
            case { Key: ConsoleKey.D }:
                _combatService.ProcessPlayerAction(state, CombatAction.Defend);
                return true;
            case { Key: ConsoleKey.F } when state.CanFlee:
                _combatService.ProcessPlayerAction(state, CombatAction.Flee);
                return true;
            case { Key: ConsoleKey.UpArrow } or { Key: ConsoleKey.W }:
                state.SelectedTargetIndex = Math.Max(0, state.SelectedTargetIndex - 1);
                return false;
            case { Key: ConsoleKey.DownArrow } or { Key: ConsoleKey.S }:
                var aliveCount = state.GetAliveEnemies().Count;
                state.SelectedTargetIndex = Math.Min(aliveCount - 1, state.SelectedTargetIndex + 1);
                return false;
            default:
                return false;
        }
    }

    private Guid? GetSelectedTargetId(CombatState state)
    {
        var aliveEnemies = state.GetAliveEnemies();
        if (aliveEnemies.Count == 0) return null;

        var index = Math.Min(state.SelectedTargetIndex, aliveEnemies.Count - 1);
        return aliveEnemies[index].Id;
    }

    private void RenderBattleUI(CombatState state)
    {
        // Current turn indicator
        var turnInfo = state.IsPlayerTurn()
            ? "[green]Your turn![/]"
            : "[yellow]Enemy turn...[/]";

        // Build compact party status (single line)
        var partyStatus = string.Join(" | ", state.Players.Select(p =>
        {
            var hpColor = p.CurrentHitPoints > p.MaxHitPoints / 2
                ? "green"
                : (p.CurrentHitPoints > p.MaxHitPoints / 4 ? "yellow" : "red");
            var status = p.IsAlive ? (p.HasUsedDefend ? "Def" : "") : "[red]Down[/]";
            return $"{p.Name}: [{hpColor}]{p.CurrentHitPoints}/{p.MaxHitPoints}[/]{(status != "" ? $" {status}" : "")}";
        }));

        // Build compact enemy list
        var aliveEnemies = state.GetAliveEnemies();
        var enemyLines = new List<string>();
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            var enemy = aliveEnemies[i];
            var selected = i == state.SelectedTargetIndex ? "[green bold]>[/] " : "  ";
            var hpColor = enemy.CurrentHitPoints > enemy.MaxHitPoints / 2
                ? "green"
                : (enemy.CurrentHitPoints > enemy.MaxHitPoints / 4 ? "yellow" : "red");
            enemyLines.Add($"{selected}{enemy.Name} [{hpColor}]{enemy.CurrentHitPoints}/{enemy.MaxHitPoints}[/]");
        }

        // Last Action Details - THE MAIN FOCUS
        var actionContent = BuildActionContent(state);

        // Combat log (last entry only for space)
        var lastLog = state.CombatLog.Count > 0 ? state.CombatLog.Last() : "Combat begins...";

        // Build main display - action details prominently at top
        _display.Update(
            new Panel(
                new Rows(
                    Align.Center(new Markup($"[bold red]COMBAT[/] - Turn {state.CurrentTurn + 1} - {turnInfo}")),
                    new Rule("[bold yellow]LAST ACTION[/]").RuleStyle("yellow"),
                    actionContent,
                    new Rule().RuleStyle("dim"),
                    new Markup($"[bold blue]Party:[/] {partyStatus}"),
                    new Markup($"[bold red]Enemies:[/]"),
                    new Markup(string.Join("\n", enemyLines)),
                    new Rule().RuleStyle("dim"),
                    new Markup($"[dim]Log: {Markup.Escape(lastLog)}[/]")
                )).Expand());
        _ctx.Refresh();

        // Controls - show appropriate text based on combat state
        string controlText;
        switch (_combatDisplayState)
        {
            case CombatDisplayState.ShowingPlayerResult:
                controlText = "[dim]Press any key to continue...[/]";
                break;
            case CombatDisplayState.ShowingEnemyResult:
                controlText = "[dim]Press any key to continue...[/]";
                break;
            default:
                controlText = $"[green]A[/]ttack | [blue]D[/]efend{(state.CanFlee ? " | [yellow]F[/]lee" : "")} | [dim]W/S or Arrows to select target[/]";
                break;
        }

        _controls.Update(
            new Panel(
                Align.Center(
                    new Markup(controlText)
                )).Expand());
        _ctx.Refresh();

        SetMessage(MessageType.Info, _scene.Message);
    }

    private IRenderable BuildActionContent(CombatState state)
    {
        // Show attack result if available
        if (state.LastAttackResult != null)
        {
            var result = state.LastAttackResult;

            var lines = new List<IRenderable>
            {
                Align.Center(new Markup($"[bold cyan]{Markup.Escape(result.AttackerName)}[/] attacks [bold red]{Markup.Escape(result.TargetName)}[/] with [bold]{Markup.Escape(result.DamageSource)}[/]")),
                Align.Center(new Markup($"[bold]Roll:[/] {result.GetAttackFormula()}")),
                Align.Center(new Markup($"[bold]Result:[/] {result.GetResultSummary()}"))
            };

            if (result.IsHit)
            {
                lines.Add(Align.Center(new Markup($"[bold]Damage:[/] {result.GetDamageFormula()}")));
            }

            return new Rows(lines.ToArray());
        }

        // Show last combat log entry for non-attack actions (like defend)
        if (state.CombatLog.Count > 0)
        {
            var lastLog = state.CombatLog.Last();
            return Align.Center(new Markup($"[bold yellow]{Markup.Escape(lastLog)}[/]"));
        }

        return Align.Center(new Markup("[dim]Waiting for action...[/]"));
    }

    private void RenderBattleResult(CombatState state)
    {
        var resultMarkup = state.Result switch
        {
            CombatResult.Victory => "[green bold]VICTORY![/]",
            CombatResult.Defeat => "[red bold]DEFEAT![/]",
            CombatResult.Fled => "[yellow]FLED![/]",
            _ => ""
        };

        var summaryLines = new List<IRenderable>
        {
            Align.Center(new Markup(resultMarkup)),
            new Rule()
        };

        if (state.Result == CombatResult.Victory)
        {
            summaryLines.Add(new Markup($"XP Earned: [green]{state.TotalXPEarned}[/]"));
            if (state.PendingLoot.Count > 0)
            {
                summaryLines.Add(new Markup($"Loot: [yellow]{state.PendingLoot.Count} item(s)[/]"));
            }

            // Apply victory rewards
            ApplyVictoryRewards(state);
        }
        else if (state.Result == CombatResult.Fled)
        {
            summaryLines.Add(new Markup("[yellow]The party escaped, but lost some stamina.[/]"));
            ApplyFleeConsequences();
        }
        else if (state.Result == CombatResult.Defeat)
        {
            summaryLines.Add(new Markup("[red]The party was overwhelmed.[/]"));
        }

        summaryLines.Add(new Rule());
        summaryLines.Add(Align.Center(new Markup("[dim]Press any key to continue...[/]")));

        _display.Update(
            new Panel(
                Align.Center(
                    new Rows(summaryLines.ToArray()),
                    VerticalAlignment.Middle
                )).Expand());
        _ctx.Refresh();

        _controls.Update(new Panel(Align.Center(new Markup(" "))).Expand());
        _ctx.Refresh();

        // Wait for key press then return to navigation
        if (_flow.Key != null && _flow.Key.Value.Key != ConsoleKey.A &&
            _flow.Key.Value.Key != ConsoleKey.D && _flow.Key.Value.Key != ConsoleKey.F)
        {
            CompleteBattle();
        }
    }

    private void ApplyVictoryRewards(CombatState state)
    {
        // Restore half of lost stamina
        var newStamina = CombatService.CalculatePostCombatStamina(state, SaveGameService.Party.Stamina);
        SaveGameService.Party.Stamina = newStamina;

        // Give loot from pending loot (enemy drops) to players with inventory space
        var playersWithSpace = SaveGameService.Party.Players
            .Where(p => p.Inventory.Count < 50)
            .ToList();

        foreach (var lootDrop in state.PendingLoot)
        {
            var recipient = playersWithSpace.FirstOrDefault(p => p.Inventory.Count < 50);
            if (recipient == null) break;

            // Convert loot drop to item via shop service
            var item = _shopService.GenerateReward();
            recipient.Inventory.Add(item);
        }

        // Give coin rewards based on XP earned
        var coinReward = state.TotalXPEarned / 2;
        SaveGameService.Party.Players.ForEach(p => p.coin += coinReward);
    }

    private void ApplyFleeConsequences()
    {
        // Deduct flee stamina cost (5%)
        SaveGameService.Party.Stamina = Math.Max(0, SaveGameService.Party.Stamina - 0.05);
    }

    private void CompleteBattle()
    {
        _scene.IsCompleted = true;
        _scene.CombatState = null;
        _scene.EncounterCR = 0;
        _combatDisplayState = CombatDisplayState.WaitingForPlayerInput;
        _flow.Screen = Screen.Navigation;
        _flow.Redirect = true;
    }
}
