using DolCon.Enums;
using DolCon.Models.Combat;
using DolCon.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DolCon.Views;

public partial class GameService
{
    private const int ActionDisplayMs = 1000;

    // Track if we're displaying the player's action result
    private bool _isDisplayingPlayerAction;
    private DateTime? _playerActionDisplayStart;

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
            _isDisplayingPlayerAction = false;
            _playerActionDisplayStart = null;
        }

        var state = _scene.CombatState;

        // Check if combat ended
        if (state.Result != CombatResult.InProgress)
        {
            RenderBattleResult(state);
            return;
        }

        // Handle player action display pause
        if (_isDisplayingPlayerAction)
        {
            RenderBattleUI(state);

            if (_playerActionDisplayStart.HasValue &&
                (DateTime.Now - _playerActionDisplayStart.Value).TotalMilliseconds >= ActionDisplayMs)
            {
                _isDisplayingPlayerAction = false;
                _playerActionDisplayStart = null;

                // Now process enemy turn if it's their turn
                if (!state.IsPlayerTurn() && state.Result == CombatResult.InProgress)
                {
                    _combatService.ProcessEnemyTurn(state);
                }
            }

            Thread.Sleep(50);
            _flow.Redirect = true;
            return;
        }

        // Handle enemy turn display pause
        if (state.IsDisplayingEnemyTurn)
        {
            RenderBattleUI(state);

            if (state.EnemyTurnDisplayStart.HasValue &&
                (DateTime.Now - state.EnemyTurnDisplayStart.Value).TotalMilliseconds >= ActionDisplayMs)
            {
                state.IsDisplayingEnemyTurn = false;
                _combatService.AdvanceTurn(state);
                _combatService.CheckCombatEnd(state);

                // If next combatant is also an enemy, process their turn automatically
                if (!state.IsPlayerTurn() && state.Result == CombatResult.InProgress)
                {
                    _combatService.ProcessEnemyTurn(state);
                }
            }

            Thread.Sleep(50);
            _flow.Redirect = true;
            return;
        }

        // Process input on player turn only
        if (state.IsPlayerTurn())
        {
            var actionTaken = ProcessPlayerCombatInput(state);

            if (actionTaken && state.LastAttackResult != null)
            {
                // Start player action display pause
                _isDisplayingPlayerAction = true;
                _playerActionDisplayStart = DateTime.Now;
            }
        }
        else
        {
            // Auto-process enemy turn (shouldn't normally reach here due to chaining above)
            _combatService.ProcessEnemyTurn(state);
        }

        RenderBattleUI(state);

        // Keep looping during action displays
        if (_isDisplayingPlayerAction || state.IsDisplayingEnemyTurn)
        {
            _flow.Redirect = true;
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
        // Party status table
        var partyTable = new Table();
        partyTable.AddColumn("Name");
        partyTable.AddColumn("HP");
        partyTable.AddColumn("Status");
        partyTable.Border(TableBorder.Rounded);

        foreach (var player in state.Players)
        {
            var hpColor = player.CurrentHitPoints > player.MaxHitPoints / 2
                ? "green"
                : (player.CurrentHitPoints > player.MaxHitPoints / 4 ? "yellow" : "red");
            var status = player.IsAlive ? (player.HasUsedDefend ? "Defending" : "Active") : "[red]Down[/]";

            partyTable.AddRow(
                player.Name,
                $"[{hpColor}]{player.CurrentHitPoints}/{player.MaxHitPoints}[/]",
                status
            );
        }

        // Enemy table
        var enemyTable = new Table();
        enemyTable.AddColumn("");
        enemyTable.AddColumn("Enemy");
        enemyTable.AddColumn("HP");
        enemyTable.AddColumn("CR");
        enemyTable.Border(TableBorder.Rounded);

        var aliveEnemies = state.GetAliveEnemies();
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            var enemy = aliveEnemies[i];
            var selected = i == state.SelectedTargetIndex ? "[green bold]>[/]" : " ";
            var hpColor = enemy.CurrentHitPoints > enemy.MaxHitPoints / 2
                ? "green"
                : (enemy.CurrentHitPoints > enemy.MaxHitPoints / 4 ? "yellow" : "red");

            enemyTable.AddRow(
                selected,
                enemy.Name,
                $"[{hpColor}]{enemy.CurrentHitPoints}/{enemy.MaxHitPoints}[/]",
                enemy.ChallengeRating.ToString("F1")
            );
        }

        // Last Action Details panel
        var actionPanel = BuildActionDetailsPanel(state);

        // Combat log (last 3 entries to make room for action details)
        var logEntries = state.CombatLog.TakeLast(3).ToList();
        var logContent = logEntries.Count > 0
            ? string.Join("\n", logEntries)
            : "Combat begins...";

        var logPanel = new Panel(new Markup(logContent))
            .Header("[bold]Combat Log[/]")
            .Border(BoxBorder.Rounded);

        // Current turn indicator
        var turnInfo = state.IsPlayerTurn()
            ? "[green]Your turn![/]"
            : "[yellow]Enemy turn...[/]";

        // Build main display
        _display.Update(
            new Panel(
                new Rows(
                    Align.Center(new Markup($"[bold red]COMBAT[/] - Turn {state.CurrentTurn + 1} - {turnInfo}")),
                    new Rule(),
                    new Columns(
                        new Panel(partyTable).Header("[bold blue]Party[/]").Expand(),
                        new Panel(enemyTable).Header("[bold red]Enemies[/]").Expand()
                    ),
                    actionPanel,
                    logPanel
                )).Expand());
        _ctx.Refresh();

        // Controls - hide during action display
        string controlText;
        if (_isDisplayingPlayerAction)
        {
            controlText = "[dim]Your attack...[/]";
        }
        else if (state.IsDisplayingEnemyTurn)
        {
            controlText = "[dim]Enemy is attacking...[/]";
        }
        else
        {
            controlText = $"[green]A[/]ttack | [blue]D[/]efend{(state.CanFlee ? " | [yellow]F[/]lee" : "")} | [dim]W/S or Arrows to select target[/]";
        }

        _controls.Update(
            new Panel(
                Align.Center(
                    new Markup(controlText)
                )).Expand());
        _ctx.Refresh();

        SetMessage(MessageType.Info, _scene.Message);
    }

    private Panel BuildActionDetailsPanel(CombatState state)
    {
        if (state.LastAttackResult == null)
        {
            return new Panel(new Markup("[dim]No actions yet...[/]"))
                .Header("[bold yellow]Last Action Details[/]")
                .Border(BoxBorder.Rounded);
        }

        var result = state.LastAttackResult;

        var rows = new List<IRenderable>
        {
            new Markup($"[bold]{Markup.Escape(result.AttackerName)}[/] attacks [bold]{Markup.Escape(result.TargetName)}[/] with {Markup.Escape(result.DamageSource)}"),
            new Markup($"Attack Roll: {result.GetAttackFormula()}"),
            new Markup($"Result: {result.GetResultSummary()}")
        };

        if (result.IsHit)
        {
            rows.Add(new Markup($"Damage: {result.GetDamageFormula()}"));
        }

        return new Panel(new Rows(rows.ToArray()))
            .Header("[bold yellow]Last Action Details[/]")
            .Border(BoxBorder.Rounded);
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

        // Give item rewards to first player with space
        foreach (var player in SaveGameService.Party.Players)
        {
            if (player.Inventory.Count < 50)
            {
                var item = _shopService.GenerateReward();
                player.Inventory.Add(item);
                break;
            }
        }

        // Give coin rewards
        var coinReward = state.TotalXPEarned / 2;
        foreach (var player in SaveGameService.Party.Players)
        {
            player.coin += coinReward;
        }
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
        _isDisplayingPlayerAction = false;
        _playerActionDisplayStart = null;
        _flow.Screen = Screen.Navigation;
        _flow.Redirect = true;
    }
}
