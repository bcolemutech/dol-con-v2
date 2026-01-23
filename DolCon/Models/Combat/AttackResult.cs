namespace DolCon.Models.Combat;

/// <summary>
/// Captures detailed information about an attack for UI display
/// </summary>
public class AttackResult
{
    public string AttackerName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;

    // Attack roll components
    public int D20Roll { get; set; }
    public int AttackModifier { get; set; }
    public int TotalAttack { get; set; }
    public int TargetAC { get; set; }

    // Result
    public bool IsHit { get; set; }
    public bool IsCritical { get; set; }
    public bool IsNatural1 { get; set; }

    // Damage (only populated on hit)
    public int BaseDamage { get; set; }
    public int BonusDamage { get; set; }
    public int TotalDamage { get; set; }

    // Damage source description (e.g., "Longsword", "Claws", "Unarmed")
    public string DamageSource { get; set; } = string.Empty;

    /// <summary>
    /// Generate formatted attack formula string
    /// Example: "d20(15) + 3 = 18 vs AC 14"
    /// </summary>
    public string GetAttackFormula()
    {
        var modPart = AttackModifier >= 0
            ? $"+ {AttackModifier}"
            : $"- {Math.Abs(AttackModifier)}";
        return $"d20({D20Roll}) {modPart} = {TotalAttack} vs AC {TargetAC}";
    }

    /// <summary>
    /// Generate formatted damage formula string
    /// Example: "4 base + 2 bonus = 6 (x2 CRIT = 12)"
    /// </summary>
    public string GetDamageFormula()
    {
        if (!IsHit) return string.Empty;

        var formula = $"{BaseDamage} base";
        if (BonusDamage > 0)
        {
            formula += $" + {BonusDamage} bonus";
        }

        var preCritDamage = BaseDamage + BonusDamage;
        formula += $" = {preCritDamage}";

        if (IsCritical)
        {
            formula += $" x2 CRIT = {TotalDamage}";
        }

        return formula;
    }

    /// <summary>
    /// Generate result summary string with Spectre.Console markup
    /// </summary>
    public string GetResultSummary()
    {
        if (IsNatural1)
        {
            return "[red]MISS[/] (Natural 1)";
        }

        if (IsCritical)
        {
            return $"[green bold]CRITICAL HIT![/] {TotalDamage} damage";
        }

        if (IsHit)
        {
            return $"[green]HIT[/] for {TotalDamage} damage";
        }

        return "[yellow]MISS[/]";
    }
}
