namespace DolCon.Core.Models.Combat;

/// <summary>
/// Tracks all state for an active combat encounter
/// </summary>
public class CombatState
{
    /// <summary>
    /// Player combatants in this fight
    /// </summary>
    public List<PlayerCombatant> Players { get; set; } = new();

    /// <summary>
    /// Enemy combatants in this fight
    /// </summary>
    public List<Enemy> Enemies { get; set; } = new();

    /// <summary>
    /// Current combat phase
    /// </summary>
    public CombatPhase CurrentPhase { get; set; } = CombatPhase.Start;

    /// <summary>
    /// Current turn number (0-indexed)
    /// </summary>
    public int CurrentTurn { get; set; }

    /// <summary>
    /// ID of the currently active combatant
    /// </summary>
    public Guid ActiveCombatantId { get; set; }

    /// <summary>
    /// All combatants sorted by initiative
    /// </summary>
    public List<CombatEntity> TurnOrder { get; set; } = new();

    /// <summary>
    /// Index into TurnOrder for current combatant
    /// </summary>
    public int CurrentTurnIndex { get; set; }

    /// <summary>
    /// Result of this combat encounter
    /// </summary>
    public CombatResult Result { get; set; } = CombatResult.InProgress;

    /// <summary>
    /// Log of combat events for display
    /// </summary>
    public List<string> CombatLog { get; set; } = new();

    /// <summary>
    /// Total damage taken by all players (for stamina recovery calculation)
    /// </summary>
    public int TotalDamageTaken { get; set; }

    /// <summary>
    /// Total XP earned from defeated enemies
    /// </summary>
    public int TotalXPEarned { get; set; }

    /// <summary>
    /// Pending loot drops from defeated enemies
    /// </summary>
    public List<LootDrop> PendingLoot { get; set; } = new();

    /// <summary>
    /// Whether the player can still flee (only allowed on first turn)
    /// </summary>
    public bool CanFlee => CurrentTurn == 0 && Result == CombatResult.InProgress;

    /// <summary>
    /// Index of selected enemy target for player attacks
    /// </summary>
    public int SelectedTargetIndex { get; set; }

    /// <summary>
    /// Details of the last attack action for UI display
    /// </summary>
    public AttackResult? LastAttackResult { get; set; }

    /// <summary>
    /// Indicates that an enemy turn is being displayed (for pause logic)
    /// </summary>
    public bool IsDisplayingEnemyTurn { get; set; }

    /// <summary>
    /// Timestamp when enemy turn display started
    /// </summary>
    public DateTime? EnemyTurnDisplayStart { get; set; }

    /// <summary>
    /// Get the currently active combatant
    /// </summary>
    public CombatEntity? GetActiveCombatant()
    {
        if (TurnOrder.Count == 0 || CurrentTurnIndex >= TurnOrder.Count)
            return null;
        return TurnOrder[CurrentTurnIndex];
    }

    /// <summary>
    /// Check if the current combatant is a player
    /// </summary>
    public bool IsPlayerTurn()
    {
        var active = GetActiveCombatant();
        return active != null && Players.Any(p => p.Id == active.Id);
    }

    /// <summary>
    /// Get alive enemies
    /// </summary>
    public List<Enemy> GetAliveEnemies()
    {
        return Enemies.Where(e => e.IsAlive).ToList();
    }

    /// <summary>
    /// Get alive players
    /// </summary>
    public List<PlayerCombatant> GetAlivePlayers()
    {
        return Players.Where(p => p.IsAlive).ToList();
    }
}
