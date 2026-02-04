using DolCon.Core.Enums;

namespace DolCon.Core.Models.Combat;

/// <summary>
/// Combat phases for turn-based combat
/// </summary>
public enum CombatPhase
{
    Start,
    Attack,
    Defend,
    End
}

/// <summary>
/// Target types for combat abilities
/// </summary>
public enum TargetType
{
    Self,
    Single,
    AllEnemies,
    AllAllies,
    All
}

/// <summary>
/// Result of combat encounter
/// </summary>
public enum CombatResult
{
    InProgress,
    Victory,
    Defeat,
    Fled
}

/// <summary>
/// Player action during combat
/// </summary>
public enum CombatAction
{
    Attack,
    Defend,
    Flee
}

/// <summary>
/// Status effect that can be applied to combat entities
/// </summary>
public class StatusEffect
{
    public string Name { get; set; } = string.Empty;
    public StatusEffectType Type { get; set; }
    public int Duration { get; set; }
    public int RemainingDuration { get; set; }
    public int DamagePerTurn { get; set; }
    public DamageType DamageType { get; set; }

    public bool IsExpired => RemainingDuration <= 0;

    public void OnStartOfTurn(CombatEntity entity)
    {
        if (DamagePerTurn > 0)
        {
            entity.TakeDamage(DamagePerTurn);
        }
        RemainingDuration--;
    }

    public void OnEndOfTurn(CombatEntity entity)
    {
        // Can be extended for effects that trigger at end of turn
    }
}

/// <summary>
/// Combat ability that can be used by entities
/// </summary>
public class CombatAbility
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CombatPhase> ValidPhases { get; set; } = new();
    public int ManaCost { get; set; }
    public int Cooldown { get; set; }
    public int CurrentCooldown { get; set; }
    public int BaseDamage { get; set; }
    public DamageType DamageType { get; set; }
    public TargetType TargetType { get; set; }
    public bool RequiresAttackRoll { get; set; }
    public bool RequiresSavingThrow { get; set; }
    public string? SavingThrowStat { get; set; }
    public int SaveDC { get; set; }
}

/// <summary>
/// Loot drop definition for enemies
/// </summary>
public class LootDrop
{
    public string ItemName { get; set; } = string.Empty;
    public double DropChance { get; set; }
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
}
