using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.Combat;

namespace DolCon.Core.Services;

public interface ISkillService
{
    void ApplySkillGains(Player player, CombatState combatState);
}

public class SkillService : ISkillService
{
    public void ApplySkillGains(Player player, CombatState combatState)
    {
        if (combatState.Result != CombatResult.Victory) return;

        var xpGained = (double)combatState.TotalXPEarned;
        var baseGain = Math.Log2(1 + xpGained);

        // Weapon skill based on equipped weapon
        var weapon = player.Inventory
            .FirstOrDefault(i => i.Equipped && i.Tags.Any(t => t.Name.Contains("Weapon")));

        if (weapon == null)
        {
            player.Skills.Unarmed += baseGain;
        }
        else
        {
            switch (weapon.Equipment)
            {
                case Equipment.OneHanded:
                    player.Skills.OneHanded += baseGain;
                    break;
                case Equipment.TwoHanded:
                    player.Skills.TwoHanded += baseGain;
                    break;
            }
        }

        // Armor skill if wearing any armor piece
        var hasArmor = player.Inventory.Any(i => i.Equipped &&
            i.Tags.Any(t => t.Name.Contains("Armor")) &&
            i.Equipment is Equipment.Head or Equipment.Body or Equipment.Legs
                or Equipment.Feet or Equipment.Hands);
        if (hasArmor)
        {
            player.Skills.Armor += baseGain;
        }

        // Shield skill
        var hasShield = player.Inventory.Any(i => i.Equipped &&
            i.Equipment == Equipment.Shield);
        if (hasShield)
        {
            player.Skills.Shield += baseGain;
        }

        // Light Aptitude based on enemy categories
        var hasUndead = combatState.Enemies.Any(e => e.Category == EnemyCategory.Undead);
        var hasDemon = combatState.Enemies.Any(e => e.Category == EnemyCategory.Demon);

        if (hasDemon)
        {
            player.Skills.LightAptitude += baseGain;
        }
        else if (hasUndead)
        {
            player.Skills.LightAptitude += baseGain * 0.25;
        }
    }
}
