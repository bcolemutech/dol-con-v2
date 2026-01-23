using DolCon.Enums;

namespace DolCon.Models.Combat;

/// <summary>
/// Base class for any entity that can participate in combat (players, enemies, companions)
/// Follows D&D-style stat system
/// </summary>
public abstract class CombatEntity
{
    // Basic Identity
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Core D&D Stats
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;
    
    // Combat Stats
    public int MaxHitPoints { get; set; }
    public int CurrentHitPoints { get; set; }
    public int ArmorClass { get; set; } = 10;
    public int Initiative { get; set; }
    
    // D&D-style Saving Throws
    public int StrengthSave { get; set; }
    public int DexteritySave { get; set; }
    public int ConstitutionSave { get; set; }
    public int IntelligenceSave { get; set; }
    public int WisdomSave { get; set; }
    public int CharismaSave { get; set; }
    
    // Status Effects (aligned with your 6-phase combat system)
    public List<StatusEffect> ActiveEffects { get; set; } = new();
    public List<CombatAbility> AvailableAbilities { get; set; } = new();
    
    // Combat Phase Flags
    public bool HasUsedPrepare1 { get; set; }
    public bool HasUsedAttack { get; set; }
    public bool HasUsedDefend { get; set; }
    public bool HasUsedPrepare2 { get; set; }
    
    // State
    public bool IsAlive => CurrentHitPoints > 0;
    public bool IsConscious => CurrentHitPoints > 0;
    public bool CanAct => IsConscious && !IsStunned && !IsParalyzed;
    
    // Common Status Checks
    public bool IsStunned => ActiveEffects.Any(e => e.Type == StatusEffectType.Stunned);
    public bool IsParalyzed => ActiveEffects.Any(e => e.Type == StatusEffectType.Paralyzed);
    public bool IsPoisoned => ActiveEffects.Any(e => e.Type == StatusEffectType.Poisoned);
    public bool IsBlinded => ActiveEffects.Any(e => e.Type == StatusEffectType.Blinded);
    
    // D&D-style Modifier Calculation
    public int GetModifier(int stat)
    {
        return (stat - 10) / 2;
    }
    
    // Advantage/Disadvantage Roll (D&D 5e style)
    public int RollWithAdvantage(Random rng, int sides = 20)
    {
        var roll1 = rng.Next(1, sides + 1);
        var roll2 = rng.Next(1, sides + 1);
        return Math.Max(roll1, roll2);
    }
    
    public int RollWithDisadvantage(Random rng, int sides = 20)
    {
        var roll1 = rng.Next(1, sides + 1);
        var roll2 = rng.Next(1, sides + 1);
        return Math.Min(roll1, roll2);
    }
    
    public int RollD20(Random rng)
    {
        return rng.Next(1, 21);
    }
    
    // Apply damage and check for death
    public virtual void TakeDamage(int damage)
    {
        CurrentHitPoints -= damage;
        if (CurrentHitPoints < 0)
            CurrentHitPoints = 0;
    }
    
    // Heal
    public virtual void Heal(int amount)
    {
        CurrentHitPoints += amount;
        if (CurrentHitPoints > MaxHitPoints)
            CurrentHitPoints = MaxHitPoints;
    }
    
    // Process effects at phase start/end
    public virtual void ProcessStartOfTurnEffects()
    {
        foreach (var effect in ActiveEffects.ToList())
        {
            effect.OnStartOfTurn(this);
        }
        CleanupExpiredEffects();
    }
    
    public virtual void ProcessEndOfTurnEffects()
    {
        foreach (var effect in ActiveEffects.ToList())
        {
            effect.OnEndOfTurn(this);
        }
        CleanupExpiredEffects();
    }
    
    private void CleanupExpiredEffects()
    {
        ActiveEffects.RemoveAll(e => e.IsExpired);
    }
    
    // Reset phase flags for new turn
    public void ResetPhaseFlags()
    {
        HasUsedPrepare1 = false;
        HasUsedAttack = false;
        HasUsedDefend = false;
        HasUsedPrepare2 = false;
    }
}
