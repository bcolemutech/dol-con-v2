using DolCon.Core.Enums;

namespace DolCon.Core.Models.Combat;

/// <summary>
/// Adapts a Player to the CombatEntity system for combat
/// HP is derived from party stamina, stats from equipped items
/// </summary>
public class PlayerCombatant : CombatEntity
{
    private readonly Player _player;
    private readonly double _initialStamina;

    /// <summary>
    /// The underlying Player this combatant represents
    /// </summary>
    public Player SourcePlayer => _player;

    public PlayerCombatant(Player player, double partyStamina)
    {
        _player = player;
        _initialStamina = partyStamina;

        // Copy identity from player
        Id = player.Id;
        Name = player.Name;

        // Calculate HP from stamina (0.0-1.0 maps to 0-100 HP)
        MaxHitPoints = (int)(partyStamina * 100);
        CurrentHitPoints = MaxHitPoints;

        // Default D&D stats
        Strength = 10;
        Dexterity = 10;
        Constitution = 10;
        Intelligence = 10;
        Wisdom = 10;
        Charisma = 10;

        // Calculate AC and combat stats from equipment
        CalculateStatsFromEquipment();
    }

    private void CalculateStatsFromEquipment()
    {
        // Base AC
        ArmorClass = 10;

        // Find equipped armor items and calculate AC bonus
        var equippedArmorItems = _player.Inventory
            .Where(i => i.Equipped && i.Tags.Any(t => t.Name.Contains("Armor")))
            .ToList();

        foreach (var item in equippedArmorItems)
        {
            ArmorClass += CalculateArmorBonus(item);
        }
    }

    private int CalculateArmorBonus(Item item)
    {
        var rarityBonus = (int)item.Rarity;

        // Different armor slots provide different base protection
        return item.Equipment switch
        {
            Equipment.Body => 3 + rarityBonus,
            Equipment.Head => 1 + rarityBonus,
            Equipment.Legs => 2 + rarityBonus,
            Equipment.Feet => 1 + rarityBonus,
            Equipment.Hands => 1 + rarityBonus,
            Equipment.Shield => 2,
            _ => 0
        };
    }

    /// <summary>
    /// Calculate weapon damage based on equipped weapon
    /// </summary>
    public int GetWeaponDamage()
    {
        var weapon = _player.Inventory
            .FirstOrDefault(i => i.Equipped &&
                i.Tags.Any(t => t.Name.Contains("Weapon")));

        if (weapon == null)
        {
            return 2; // Unarmed damage
        }

        var rarityBonus = (int)weapon.Rarity * 2;

        // Base damage by weapon type
        var baseDamage = weapon.Equipment switch
        {
            Equipment.OneHanded => 4,
            Equipment.TwoHanded => 6,
            _ => 4
        };

        return baseDamage + rarityBonus;
    }

    /// <summary>
    /// Calculate remaining stamina percentage after combat damage
    /// Used to sync damage back to party stamina
    /// </summary>
    public double GetRemainingStaminaPercent()
    {
        if (MaxHitPoints <= 0) return 0;
        return (double)CurrentHitPoints / MaxHitPoints * _initialStamina;
    }
}
