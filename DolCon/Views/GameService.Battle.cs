using DolCon.Enums;
using DolCon.Models.Combat;
using DolCon.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DolCon.Views;

public partial class GameService
{
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
        }

        var state = _scene.CombatState;

        // Check if combat ended
        if (state.Result != CombatResult.InProgress)
        {
            RenderBattleResult(state);
            return;
        }

        // Process input
        if (state.IsPlayerTurn())
        {
            ProcessPlayerCombatInput(state);
        }
        else
        {
            // Auto-process enemy turn
            _combatService.ProcessEnemyTurn(state);
        }

        RenderBattleUI(state);
    }

    private void ProcessPlayerCombatInput(CombatState state)
    {
        switch (_flow.Key)
        {
            case { Key: ConsoleKey.A }:
                var targetId = GetSelectedTargetId(state);
                _combatService.ProcessPlayerAction(state, CombatAction.Attack, targetId);
                break;
            case { Key: ConsoleKey.D }:
                _combatService.ProcessPlayerAction(state, CombatAction.Defend);
                break;
            case { Key: ConsoleKey.F } when state.CanFlee:
                _combatService.ProcessPlayerAction(state, CombatAction.Flee);
                break;
            case { Key: ConsoleKey.UpArrow } or { Key: ConsoleKey.W }:
                state.SelectedTargetIndex = Math.Max(0, state.SelectedTargetIndex - 1);
                break;
            case { Key: ConsoleKey.DownArrow } or { Key: ConsoleKey.S }:
                var aliveCount = state.GetAliveEnemies().Count;
                state.SelectedTargetIndex = Math.Min(aliveCount - 1, state.SelectedTargetIndex + 1);
                break;
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

        // Combat log (last 5 entries)
        var logEntries = state.CombatLog.TakeLast(5).ToList();
        var logContent = logEntries.Count > 0
            ? string.Join("\n", logEntries)
            : "Combat begins...";

        var logPanel = new Panel(new Markup(logContent))
            .Header("[bold]Combat Log[/]")
            .Border(BoxBorder.Rounded);

        // Current turn indicator
        var turnInfo = state.IsPlayerTurn()
            ? "[green]Your turn![/]"
            : $"[yellow]Enemy turn...[/]";

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
                    logPanel
                )).Expand());
        _ctx.Refresh();

        // Controls
        var fleeOption = state.CanFlee ? " | [yellow]F[/]lee" : "";
        _controls.Update(
            new Panel(
                Align.Center(
                    new Markup($"[green]A[/]ttack | [blue]D[/]efend{fleeOption} | [dim]W/S or Arrows to select target[/]")
                )).Expand());
        _ctx.Refresh();

        SetMessage(MessageType.Info, _scene.Message);
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
        _flow.Screen = Screen.Navigation;
        _flow.Redirect = true;
    }
}
